using UnityEngine;

namespace Game
{
    public class CopyFromState : StateBase
    {
        private Carpet m_Carpet;
        private AreaManager m_AreaManager;
        private int m_AreaId;
        private int m_Index;
        private bool m_IsAddress;

        private StateQueue m_CopyFromQueue;

        public CopyFromState(PlayerController player)
        {
            m_Player = player;

            m_CopyFromQueue = new StateQueue();
            m_CopyFromQueue.AddState(StateType.Walk, new WalkState(player));
            m_CopyFromQueue.AddState(StateType.CopyFrom_CopyFrom, new CopyFrom_CopyFrom(player));
            m_CopyFromQueue.OnQueueCompleted += Complete;
        }

        public CopyFromState(PlayerController player, Carpet carpet) : this(player)
        {
            m_Carpet = carpet;
        }

        public CopyFromState(PlayerController player, AreaManager area) : this(player)
        {
            m_AreaManager = area;
        }

        public void SetDestinationDataCube(int areaId, int index, bool isAddress)
        {
            m_AreaId = areaId;
            m_Index = index;
            m_IsAddress = isAddress;
        }

        public override void Enter()
        {
            if (!TryResolveAddress(m_Index, out int actualIndex))
            {
                Complete();
                return;
            }

            DataCube fromDataCube = null;
            switch (LevelManager.Ins.GameMode)
            {
                case GameMode.Classic:
                    fromDataCube = m_Carpet.GetDataCube(actualIndex);
                    break;

                case GameMode.Advanced:
                    break;
            }
            if (fromDataCube == null)
            {
                CodeExecutor.Ins.HandleExecutionError("空值！\n你不能对地毯上的一块空格使用\nCOPYFROM 命令！\n你必须先在上面写点什么。");
                Complete();
                return;
            }
            fromDataCube.SetRendererSortingOrder(6);

            GameManager.StateLog(this, "进入");

            WalkState walk = m_CopyFromQueue.GetStateByType(StateType.Walk) as WalkState;
            CopyFrom_CopyFrom copyFrom = m_CopyFromQueue.GetStateByType(StateType.CopyFrom_CopyFrom) as CopyFrom_CopyFrom;
            switch (LevelManager.Ins.GameMode)
            {
                case GameMode.Classic:
                    walk.SetDestination(m_Carpet.GetPlayerDestination(actualIndex));
                    break;

                case GameMode.Advanced:
                    walk.SetDestination(Vector3.zero);
                    break;
            }
            copyFrom.SetDestination(m_AreaId, actualIndex);

            m_CopyFromQueue.Clear();
            m_CopyFromQueue.Enqueue(StateType.Walk);
            m_CopyFromQueue.Enqueue(StateType.CopyFrom_CopyFrom);
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

        public override StateMachine GetSubStateMachine() => m_CopyFromQueue;
    }

    public class CopyFrom_CopyFrom : StateBase
    {
        private int m_AreaId;
        private int m_Index;

        public CopyFrom_CopyFrom(PlayerController player)
        {
            m_Player = player;
            m_Player.OnCopyFromCompleted += Complete;
        }

        public void SetDestination(int areaId, int index)
        {
            m_AreaId = areaId;
            m_Index = index;
        }

        public override void Enter()
        {
            m_Player.CopyFrom_CopyFrom(m_AreaId, m_Index);

            GameManager.StateLog(this, "进入", $"目标 {m_AreaId} + {m_Index}");
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