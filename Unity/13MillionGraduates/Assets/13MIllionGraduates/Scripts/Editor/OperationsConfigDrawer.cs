using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game
{
    [CustomPropertyDrawer(typeof(OperationsConfig))]
    public class OperationsConfigDrawer : PropertyDrawer
    {
        private static readonly Dictionary<string, bool> Foldouts = new();

        private static readonly OperationType[] Types =
            (OperationType[])System.Enum.GetValues(typeof(OperationType));

        private static readonly HashSet<OperationType> AddressableTypes = new()
        {
            OperationType.CopyTo,
            OperationType.CopyFrom,
            OperationType.Arithmetic,
        };

        private const float BoxPadTop = 4f;
        private const float BoxPadBottom = 4f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            string key = property.propertyPath;
            if (!Foldouts.ContainsKey(key)) Foldouts[key] = true;

            if (!Foldouts[key])
                return EditorGUIUtility.singleLineHeight;

            float single = EditorGUIUtility.singleLineHeight;
            float height = single; // foldout header

            foreach (var type in Types)
            {
                var cfg = property.FindPropertyRelative(type.ToString());
                var enabled = cfg.FindPropertyRelative("Enabled");

                height += single; // toggle row
                if (enabled.boolValue)
                {
                    if (AddressableTypes.Contains(type))
                        height += single;
                    if (type == OperationType.Arithmetic)
                        height += single * 3;
                    if (type == OperationType.Jump)
                        height += single * 3;
                }
            }

            height += BoxPadTop + BoxPadBottom;
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            string key = property.propertyPath;
            if (!Foldouts.ContainsKey(key)) Foldouts[key] = true;

            float single = EditorGUIUtility.singleLineHeight;
            float width = position.width;

            // Foldout header
            Rect foldoutRect = new Rect(position.x, position.y, width, single);
            Foldouts[key] = EditorGUI.Foldout(foldoutRect, Foldouts[key], "Operations", true);

            if (!Foldouts[key])
            {
                EditorGUI.EndProperty();
                return;
            }

            // 边框 box
            float boxY = position.y + single;
            float boxH = position.height - single;
            Rect boxRect = new Rect(position.x + 2f, boxY, width - 4f, boxH);
            GUI.Box(boxRect, GUIContent.none, EditorStyles.helpBox);

            float y = boxY + BoxPadTop;
            float contentX = position.x + 8f;
            float contentWidth = width - 16f;

            foreach (var type in Types)
            {
                var cfg = property.FindPropertyRelative(type.ToString());
                var enabledProp = cfg.FindPropertyRelative("Enabled");

                // 主行：[✓] TypeName  |  ON/OFF
                Rect lineRect = new Rect(contentX, y, contentWidth, single);
                EditorGUI.BeginChangeCheck();
                bool on = EditorGUI.ToggleLeft(lineRect, $"  {type}", enabledProp.boolValue);
                if (EditorGUI.EndChangeCheck())
                {
                    enabledProp.boolValue = on;
                    property.serializedObject.ApplyModifiedProperties();
                }

                Rect statusRect = new Rect(contentX + contentWidth - 50f, y, 50f, single);
                GUI.color = enabledProp.boolValue ? Color.green : Color.red;
                EditorGUI.LabelField(statusRect, enabledProp.boolValue ? "ON" : "OFF", EditorStyles.boldLabel);
                GUI.color = Color.white;

                y += single;

                if (enabledProp.boolValue)
                {
                    float indent = 15f;
                    float subX = contentX + indent;
                    float subWidth = contentWidth - indent;

                    // 取址模式
                    if (AddressableTypes.Contains(type))
                    {
                        var addrProp = cfg.FindPropertyRelative("Addressabled");
                        Rect r = new Rect(subX, y, subWidth, single);
                        EditorGUI.BeginChangeCheck();
                        bool v = EditorGUI.ToggleLeft(r, "取址模式", addrProp.boolValue);
                        if (EditorGUI.EndChangeCheck()) { addrProp.boolValue = v; property.serializedObject.ApplyModifiedProperties(); }
                        y += single;
                    }

                    // Arithmetic 子选项
                    if (type == OperationType.Arithmetic)
                    {
                        y = DrawToggle(cfg, property, "Arithmetic_Basic",   "四则运算 (Add/Sub/Mul/Div)", subX, y, subWidth);
                        y = DrawToggle(cfg, property, "Arithmetic_Unary",   "一元运算 (Inc/Dec)", subX, y, subWidth);
                        y = DrawToggle(cfg, property, "Arithmetic_Compare", "比较运算 (==/!=/>/</>=/<=)", subX, y, subWidth);
                    }

                    // Jump 子选项
                    if (type == OperationType.Jump)
                    {
                        y = DrawToggle(cfg, property, "Jump_Always",    "Always",      subX, y, subWidth);
                        y = DrawToggle(cfg, property, "Jump_IfZero",    "If Zero",     subX, y, subWidth);
                        y = DrawToggle(cfg, property, "Jump_IfNegative", "If Negative", subX, y, subWidth);
                    }
                }
            }

            EditorGUI.EndProperty();
        }

        private static float DrawToggle(SerializedProperty parent, SerializedProperty rootProperty,
            string propName, string displayLabel, float x, float y, float width)
        {
            var prop = parent.FindPropertyRelative(propName);
            Rect r = new Rect(x, y, width, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginChangeCheck();
            bool v = EditorGUI.ToggleLeft(r, displayLabel, prop.boolValue);
            if (EditorGUI.EndChangeCheck()) { prop.boolValue = v; rootProperty.serializedObject.ApplyModifiedProperties(); }
            return y + EditorGUIUtility.singleLineHeight;
        }
    }
}
