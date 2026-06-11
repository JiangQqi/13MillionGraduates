using UnityEngine;
using UnityEngine.Events;

namespace Game
{
    public class DropDataCubeState : StateBase
    {
        public DropDataCubeState(PlayerController player)
        {
            m_Player = player;

            m_Player.OnDropDataCubeCompleted += Complete;
        }

        public override void Enter()
        {
            if (m_Player.HoldingDataCube == null)
            {
                CodeExecutor.Ins.HandleExecutionError("你打算扔掉空气吗！");
                Complete();
                return;
            }

            m_Player.DropDataCube();

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