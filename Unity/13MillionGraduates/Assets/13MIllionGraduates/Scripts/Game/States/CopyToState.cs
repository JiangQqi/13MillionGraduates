using UnityEngine;

namespace Game
{
    public class CopyToState : StateBase
    {
        private Carpet m_Carpet;
        private AreaManager m_AreaManager;
        private int m_AreaId;
        private int m_Index;
        private bool m_IsAddress;

        private StateQueue m_CopyToQueue;

        public CopyToState(PlayerController player)
        {
            m_Player = player;

            m_CopyToQueue = new StateQueue();
            m_CopyToQueue.AddState(StateType.Walk, new WalkState(player));
            m_CopyToQueue.AddState(StateType.CopyTo_CopyTo, new CopyTo_CopyTo(player));
            m_CopyToQueue.OnQueueCompleted += Complete;
        }

        /// <summary>
        /// Classic调用此构造函数
        /// </summary>
        public CopyToState(PlayerController player, Carpet carpet) : this(player)
        {
            m_Carpet = carpet;
        }

        /// <summary>
        /// Advanced调用此构造函数
        /// </summary>
        public CopyToState(PlayerController player, AreaManager area) : this(player)
        {
            m_AreaManager = area;
        }

        public void SetDestination(int areaId, int index, bool isAddress)
        {
            m_AreaId = areaId;
            m_Index = index;
            m_IsAddress = isAddress;
        }

        public override void Enter()
        {
            if (m_Player.HoldingDataCube == null)
            {
                CodeExecutor.Ins.HandleExecutionError("空值！\n你不能两手空空的去执行\nCOPYTO！");
                Complete();
                return;
            }

            if (!TryResolveAddress(m_Index, out int actualIndex))
            {
                Complete();
                return;
            }

            DataCube cube = null;
            switch (LevelManager.Ins.GameMode)
            {
                case GameMode.Classic:
                    cube = m_Carpet.GetDataCube(actualIndex);
                    break;

                case GameMode.Advanced:
                    break;
            }
            cube?.SetRendererSortingOrder(6);

            GameManager.StateLog(this, "进入");

            WalkState walk = m_CopyToQueue.GetStateByType(StateType.Walk) as WalkState;
            CopyTo_CopyTo copyTo = m_CopyToQueue.GetStateByType(StateType.CopyTo_CopyTo) as CopyTo_CopyTo;
            switch (LevelManager.Ins.GameMode)
            {
                case GameMode.Classic:
                    walk.SetDestination(m_Carpet.GetPlayerDestination(actualIndex));
                    break;

                case GameMode.Advanced:
                    walk.SetDestination(Vector3.zero);
                    break;
            }
            copyTo.SetDestination(m_AreaId, actualIndex);

            m_CopyToQueue.Clear();
            m_CopyToQueue.Enqueue(StateType.Walk);
            m_CopyToQueue.Enqueue(StateType.CopyTo_CopyTo);
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

        public override void Update()
        {
        }

        public override void Completed()
        {
            GameManager.StateLog(this, "退出");
        }

        public override StateMachine GetSubStateMachine() => m_CopyToQueue;
    }

    public class CopyTo_CopyTo : StateBase
    {
        private int m_AreaId;
        private int m_Index;
        private bool m_IsAddress;

        public CopyTo_CopyTo(PlayerController player)
        {
            m_Player = player;
            m_Player.OnCopyToCompleted += Complete;
        }

        public void SetDestination(int areaId, int index)
        {
            m_AreaId = areaId;
            m_Index = index;
        }

        public override void Enter()
        {
            m_Player.CopyTo_CopyTo(m_AreaId, m_Index);

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