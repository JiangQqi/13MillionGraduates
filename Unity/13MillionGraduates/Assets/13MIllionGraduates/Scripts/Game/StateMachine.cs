using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Game
{
    /// <summary>
    /// 当前采用: 单例复用 State 实例
    /// 应修改为: 每次 Execute 时 new 新实例
    /// 
    /// 由于时间紧迫，暂不修改
    /// </summary>
    public class StateMachine
    {
        protected StateBase m_CurrentState;
        protected Dictionary<StateType, StateBase> m_States = new Dictionary<StateType, StateBase>();
        protected Dictionary<StateBase, StateType> m_ReverseLookup = new Dictionary<StateBase, StateType>();

        public StateBase CurrState => m_CurrentState;

        public void AddState(StateType type, StateBase state)
        {
            m_States[type] = state;
            m_ReverseLookup[state] = type;
        }

        public virtual void ChangeState(StateType type)
        {
            if (m_States.TryGetValue(type, out StateBase newState)) 
            {
                m_CurrentState = newState;
                m_CurrentState.Enter();
            }
            else
            {
                Debug.LogWarning("未找到 " + type + " 状态");
            }
        }

        public void Update()
        {
            if (m_CurrentState == null) return;

            m_CurrentState.Update();
            StateMachine subMachine = m_CurrentState.GetSubStateMachine();
            subMachine?.Update();
        }

        public StateType GetCurrStateType()
        {
            if (m_CurrentState == null) return StateType.None;

            return m_ReverseLookup.TryGetValue(m_CurrentState, out var type) 
                ? type 
                : StateType.None;
        }

        public StateBase GetStateByType(StateType type)
        {
            if (m_States.TryGetValue(type, out StateBase state))
            {
                return state;
            }
            else
            {
                Debug.LogError($"[StateMachine] 未找到 {type} 的StateBase，请先调用AddState");
                return null;
            }
        }
    }

    public class StateQueue : StateMachine
    {
        //初步认为状态队列机不适宜嵌套子状态队列机
        private Queue<StateBase> m_StateQueue = new Queue<StateBase>();

        public event UnityAction OnQueueCompleted;

        public override void ChangeState(StateType type)
        {
            if (m_CurrentState != null) m_CurrentState.OnCompleted -= Advance;

            base.ChangeState(type);

            if (m_CurrentState != null) m_CurrentState.OnCompleted += Advance;
        }

        public void Enqueue(StateType type)
        {
            if (m_States.TryGetValue(type, out StateBase newState))
            {
                m_StateQueue.Enqueue(newState);

                if (m_CurrentState == null)
                {
                    ChangeState(type);
                }
            }
            else
            {
                Debug.LogWarning("未找到 " + type + " 状态");
            }
        }

        private void Advance()
        {
            m_CurrentState.OnCompleted -= Advance;
            m_StateQueue.Dequeue();

            if (m_StateQueue.Count > 0)
            {
                m_CurrentState = m_StateQueue.Peek();
                m_CurrentState.Enter();

                m_CurrentState.OnCompleted += Advance;
            }
            else
            {
                m_CurrentState = null;
                OnQueueCompleted?.Invoke();
            }
        }

        public void Clear()
        {
            if (m_CurrentState != null) m_CurrentState.OnCompleted -= Advance;
            m_CurrentState = null;

            m_StateQueue.Clear();
        }
    }
}