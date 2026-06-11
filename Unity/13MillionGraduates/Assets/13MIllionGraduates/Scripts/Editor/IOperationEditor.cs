using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// 因 JumpOperation 需引用 LabelOperation实例，导致该 Editor 暂无法正常使用
    /// </summary>
    [CustomPropertyDrawer(typeof(IOperation), true)]
    public class IOperationDrawer : PropertyDrawer
    {
        private static string[] _operationNames = Enum.GetNames(typeof(OperationType));
        private static OperationType[] _operationValues = (OperationType[])Enum.GetValues(typeof(OperationType));

        private static Dictionary<OperationType, Type> _typeMap = new()
        {
            { OperationType.InBox, typeof(InBoxOperation) },
            { OperationType.OutBox, typeof(OutBoxOperation) },
            { OperationType.CopyTo, typeof(CopyToOperation) },
            { OperationType.CopyFrom, typeof(CopyFromOperation) },
            { OperationType.Arithmetic, typeof(ArithmeticOperation) },
            { OperationType.Jump, typeof(JumpOperation)},
            { OperationType.Label, typeof(LabelOperation)},
        };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            OperationType currentType = OperationType.InBox;
            if (property.managedReferenceValue != null)
            {
                var currOp = (IOperation)property.managedReferenceValue;
                currentType = currOp.OperationType;
            }

            // 下拉框
            Rect popupRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            int selectedIdx = Array.IndexOf(_operationValues, currentType);
            int newIdx = EditorGUI.Popup(popupRect, label.text, selectedIdx, _operationNames);

            // 类型改变时创建新实例
            if (newIdx != selectedIdx && newIdx >= 0 || property.managedReferenceValue == null) 
            {
                OperationType newType = _operationValues[newIdx];
                Type instanceType = _typeMap[newType];
                var newInstance = Activator.CreateInstance(instanceType);
                property.managedReferenceValue = newInstance;
            }

            // 参数
            if (property.managedReferenceValue != null)
            {
                var currOp = (IOperation)property.managedReferenceValue;
                var endProperty = property.GetEndProperty();
                var param = property.Copy();
                float yOffset = position.y + EditorGUIUtility.singleLineHeight + 2f;

                while (param.NextVisible(true) && !SerializedProperty.EqualContents(param, endProperty))
                {
                    float paramHeight = EditorGUI.GetPropertyHeight(param, true);
                    Rect propertyRect = new Rect(position.x + 15f, yOffset, position.width - 15f, paramHeight);

                    string paramLabel = GetParamLabel(currOp.OperationType, param.name);
                    EditorGUI.PropertyField(propertyRect, param, new GUIContent(paramLabel), true);
                    yOffset += paramHeight + 2f;
                }
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float totalHeight = EditorGUIUtility.singleLineHeight;

            if (property.managedReferenceValue != null)
            {
                var endProperty = property.GetEndProperty();
                var param = property.Copy();

                while (param.NextVisible(true) && !SerializedProperty.EqualContents(param, endProperty))
                {
                    totalHeight += EditorGUI.GetPropertyHeight(param, true) + 2f;
                }
            }

            return totalHeight;
        }

        private string GetParamLabel(OperationType opType, string propertyName)
        {
            return propertyName switch
            {
                "Param1" => opType switch
                {
                    OperationType.CopyTo => "Area Id",
                    OperationType.CopyFrom => "Area Id",
                    OperationType.Arithmetic => "Area Id",
                    OperationType.Jump => "Label",
                    _ => "Param1"
                },
                "Param2" => opType switch
                {
                    OperationType.CopyTo => "Index",
                    OperationType.CopyFrom => "Index",
                    OperationType.Arithmetic => "Index",
                    OperationType.Jump => "JumpType",
                    _ => "Param2"
                },
                "Param3" => opType switch
                {
                    OperationType.CopyTo => "IsAddress",
                    OperationType.CopyFrom => "IsAddress",
                    OperationType.Arithmetic => "Arithmetic Type",
                    _ => "Param3"
                },
                "Param4" => opType switch
                {
                    OperationType.Arithmetic => "IsAddress",
                    _ => "Param4"
                },
                _ => propertyName
            };
        }
    }
}