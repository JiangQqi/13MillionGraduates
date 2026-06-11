using UnityEngine;
using UnityEngine.Events;

namespace Game
{
    public abstract class StateBase
    {
        public abstract void Enter();
        public abstract void Update();
        public abstract void Completed();
        public virtual StateMachine GetSubStateMachine() => null;

        protected PlayerController m_Player;

        public event UnityAction OnCompleted;

        public void Complete()
        {
            Completed();
            OnCompleted?.Invoke();
        }
    }
}