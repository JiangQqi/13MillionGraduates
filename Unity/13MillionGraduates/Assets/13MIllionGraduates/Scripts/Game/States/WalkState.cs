using UnityEngine;

namespace Game
{
    public class WalkState : StateBase
    {
        private Vector3 m_Destination;

        public WalkState(PlayerController player)
        {
            m_Player = player;
        }

        public WalkState(PlayerController player, Vector3 destination) : this(player)
        {
            m_Destination = destination;
        }

        public void SetDestination(Vector3 destination)
        {
            m_Destination = destination;
        }

        public override void Enter()
        {
            m_Player.SetDestination(m_Destination);

            GameManager.StateLog(this, "进入");
        }


        public override void Update()
        {
            if (m_Player.HasReachedDestination())
            {
                Complete();
            }
        }

        public override void Completed()
        {
            GameManager.StateLog(this, "退出");
        }
    }
}