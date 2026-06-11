using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    [CreateAssetMenu(menuName = "LevelConfig")]
    public class LevelConfig : ScriptableObject
    {
        [Header("Basic")]
        [Tooltip("关卡ID，同关卡顺序（测试关卡为0，正式关卡从1开始）")]
        public int LevelID;
        public List<int> RequiredLevels;
        public string LevelTitle;
        [TextArea(3, 10)] public string LevelDescription;
        public int OptimalLines;
        public int OptimalSteps;

        [Header("Game Mode")]
        public GameMode GameMode;
        public int GridRows = 5;
        public int GridCols = 5;

        [Header("Initial DataCubes")]
        public List<DataCubeEntry> InitialDataCubes = new();

        [Header("Operations")]
        public OperationsConfig AvailableOperations = new();

        [Header("Correct Operations")]
        [TextArea(3, 10)] public string CorrectOperations;

        [Header("Inputs")]
        [Tooltip("场景中 Inbox 的输入数据，为空时随机生成")]
        public List<string> VisualInputCubes = new();
        [Tooltip("后台验证用的多组测试输入")]
        public List<Cubes> TestInputCubes = new List<Cubes>();

        [Header("Talks")]
        [TextArea(2, 4)] public List<string> ActiveTalk;
        [TextArea(2, 4)] public List<string> MoreTalk;
        [TextArea(2, 4)] public List<string> ExampleTalk;
        [TextArea(2, 4)] public List<string> OptimalTalk;

        [Header("Audio")]
        public AudioClip BGM;
    }

    [Serializable]
    public class Cubes
    {
        public List<string> Values = new();
    }
}