using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Game
{
    public class Boss : MonoBehaviour, IResettable
    {
        private enum BossState
        {
            Idle,
            Talking,
            Angry,
        }

        [Header("References")]
        public Animator Animator;

        private BossState m_State;

        private bool m_WaitForClick = false;

        public bool IsIdling => m_State == BossState.Idle;
        public bool WaitForClick => m_WaitForClick;

        public event UnityAction<List<string>> OnTalking;
        public event UnityAction<string, bool> OnAngry;

        private void Awake()
        {
            GameManager.RegisterResettable(this);
            GameManager.Ins.OnGameInitialized += OnGameInitialized;
        }

        private void OnGameInitialized()
        {
            if (SaveManager.IsPassed(LevelManager.Ins.LevelId)) return;

            Talk(LevelManager.Ins.LevelConfig.ActiveTalk);
        }

        public void OnReset()
        {
            Idle();
        }

        private void ChangeState(BossState newState)
        {
            if (m_State == newState) return;

            OnStateExit(m_State);
            m_State = newState;
            OnStateEnter(m_State);
        }

        private void OnStateEnter(BossState state)
        {
            switch (state)
            {
                case BossState.Talking:
                    Animator.SetBool("IsTalking", true);
                    break;
                case BossState.Angry:
                    Animator.SetBool("IsAngry", true);
                    break;
            }
        }

        private void OnStateExit(BossState state)
        {
            switch (state)
            {
                case BossState.Talking:
                    Animator.SetBool("IsTalking", false);
                    break;
                case BossState.Angry:
                    Animator.SetBool("IsAngry", false);
                    break;
            }
        }

        public void Idle()
        {
            m_WaitForClick = false;
            ChangeState(BossState.Idle);
        }

        public void Talk(List<string> texts)
        {
            if (texts == null || texts.Count == 0) return;

            ChangeState(BossState.Talking);
            OnTalking?.Invoke(texts);

            m_WaitForClick = true;
        }

        public void Angry(string text, bool waitForClick = false)
        {
            ChangeState(BossState.Angry);
            OnAngry?.Invoke(text, waitForClick);

            m_WaitForClick = waitForClick;
        }
    }
}