using UnityEngine;

namespace Game
{
    public class ArithmeticState : StateBase
    {
        private Carpet m_Carpet;
        private AreaManager m_AreaManager;
        private int m_AreaId;
        private int m_Index;
        private ArithmeticType m_ArithmeticType;
        private bool m_IsAddress;

        private StateQueue m_ArithmeticQueue;

        public ArithmeticState(PlayerController player)
        {
            m_Player = player;

            m_ArithmeticQueue = new StateQueue();
            m_ArithmeticQueue.AddState(StateType.Walk, new WalkState(player));
            m_ArithmeticQueue.AddState(StateType.Arithmetic_Arithmetic, new Arithmetic_Arithmetic(player));
            m_ArithmeticQueue.OnQueueCompleted += Complete;
        }

        public ArithmeticState(PlayerController player, Carpet carpet) : this(player)
        {
            m_Carpet = carpet;
        }

        public ArithmeticState(PlayerController player, AreaManager area) : this(player)
        {
            m_AreaManager = area;
        }

        public void SetDestinationDataCubeAndType(int areaId, int index, ArithmeticType type, bool isAddress)
        {
            m_AreaId = areaId;
            m_Index = index;
            m_ArithmeticType = type;
            m_IsAddress = isAddress;
        }

        public override void Enter()
        {
            if (m_Player.HoldingDataCube == null && !Arithmetic.IsUnaryType(m_ArithmeticType))  
            {
                CodeExecutor.Ins.HandleExecutionError($"空值！\n你不能两手空空的去执行\nARITHMETIC {m_ArithmeticType.ToString().ToUpper()} ！");
                Complete();
                return;
            }

            if (!TryResolveAddress(m_Index, out int actualIndex))
            {
                Complete();
                return;
            }

            DataCube targetDataCube = null;
            switch (LevelManager.Ins.GameMode)
            {
                case GameMode.Classic:
                    targetDataCube = m_Carpet.GetDataCube(actualIndex);
                    break;

                case GameMode.Advanced:
                    break;
            }
            if (targetDataCube == null)
            {
                CodeExecutor.Ins.HandleExecutionError($"空值！\n你不能对地毯上的一块空格使用\nARITHMETIC {m_ArithmeticType.ToString().ToUpper()} 命令！\n你必须先在上面写点什么。");
                Complete();
                return;
            }
            targetDataCube.SetRendererSortingOrder(6);

            GameManager.StateLog(this, "进入");

            WalkState walk = m_ArithmeticQueue.GetStateByType(StateType.Walk) as WalkState;
            Arithmetic_Arithmetic arithmetic = m_ArithmeticQueue.GetStateByType(StateType.Arithmetic_Arithmetic) as Arithmetic_Arithmetic;
            switch (LevelManager.Ins.GameMode)
            {
                case GameMode.Classic:
                    walk.SetDestination(m_Carpet.GetPlayerDestination(actualIndex));
                    break;

                case GameMode.Advanced:
                    walk.SetDestination(Vector3.zero);
                    break;
            }
            arithmetic.SetDestinationDataCubeAndType(m_AreaId, actualIndex, m_ArithmeticType);

            m_ArithmeticQueue.Clear();
            m_ArithmeticQueue.Enqueue(StateType.Walk);
            m_ArithmeticQueue.Enqueue(StateType.Arithmetic_Arithmetic);
        }

        public override void Update()
        {
        }

        public override void Completed()
        {
            GameManager.StateLog(this, "退出");
        }

        private bool TryResolveAddress(int index, out int actualIndex)
        {
            actualIndex = index;
            if (!m_IsAddress) return true;

            if (LevelManager.Ins.GameMode == GameMode.Classic)
            {
                DataCube addrCube = m_Carpet.GetDataCube(index);
                if (addrCube == null || Arithmetic.GetTypeAndValue(addrCube.Value, out object parsed) == ValueType.None) 
                {
                    CodeExecutor.Ins.HandleExecutionError("地址无效！没人知道\n这是哪个格子！\n你究竟打算去哪？");
                    return false;
                }
                actualIndex = Arithmetic.ToInt(parsed);
                int carpetSize = m_Carpet.Row * m_Carpet.Col;
                if (actualIndex < 0 || actualIndex >= carpetSize)
                {
                    CodeExecutor.Ins.HandleExecutionError($"地址无效！找不到第\n{actualIndex}号格子！\n你究竟打算去哪？");
                    return false;
                }
            }
            return true;
        }

        public override StateMachine GetSubStateMachine() => m_ArithmeticQueue;
    }

    public class Arithmetic_Arithmetic : StateBase
    {
        private int m_AreaId;
        private int m_Index;
        private ArithmeticType m_ArithmeticType;
        private bool m_IsAddress;

        public Arithmetic_Arithmetic(PlayerController player)
        {
            m_Player = player;
            m_Player.OnArithmeticCompleted += Complete;
        }

        public void SetDestinationDataCubeAndType(int areaId, int index, ArithmeticType type)
        {
            m_AreaId = areaId;
            m_Index = index;
            m_ArithmeticType = type;
        }

        public override void Enter()
        {
            m_Player.Arithmetic_Arithmetic(m_AreaId, m_Index, m_ArithmeticType);

            GameManager.StateLog(this, "进入", $"目标 AreaId: {m_AreaId} , Index: {m_Index}, 模式 {m_ArithmeticType}");
        }

        public override void Update()
        {
        }

        public override void Completed()
        {
            GameManager.StateLog(this, "退出");
        }
    }
}