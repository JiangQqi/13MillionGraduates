using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace Game.UI
{
    /// <summary>
    /// 入场出场动画应该用Animator实现(如Modify_Index_Fram)
    /// 可是我一时糊涂用AnimatonCurve做了
    /// </summary>
    public class PlayerDialog : MonoBehaviour, IPointerDownHandler
    {
        private enum State
        {
            Hidden,
            Revealing,
            Awaiting,
            Hiding,
        }

        [Header("References")]
        public Transform DialogSocket;
        public RectTransform[] Arrows = new RectTransform[4];
        public Transform SelectedBackground;

        [Header("More Talk Tip")]
        public RectTransform BossTipSocket;
        public RectTransform BossTipBang;
        public RectTransform PlayerTipBang;

        [Header("Setting")]
        public AnimationCurve LerpCurve;

        private State m_State;
        private float m_Progress;
        private float[] m_ArrowOffset = new float[4];

        private bool m_HasReadMoreTalk;
        private bool m_ShowTip;

        private void Awake()
        {
            LevelManager.Ins.OnLevelInitialized += () =>
            {
                m_HasReadMoreTalk = SaveManager.HasReadMoreTalk(LevelManager.Ins.LevelId);
                ShowTip(!m_HasReadMoreTalk);
            };
        }

        public void ShowDialog()
        {
            if (!CodeExecutor.Ins.IsStopped) return;

            DialogSocket.gameObject.SetActive(true);
            DialogSocket.localScale = Vector3.one * .5f;
            DialogSocket.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-5f, 5f));

            m_Progress = 0f;
            m_State = State.Revealing;

            for (int i = 0; i < 4; i++) m_ArrowOffset[i] = Random.Range(0f, Mathf.PI * 4f);
        }

        public void HideDialog()
        {
            if (m_State == State.Hiding) return;

            m_Progress = 0f;
            m_State = State.Hiding;
        }

        private void UpdateShowMoreTalkTip()
        {
            if (m_HasReadMoreTalk) return;

            bool show = GameManager.Ins.Boss.IsIdling && CodeExecutor.Ins.IsStopped;
            if (m_ShowTip == show) return;

            m_ShowTip = show;
            ShowTip(show);
        }

        private void ShowTip(bool show)
        {
            BossTipSocket.gameObject.SetActive(show);
            PlayerTipBang.gameObject.SetActive(show);
            Arrows[1].gameObject.SetActive(!show);
        }

        private void Update()
        {
            UpdateShowMoreTalkTip();

            m_Progress += Time.unscaledDeltaTime / .3f;
            float t = LerpCurve.Evaluate(Mathf.Clamp01(m_Progress));

            switch (m_State)
            {
                case State.Revealing:
                    DialogSocket.localScale = Vector3.LerpUnclamped(Vector3.one * .5f, Vector3.one, t);

                    if (m_Progress >= 1) m_State = State.Awaiting;
                    break;

                case State.Hiding:
                    DialogSocket.localScale = Vector3.LerpUnclamped(Vector3.one, Vector3.one * .5f, t);

                    if (m_Progress >= .4f)
                    {
                        DialogSocket.gameObject.SetActive(false);
                        m_State = State.Hidden;
                    }
                    break;
            }

            if (m_State != State.Hidden)
            {
                for (int i = 0; i < 4; i++) 
                {
                    float sin = Mathf.Sin(4 * Time.unscaledTime * Mathf.PI + m_ArrowOffset[i]) * 6f;
                    Arrows[i].anchoredPosition = new Vector3(-25f + sin, 0, 0);
                }
            }

            if (m_ShowTip)
            {
                float sin1 = Mathf.Sin(Time.unscaledTime * Mathf.PI);
                float sin2 = Mathf.Sin(2 * Time.unscaledTime * Mathf.PI);
                float sin4 = Mathf.Sin(4 * Time.unscaledTime * Mathf.PI);
                BossTipSocket.localRotation = Quaternion.Euler(0, 0, sin1 * 5f - 10f);
                BossTipBang.localScale = Vector3.one * (sin2 * .1f + 1f);
                BossTipBang.localRotation = Quaternion.Euler(0, 0, sin1 * 5f);
                PlayerTipBang.localRotation = Quaternion.Euler(0, 0, sin4 * 10f);
            }
        }

        public void OnPointerEnter(int idx)
        {
            SelectedBackground.gameObject.SetActive(true);
            SelectedBackground.localPosition = new Vector3(108f, -54.5f - idx * 77f, 0f);
        }

        public void OnPointerExit()
        {
            SelectedBackground.gameObject.SetActive(false);
        }

        public void OnTalkButtonDown(int idx)
        {
            List<string> talk = new();
            switch (idx)
            {
                case 0: 
                    talk = LevelManager.Ins.LevelConfig.ActiveTalk; 
                    break;
                case 1:
                    SaveManager.SaveHasReadMoreTalk(LevelManager.Ins.LevelId, true);
                    m_HasReadMoreTalk = true;
                    ShowTip(false);

                    talk = LevelManager.Ins.LevelConfig.MoreTalk; 
                    break;
                case 2:
                    talk = LevelManager.Ins.LevelConfig.ExampleTalk; 
                    break;
                case 3: 
                    talk = LevelManager.Ins.LevelConfig.OptimalTalk; 
                    break;
            }
            GameManager.Ins.Boss.Talk(talk);

            HideDialog();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            HideDialog();
        }
    }
}