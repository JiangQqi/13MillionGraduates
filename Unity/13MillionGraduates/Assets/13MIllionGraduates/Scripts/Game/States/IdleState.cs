using UnityEngine;

namespace Game
{
    public class IdleState : StateBase
    {
        public IdleState(PlayerController player)
        {
            m_Player = player;
        }

        public override void Enter()
        {
            GameManager.StateLog(this, "½øÈë");
        }

        public override void Update()
        {

        }

        public override void Completed()
        {
            GameManager.StateLog(this, "ÍË³ö");
        }
    }
}