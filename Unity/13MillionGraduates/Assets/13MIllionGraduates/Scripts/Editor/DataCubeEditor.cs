using UnityEditor;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

namespace Game
{
    [CustomEditor(typeof(DataCube))]
    public class DataCubeEditor : Editor
    {
        private DataCube _target;
        private string _value;

        private void OnEnable()
        {
            _target = target as DataCube;

            if (_target != null)
            {
                _value = _target.Value;
            }
            else
            {
                _value = "10";
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (_target == null) return;

            //Initialize Carpet
            GUILayout.Space(20);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            GUILayout.Label("SetValue", EditorStyles.boldLabel);
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Value", GUILayout.Width(40));
            _value = EditorGUILayout.TextField(_value);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            if (GUILayout.Button("Set Value", GUILayout.Height(20)))
            {
                _target.SetValue(_value);
                _value = _target.Value;
                EditorUtility.SetDirty(_target);
                Repaint();
            }

            //Infos
            GUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.LabelField($"Current data cube state：value {_target.Value} , Type {_target.Type}", EditorStyles.miniLabel);
        }
    }
}