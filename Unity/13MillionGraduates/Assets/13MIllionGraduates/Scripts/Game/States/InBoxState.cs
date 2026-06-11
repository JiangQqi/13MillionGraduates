using UnityEngine;

namespace Game
{
    // State - InBox
    public class InBoxState : StateBase
    {
        private InBoxConveyer m_InBoxConveyer;
        private StateQueue m_InBoxStateQueue;

        public InBoxState(PlayerController player, InBoxConveyer inBoxConveyer)
        {
            m_Player = player;
            m_InBoxConveyer = inBoxConveyer;
            m_InBoxStateQueue = new StateQueue();

            m_InBoxStateQueue.AddState(StateType.DropDataCube, new DropDataCubeState(player));
            m_InBoxStateQueue.AddState(StateType.Walk, new WalkState(player, m_InBoxConveyer.InBoxTransform.position));
            m_InBoxStateQueue.AddState(StateType.InBox_GrabDataCube, new InBox_GrabDataCube(player, m_InBoxConveyer));

            m_InBoxStateQueue.OnQueueCompleted += Complete;
        }

        public override void Enter()
        {
            DataCube cube = m_InBoxConveyer.GetFirstDataCube();
            if (cube == null)
            {
                CodeExecutor.Ins.EndCheck();
                Complete();
                return;
            }
            cube.SetRendererSortingOrder(6);

            GameManager.StateLog(this, "进入");

            m_InBoxStateQueue.Clear();
            if (m_Player.HoldingDataCube != null)
            {
                m_InBoxStateQueue.Enqueue(StateType.DropDataCube);
            }
            m_InBoxStateQueue.Enqueue(StateType.Walk);
            m_InBoxStateQueue.Enqueue(StateType.InBox_GrabDataCube);
        }

        public override void Update()
        {
        }

        public override void Completed()
        {
            GameManager.StateLog(this, "退出");
        }

        public override StateMachine GetSubStateMachine() => m_InBoxStateQueue;
    }

    //State - InBox_GrabDataCube
    public class InBox_GrabDataCube : StateBase
    {
        private InBoxConveyer m_InBoxConveyer;

        private bool m_IsGrabing;

        public InBox_GrabDataCube(PlayerController player, InBoxConveyer conveyer)
        {
            m_Player = player;
            m_InBoxConveyer= conveyer;

            m_Player.OnInBoxCompleted += Complete;
        }

        public override void Enter()
        {
            m_IsGrabing = false;

            GameManager.StateLog(this, "进入");
        }

        public override void Update()
        {
            if (!m_IsGrabing && m_InBoxConveyer.IsDataCubeAvailable) 
            {
                m_Player.InBox_GrabDataCube();
                m_IsGrabing = true;
            }
        }

        public override void Completed()
        {
            GameManager.StateLog(this, "退出");
        }
    }
}