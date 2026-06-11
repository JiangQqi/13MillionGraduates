using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace Game
{
    [CustomEditor(typeof(AreaManager))]
    public class AreaManagerEditor : Editor
    {
        private AreaManager _target;
        private int _row;
        private int _col;

        private int _x1;
        private int _x2;
        private int _y1;
        private int _y2;

        private void OnEnable()
        {
            _target = target as AreaManager;

            if (_target != null)
            {
                _row = Mathf.Clamp(_target.Row, 1, 20);
                _col = Mathf.Clamp(_target.Col, 1, 20);
            }
            else
            {
                _row = 10;
                _col = 10;
            }

            _x1 = _y1 = 1;
            _x2 = _y2 = 2;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (_target == null) return;

            //Initialize Carpet Manager
            GUILayout.Space(20);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            GUILayout.Label("Initialize Carpet Manager", EditorStyles.boldLabel);
            GUILayout.Space(5);

            _row = EditorGUILayout.IntSlider("Row", _row, 1, 20);
            _col = EditorGUILayout.IntSlider("Col", _col, 1, 20);

            GUILayout.Space(5);

            if (GUILayout.Button("Initialize Carpet Manager", GUILayout.Height(20)))
            {
                _target.InitializeCarpetManager(_row, _col);
                EditorUtility.SetDirty(_target);
                Repaint();
            }

            //Create Area
            GUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            GUILayout.Label("Create Area", EditorStyles.boldLabel);
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label("X1", GUILayout.Width(30));
            _x1 = EditorGUILayout.IntField(_x1, GUILayout.Width(60));
            GUILayout.Label("Y1", GUILayout.Width(30));
            _y1 = EditorGUILayout.IntField(_y1, GUILayout.Width(60));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("X2", GUILayout.Width(30));
            _x2 = EditorGUILayout.IntField(_x2, GUILayout.Width(60));
            GUILayout.Label("Y2", GUILayout.Width(30));
            _y2 = EditorGUILayout.IntField(_y2, GUILayout.Width(60));
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            if (GUILayout.Button("Initialize Value", GUILayout.Height(20)))
            {
                _target.CreateArea(_x1 - 1, _x2 - 1, _y1 - 1, _y2 - 1);
                EditorUtility.SetDirty(_target);
                Repaint();
            }

            //Infos
            GUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.LabelField($"Current carpet manager state：row={_target.Row}, col={_target.Col}", EditorStyles.miniLabel);
        }
    }
}