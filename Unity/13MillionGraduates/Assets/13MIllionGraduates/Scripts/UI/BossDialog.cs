using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace Game.UI
{
    public class BossDialog : MonoBehaviour, IPointerDownHandler, IResettable
    {
        /// <summary>
        /// 入场出场动画应该用Animator实现(如Modify_Index_Fram)
        /// 可是我一时糊涂用AnimatonCurve做了
        /// </summary>
        private enum State
        {
            Hidden,
            Revealing,
            Awaiting,
            Hiding,
        }

        [Header("References")]
        public Transform DialogSocket;
        public RectTransform DialogFrame;
        public TextMeshProUGUI DialogText;
        public RectTransform DialogArrow;
        private Image m_DialogFrameImage;
        public GameObject DialogMask;
        public GameObject AngryVFX;

        [Header("Setting")]
        public AnimationCurve LerpCurve;
        public Color TalkDialogFrameColor;
        public Color AngryDialogFrameColor;
        public Color TalkDialogTextColor;
        public Color AngryDialogTextColor;

        private List<string> m_Texts;

        private State m_State;

        private bool m_WaitForClick;
        private bool m_IsAngry;
        private bool m_WasHidden;
        private float m_DesiredRotationZ;
        private float m_LastRotationZ;
        private Vector2 m_DesiredSize;
        private Vector2 m_LastSize;
        private int m_CurrTalkDialogIndex;
        private float m_Progress;
        private int m_TotalCharCount;
        private float m_TypingTimer;

        private const float CHAR_INTERVAL = .035f;

        private void Awake()
        {
            m_DialogFrameImage = DialogFrame.GetComponent<Image>();

            GameManager.RegisterResettable(this);

            GameManager.Ins.Boss.OnTalking += OnTalk;
            GameManager.Ins.Boss.OnAngry += OnAngry;

            DialogSocket.gameObject.SetActive(false);
            DialogMask.SetActive(false);
            AngryVFX.SetActive(false);
            m_State = State.Hidden;
        }

        public void OnReset()
        {
            DialogSocket.gameObject.SetActive(false);
            DialogMask.SetActive(false);
            AngryVFX.SetActive(false);
            m_State = State.Hidden;
        }

        private void OnTalk(List<string> texts)
        {
            if (m_State != State.Hidden) return;

            m_Texts = texts;
            BeginDialog(false, true);
        }

        private void OnAngry(string text, bool needAdvance)
        {
            if (m_State != State.Hidden) return;

            m_Texts = new List<string> { text };
            BeginDialog(true, needAdvance);

            AngryVFX.SetActive(true);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !m_WaitForClick) return;

            switch (m_State)
            {
                case State.Revealing:
                    OnAwait();
                    break;

                case State.Awaiting:
                    AdvanceDialog();
                    break;
            }
        }

        private void BeginDialog(bool isAngry, bool waitForClick)
        {
            if (m_State != State.Hidden) return;

            m_WaitForClick = waitForClick;
            m_IsAngry = isAngry;
            m_WasHidden = true;
            m_Progress = 0;

            DialogSocket.gameObject.SetActive(true);
            DialogMask.SetActive(waitForClick);
            m_DialogFrameImage.color = isAngry ? AngryDialogFrameColor : TalkDialogFrameColor;
            DialogText.color = isAngry ? AngryDialogTextColor : TalkDialogTextColor;
            DialogArrow.gameObject.SetActive(false);

            DialogSocket.localScale = Vector3.one * .5f;

            m_DesiredRotationZ = Random.Range(-5f, 5f);
            m_LastRotationZ = m_DesiredRotationZ;
            DialogSocket.localRotation = Quaternion.Euler(0f, 0f, m_DesiredRotationZ);

            m_CurrTalkDialogIndex = 1;
            DialogText.text = m_Texts[0];
            DialogText.ForceMeshUpdate();
            m_TotalCharCount = DialogText.textInfo.characterCount;
            DialogText.maxVisibleCharacters = 0;
            m_TypingTimer = 0;

            m_DesiredSize = new Vector2(
                Mathf.Max(DialogText.preferredWidth + 175f, 400f),
                Mathf.Max(DialogText.preferredHeight + 110f, 225f)
                );
            m_LastSize = m_DesiredSize;
            SetDialogFrameSize(m_DesiredSize);

            // Lock arrow to frame bottom-right
            DialogArrow.anchorMin = new Vector2(1f, 0f);
            DialogArrow.anchorMax = new Vector2(1f, 0f);
            DialogArrow.pivot = new Vector2(1f, 0f);
            DialogArrow.anchoredPosition = new Vector2(10f, 10f);

            m_State = State.Revealing;
        }

        private void AdvanceDialog()
        {
            if (m_State != State.Awaiting) return;

            if (!m_WaitForClick || m_CurrTalkDialogIndex >= m_Texts.Count) 
            {
                HideDialog();
                return;
            }

            m_WasHidden = false;
            m_Progress = 0;

            m_LastRotationZ = m_DesiredRotationZ;
            m_DesiredRotationZ = Random.Range(-5f, 5f);

            DialogText.text = m_Texts[m_CurrTalkDialogIndex++];
            DialogText.ForceMeshUpdate();
            m_TotalCharCount = DialogText.textInfo.characterCount;
            DialogText.maxVisibleCharacters = 0;
            m_TypingTimer = 0;

            m_LastSize = m_DesiredSize;
            m_DesiredSize = new Vector2(
                Mathf.Max(DialogText.preferredWidth + 175f, 400f),
                Mathf.Max(DialogText.preferredHeight + 110f, 225f)
                );

            m_State = State.Revealing;
        }

        public void OnAwait()
        {
            DialogSocket.localScale = Vector3.one;
            DialogSocket.localRotation = Quaternion.Euler(0f, 0f, m_DesiredRotationZ);
            SetDialogFrameSize(m_DesiredSize);
            DialogArrow.gameObject.SetActive(!m_IsAngry);

            DialogText.maxVisibleCharacters = m_TotalCharCount;

            m_State = State.Awaiting;
        }

        private void HideDialog()
        {
            m_Progress = 0;
            m_State = State.Hiding;

            GameManager.Ins.Boss.Idle();
            AngryVFX.SetActive(false);
        }

        private void Update()
        {
            switch (m_State)
            {
                case State.Revealing:
                    m_Progress += Time.unscaledDeltaTime / .3f;
                    float t = LerpCurve.Evaluate(Mathf.Clamp01(m_Progress));
                    m_TypingTimer += Time.unscaledDeltaTime;

                    if (m_WasHidden) DialogSocket.localScale = Vector3.LerpUnclamped(Vector3.one * .5f, Vector3.one, t);
                    DialogSocket.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(m_LastRotationZ, m_DesiredRotationZ, t));
                    SetDialogFrameSize(Vector2.LerpUnclamped(m_LastSize, m_DesiredSize, t));

                    int tgtChars = Mathf.Min(Mathf.FloorToInt(m_TypingTimer / CHAR_INTERVAL), m_TotalCharCount);
                    if (tgtChars != DialogText.maxVisibleCharacters) DialogText.maxVisibleCharacters = tgtChars;

                    if (m_Progress >= 1 && DialogText.maxVisibleCharacters >= m_TotalCharCount) OnAwait();
                    break;

                case State.Hiding:
                    m_Progress += Time.unscaledDeltaTime / .3f;
                    t = LerpCurve.Evaluate(Mathf.Clamp01(m_Progress));

                    DialogSocket.localScale = Vector3.LerpUnclamped(Vector3.one, Vector3.one * .5f, t);

                    if (m_Progress >= .4f) 
                    {
                        DialogSocket.gameObject.SetActive(false);
                        DialogMask.SetActive(false);
                        m_State = State.Hidden;
                    }
                    break;
            }

            if (!m_IsAngry) 
            {
                float sin = Mathf.Sin(2 * Time.unscaledTime * Mathf.PI) * 5f;
                DialogArrow.anchoredPosition = new Vector2(-80f, 30f + sin);
            }
        }

        private void SetDialogFrameSize(Vector2 size)
        {
            DialogFrame.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            DialogFrame.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        }
    }
}