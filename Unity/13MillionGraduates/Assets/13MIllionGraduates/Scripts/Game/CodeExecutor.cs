using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Linq;

namespace Game
{
    /// <summary>
    /// 目前暂未实现Step功能
    /// StepPrev功能复杂(需记录Player,InboxConveyer,OutboxConveyer,Carpet状态)，时间紧迫暂不实现
    /// </summary>
    public class CodeExecutor : MonoBehaviour, IResettable
    {
        private enum ExecutionState
        {
            Stopped,
            Running,
            Paused,
            Passed,
            Errored,
        }

        public static CodeExecutor Ins => _ins == null ? _ins = FindFirstObjectByType<CodeExecutor>() : _ins;
        private static CodeExecutor _ins;

        [Header("Preferences")]
        public PlayerController Player;

        private ExecutionState m_State = ExecutionState.Stopped;
        private List<IOperation> m_Operations;
        private Dictionary<LabelOperation, int> m_LabelIndex;
        private int m_CurrOperation;

        public bool IsStopped => m_State == ExecutionState.Stopped;
        public bool IsRunning => m_State == ExecutionState.Running;
        public bool IsPaused => m_State == ExecutionState.Paused;
        public bool IsPassed => m_State == ExecutionState.Passed;
        public bool IsErrored => m_State == ExecutionState.Errored;

        public List<IOperation> Operations
        {
            get => m_Operations;
            set
            {
                m_Operations = value;

                m_LabelIndex = new Dictionary<LabelOperation, int>();
                if (m_Operations == null) return;
                for (int i = 0; i < m_Operations.Count; i++)
                    if (m_Operations[i] is LabelOperation label)
                        m_LabelIndex[label] = i;
            }
        }

        public string State => m_State.ToString();

        public event UnityAction OnStateChanged;
        public event UnityAction<int> OnOperationChanged;

        private void Awake()
        {
            GameManager.RegisterResettable(this);
        }

        public void OnReset()
        {
            ChangeState(ExecutionState.Stopped);
        }

        public void StartExecution()
        {
            ChangeState(ExecutionState.Running);

            m_CurrOperation = -1;

            Advance();
        }

        private void ChangeState(ExecutionState newState)
        {
            m_State = newState;
            OnStateChanged?.Invoke();
        }

        internal void Advance()
        {
            Player.StateMachine.CurrState.OnCompleted -= Advance;

            if (!IsRunning) return;

            m_CurrOperation++;
            OnOperationChanged?.Invoke(m_CurrOperation);
            if (m_CurrOperation >= m_Operations.Count)
            {
                EndCheck();
                return;
            }

            var op = m_Operations[m_CurrOperation];
            switch(op.OperationType)
            {
                case OperationType.Jump:
                    JumpOperation jump = op as JumpOperation;

                    if (jump.Param2 != JumpType.Any && Player.HoldingDataCube == null) 
                    {
                        HandleExecutionError($"空值！\n你不能两手空空的去执行\nJUMP {jump.Param2.ToString().ToUpper()} !");
                        return;
                    }

                    bool shouldJump = jump.Param2 switch
                    {
                        JumpType.Any => true,
                        JumpType.IfZero => Arithmetic.EvaluateZero(Player.HoldingDataCube.Value),
                        JumpType.IfNegative => Arithmetic.EvaluateNegative(Player.HoldingDataCube.Value),
                        _ => false,
                    };
                    if (shouldJump)
                    {
                        if (!m_LabelIndex.TryGetValue(jump.Param1, out int targetIndex))
                        {
                            HandleExecutionError("Jump 目标不存在!");
                            return;
                        }
                        m_CurrOperation = targetIndex - 1;
                    }

                    Advance();
                    break;

                case OperationType.Label:
                    Advance();
                    break;

                default:
                    op.Execute(Player);
                    Player.StateMachine.CurrState.OnCompleted += Advance;
                    break;
            }
        }

        /// <summary>
        /// 可能由于Player尝试Inbox但InboxConveyer为空时而提前EndedCheck
        /// </summary>
        public void EndCheck()
        {
            if (GameManager.Ins.OutBoxConveyer.OutputCount < LevelManager.Ins.CurrExpectedOutputCount)
            {
                HandleExecutionError($"OUTBOX(输出栏)里的东西，还不够多！\n公司高层想要 {LevelManager.Ins.CurrExpectedOutputCount} 个，而不是 {GameManager.Ins.OutBoxConveyer.OutputCount} 个！");
                return;
            }

            var playerSim = new CodeSimulator();
            var correctSim = new CodeSimulator();
            var correctOps = LevelManager.Ins.CorrectOperations;
            var carpet = LevelManager.Ins.InitialCarpetValues;
            foreach (var test in LevelManager.Ins.TestInputCubes) 
            {
                playerSim.Run(m_Operations, test, carpet);
                correctSim.Run(correctOps, test, carpet);
                if (!playerSim.Success || !playerSim.Outputs.SequenceEqual(correctSim.Outputs)) 
                {
                    HandleExecutionError("确实，\n你的代码能够应付这些INBOX(输入栏)里的数据......\n但它并不能应付所有的情况！");
                    return;
                }
            }

            GameManager.Ins.Pass(playerSim.TotalSteps / LevelManager.Ins.TestInputCubes.Count);
            ChangeState(ExecutionState.Passed);
        }

        public void HandleExecutionError(string ErrorMessage)
        {
            ChangeState(ExecutionState.Errored);

            GameManager.Ins.Boss.Angry(ErrorMessage);

            Debug.LogError(ErrorMessage);
        }
    }
}