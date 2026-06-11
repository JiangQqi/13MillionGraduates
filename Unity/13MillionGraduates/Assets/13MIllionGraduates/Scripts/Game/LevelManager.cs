using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Game
{
    public class LevelManager : MonoBehaviour, IResettable
    {
        public static LevelManager Ins => _ins == null ? _ins = FindFirstObjectByType<LevelManager>() : _ins;
        private static LevelManager _ins;

        private int m_LevelId;
        private LevelConfig m_LevelConfig;
        private GameMode m_GameMode;
        private OperationsConfig m_AvailableOperations = new();
        private List<IOperation> m_CorrectOperations;
        private string[] m_InitialCarpetValues;

        public int LevelId => m_LevelId;
        public LevelConfig LevelConfig => m_LevelConfig;
        public GameMode GameMode => m_GameMode;
        public OperationsConfig AvailableOperations => m_AvailableOperations;
        public List<IOperation> CorrectOperations => m_CorrectOperations;
        public string[] InitialCarpetValues => m_InitialCarpetValues;

        /// <summary>
        /// LevelConfig是否指定VisualInputCubes
        /// </summary>
        private bool m_ForcedVisualInputCubes = false;
        private List<string> m_CurrVisualInputCubes;
        private List<string> m_CurrExpectedOutputCubes;
        private List<List<string>> m_TestInputCubes;

        public List<string> CurrVisualInputCubes => m_CurrVisualInputCubes;
        public string CurrExpectedOutputCube(int i) => m_CurrExpectedOutputCubes[i];
        public int CurrExpectedOutputCount => m_CurrExpectedOutputCubes?.Count ?? 0;
        public List<List<string>> TestInputCubes => m_TestInputCubes;

        public event UnityAction OnLevelInitialized;

        private void Awake()
        {
            GameManager.RegisterResettable(this);
        }

        private void Start()
        {
            Initialize(Elevator.Ins?.PendingLevelConfig);
        }

        public void OnReset()
        {
            GenerateVisualInputCubes();
            SimulateVisualInputCubes();

            GameManager.Ins.InBoxConveyer.Init(CurrVisualInputCubes);
        }

        public void Initialize(LevelConfig cfg)
        {
            if (cfg == null) return;
            m_LevelConfig = cfg;

            m_LevelId = cfg.LevelID;
            m_GameMode = cfg.GameMode;
            switch (m_GameMode)
            {
                case GameMode.Classic:
                    var carpet = GameManager.Ins.Carpet;

                    carpet.gameObject.SetActive(true);
                    GameManager.Ins.AreaManager.gameObject.SetActive(false);

                    carpet.InitializeCarpet(cfg.GridRows, cfg.GridCols);
                    foreach (var entry in cfg.InitialDataCubes) carpet.SpawnDataCube(entry.Index, entry.Value);
                    carpet.CacheCurrDataCubes();
                    m_InitialCarpetValues = carpet.ExportValues();
                    break;

                case GameMode.Advanced:
                    GameManager.Ins.Carpet.gameObject.SetActive(false);
                    GameManager.Ins.AreaManager.gameObject.SetActive(true);
                    break;
            }
            m_AvailableOperations = cfg.AvailableOperations;
            m_CorrectOperations = OperationSerializer.Parse(cfg.CorrectOperations);

            m_ForcedVisualInputCubes = cfg.VisualInputCubes.Count > 0;
            if (cfg.VisualInputCubes.Count > 0) m_CurrVisualInputCubes = cfg.VisualInputCubes;
            GenerateVisualInputCubes();
            SimulateVisualInputCubes(true);

            m_TestInputCubes = new();
            foreach (var test in cfg.TestInputCubes) m_TestInputCubes.Add(new List<string>(test.Values));

            AudioManager.Ins.SetBgm(cfg.BGM);

            OnLevelInitialized?.Invoke();
        }

        public OperationConfig GetOperationConfig(OperationType type) => type switch
        {
            OperationType.InBox => AvailableOperations.InBox,
            OperationType.OutBox => AvailableOperations.OutBox,
            OperationType.CopyTo => AvailableOperations.CopyTo,
            OperationType.CopyFrom => AvailableOperations.CopyFrom,
            OperationType.Arithmetic => AvailableOperations.Arithmetic,
            OperationType.Jump => AvailableOperations.Jump,
            OperationType.Label => AvailableOperations.Label,
            _ => null
        };

        private void GenerateVisualInputCubes()
        {
            if (m_ForcedVisualInputCubes) return;

            m_CurrVisualInputCubes = InputCubesGenerator.GenerateInputCubes(m_LevelId);
        }

        /// <summary>
        /// 如果Cfg指定VisualInputCubes，则仅通过force执行一次
        /// </summary>
        private void SimulateVisualInputCubes(bool force = false)
        {
            if (m_ForcedVisualInputCubes && !force) return;

            CodeSimulator sim = new();
            sim.Run(m_CorrectOperations, m_CurrVisualInputCubes, GameManager.Ins.Carpet.ExportValues());
            m_CurrExpectedOutputCubes = sim.Outputs;
        }
    }

    [Serializable]
    public class OperationConfig
    {
        public bool Enabled = true;
        public bool Addressabled = true;

        // 仅用于 Arithmetic
        public bool Arithmetic_Basic   = true;
        public bool Arithmetic_Unary   = true;
        public bool Arithmetic_Compare = true;

        // 仅用于 Jump
        public bool Jump_Always    = true;
        public bool Jump_IfZero    = true;
        public bool Jump_IfNegative = true;
    }

    [Serializable]
    public class OperationsConfig
    {
        public OperationConfig InBox = new();
        public OperationConfig OutBox = new();
        public OperationConfig CopyTo = new();
        public OperationConfig CopyFrom = new();
        public OperationConfig Arithmetic = new();
        public OperationConfig Jump = new();
        public OperationConfig Label = new();
    }
}