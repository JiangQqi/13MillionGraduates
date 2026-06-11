using UnityEditor;
using UnityEngine;

namespace Game
{
    [CustomEditor(typeof(Carpet))]
    public class CarpetEditor : Editor
    {
        private Carpet _target;
        private int _row;
        private int _col;

        private int _valueIndex;
        private string _value;

        private void OnEnable()
        {
            _target = target as Carpet;

            if (_target != null)
            {
                _row = Mathf.Clamp(_target.Row, 1, 10);
                _col = Mathf.Clamp(_target.Col, 1, 10);
            }
            else
            {
                _row = 1;
                _col = 1;
            }

            _valueIndex = 0;
            _value = "10";
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (_target == null) return;

            //Initialize Carpet
            GUILayout.Space(20);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            GUILayout.Label("Initialize Carpet", EditorStyles.boldLabel);
            GUILayout.Space(5);

            _row = EditorGUILayout.IntSlider("Row", _row, 1, 10);
            _col = EditorGUILayout.IntSlider("Col", _col, 1, 10);

            GUILayout.Space(5);

            if (GUILayout.Button("Initialize Carpet", GUILayout.Height(20))) 
            {
                _target.InitializeCarpet(_row, _col);
                EditorUtility.SetDirty(_target);
                Repaint();
            }

            //Initialize Value
            GUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            GUILayout.Label("Initialize Value", EditorStyles.boldLabel);
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Index", GUILayout.Width(40));
            _valueIndex = EditorGUILayout.IntField(_valueIndex, GUILayout.Width(60));
            GUILayout.Label("Value", GUILayout.Width(40));
            _value = EditorGUILayout.TextField(_value);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            if (GUILayout.Button("Initialize Value", GUILayout.Height(20)))
            {
                _target.SpawnDataCube(_valueIndex, _value);
                EditorUtility.SetDirty(_target);
                Repaint();
            }

            //Infos
            GUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.LabelField($"Current carpet state：row={_target.Row}, col={_target.Col}", EditorStyles.miniLabel);
        }
    }
}