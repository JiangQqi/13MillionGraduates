using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Game
{
    public enum GameMode
    {
        Classic,
        Advanced,
    };

    public class GameManager : MonoBehaviour
    {
        public static GameManager Ins => _ins == null ? _ins = FindFirstObjectByType<GameManager>() : _ins;
        private static GameManager _ins;

        /// <summary>
        /// 经典模式使用Carpet
        /// 进阶模式使用AreaManager
        /// </summary>
        [Header("References")]
        public PlayerController Player;
        public Boss Boss;
        public Carpet Carpet;
        public AreaManager AreaManager;
        public InBoxConveyer InBoxConveyer;
        public OutBoxConveyer OutBoxConveyer;

        [Header("Assets")]
        public DataCube DataCubePrefab;
        public List<Sprite> DataCubeSpirtes = new List<Sprite>();
        public List<Color> DataCubeTextColors = new List<Color>();

        [Header("Debug")]
        public bool ShowStateDebug = false;

        private List<IResettable> m_Resettable = new List<IResettable>();

        private bool m_IsInitialized = false;
        public bool IsInitialized => m_IsInitialized;

        private float m_ExecutionSpeed = 1f;
        public float ExecutionSpeed
        {
            get => m_ExecutionSpeed;
            set
            {
                m_ExecutionSpeed = Mathf.Clamp(value, 1f, 8f);
                Time.timeScale = m_ExecutionSpeed;
            }
        }

        public Func<int> GetLineCount;

        public event UnityAction OnGameInitialized;
        public event UnityAction<int, int> OnGamePassed;

        public static void RegisterResettable(IResettable resettable) => Ins.m_Resettable.Add(resettable);
        public static void UnRegisterResettable(IResettable resettable) => Ins.m_Resettable.Remove(resettable);

        private void Awake()
        {
            Elevator.Ins.OnElevatorOpened += OnGameInitialize;
        }

        public void Restart()
        {
            foreach (var r in m_Resettable) r.OnReset();
        }

        private int lines;
        private int steps;
        public void Pass(int step)
        {
            int levelId = LevelManager.Ins.LevelId;
            lines = GetLineCount?.Invoke() ?? 0;
            steps = step;
            SaveManager.TrySaveBest(levelId, lines, steps);
        }

        private void OnGameInitialize()
        {
            m_IsInitialized = true;
            OnGameInitialized?.Invoke();
        }

        public void InvokeOnGamePassed() => OnGamePassed?.Invoke(lines, steps);

        private void OnDestroy()
        {
            if (Elevator.Ins != null) Elevator.Ins.OnElevatorOpened -= OnGameInitialize;
        }

        #region State Log
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void StateLog<T>(T state, string action) where T : StateBase
        {
            if (Ins != null && Ins.ShowStateDebug)
                Debug.Log($"{action}{state.GetType().Name}");
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void StateLog<T>(T state, string action, string detail) where T : StateBase
        {
            if (Ins != null && Ins.ShowStateDebug)
                Debug.Log($"{action}{state.GetType().Name}：{detail}");
        }
        #endregion
    }
}