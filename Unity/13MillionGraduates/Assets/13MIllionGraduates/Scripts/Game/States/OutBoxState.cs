using UnityEngine;

namespace Game
{
    public class OutBoxState : StateBase
    {
        private OutBoxConveyer m_OutBoxConveyer;
        private StateQueue m_OutBoxStateQueue;

        public OutBoxState(PlayerController player, OutBoxConveyer conveyer)
        {
            m_Player = player;
            m_OutBoxConveyer = conveyer;
            m_OutBoxStateQueue = new StateQueue();

            m_OutBoxStateQueue.AddState(StateType.Walk, new WalkState(m_Player, m_OutBoxConveyer.OutBoxTransform.position));
            m_OutBoxStateQueue.AddState(StateType.OutBox_DropDataCube, new OutBox_DropDataCube(m_Player, conveyer));

            m_OutBoxStateQueue.OnQueueCompleted += Complete;
        }

        public override void Enter()
        {
            if (m_Player.HoldingDataCube == null)
            {
                CodeExecutor.Ins.HandleExecutionError("空值！\n你不能两手空空的去执行\nOUTBOX！");
                Complete();
                return;
            }

            GameManager.StateLog(this, "进入");

            m_OutBoxStateQueue.Clear();
            m_OutBoxStateQueue.Enqueue(StateType.Walk);
            m_OutBoxStateQueue.Enqueue(StateType.OutBox_DropDataCube);
        }

        public override void Update()
        {
        }

        public override void Completed()
        {
            GameManager.StateLog(this, "退出");
        }

        public override StateMachine GetSubStateMachine() => m_OutBoxStateQueue;
    }

    public class OutBox_DropDataCube : StateBase
    {
        private OutBoxConveyer m_OutBoxConveyer;

        public OutBox_DropDataCube(PlayerController player, OutBoxConveyer conveyer)
        {
            m_Player = player;
            m_OutBoxConveyer = conveyer;

            m_Player.OnOutBoxCompleted += Complete;
        }

        public override void Enter()
        {
            m_Player.OutBox_DropDataCube();
            m_OutBoxConveyer.DriveConveyer(1);

            GameManager.StateLog(this, "进入");
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