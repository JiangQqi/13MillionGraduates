using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.UI
{
    public class NotePad : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public static NotePad Ins => _ins == null ? _ins = FindFirstObjectByType<NotePad>() : _ins;
        private static NotePad _ins;

        [Header("Main Panel")]
        public RectTransform MainPanel;
        private float PanelHeight
        {
            get => MainPanel.rect.height;
            set => MainPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, value);
        }
        private float m_TargetPanelHeight;
        private Vector3 m_StartPos;
        private float m_OffsetY;
        private bool m_IsDragging;
        private float m_DragStartMouseYAddOffsetY;
        private bool m_IsHovering;
        private bool m_InEntranced = false;

        /// <summary>
        /// 当玩家ClearPage或PastePage时PanelHeight的修改速度应该很快
        /// </summary>
        private bool m_ResizeFast;

        [Header("Title")]
        public TextMeshProUGUI TitleText;

        [Header("Description")]
        public RectTransform DescriptionFrame;
        public TextMeshProUGUI DescriptionText;

        [Header("Page")]
        public Image[] Pages;

        [Header("Code")]
        public CodeManager CodeManager;

        [Header("Operation")]
        public List<OperationItem> OperationPrefabs = new List<OperationItem>();
        public RectTransform OperationFrame;
        public Transform OperationSocket;
        private Vector3 m_OpeartionStartPos;

        [Header("Prefabs")]
        public List<CodeItem> CodeItemPrefabs = new List<CodeItem>();
        public List<Sprite> ArithmeticSprites = new List<Sprite>();
        public Sprite PageSelected;
        public Sprite PageUnSelected;
        public AnimationCurve LerpCurve;

        private void Awake()
        {
            transform.localPosition = new Vector3(2000f, 600f, 0f);

            m_StartPos = MainPanel.localPosition;
            m_OffsetY = 0;

            m_OpeartionStartPos = OperationFrame.localPosition;

            LevelManager.Ins.OnLevelInitialized += OnLevelInitialized;
            GameManager.Ins.OnGameInitialized += () => StartCoroutine(Entrance());
        }

        private void OnLevelInitialized()
        {
            InitPage();

            RefreshOperations();

            TitleText.text = LevelManager.Ins.LevelConfig.LevelTitle;
            DescriptionText.text = LevelManager.Ins.LevelConfig.LevelDescription;

            DescriptionText.ForceMeshUpdate();
            float descHeight = DescriptionText.preferredHeight;

            DescriptionFrame.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, descHeight + 51f);

            RectTransform codeField = CodeManager.GetComponent<RectTransform>();
            codeField.offsetMax = new Vector2(codeField.offsetMax.x, -descHeight - 200f);
        }

        /// <summary>
        /// 入场动画
        /// </summary>
        private IEnumerator Entrance()
        {
            float elasped = 0f;
            Vector3 start = new Vector3(2000f, 600f, 0f);
            Vector3 end = new Vector3(1250f, 600f, 0f);
            while (elasped <= .4f)
            {
                elasped += Time.unscaledDeltaTime;
                float t = LerpCurve.Evaluate(elasped / .4f);
                transform.localPosition = Vector3.LerpUnclamped(start, end, t);
                yield return null;
            }

            transform.localPosition = end;
            m_InEntranced = true;
        }

        private void Update()
        {
            UpdateMainPanelHeight();
            UpdateMainPanelPosition();

            bool hideOperations = CodeManager.IsModifying || !CodeExecutor.Ins.IsStopped || !m_InEntranced;
            Lerp(OperationFrame, m_OpeartionStartPos + Vector3.right * (hideOperations ? 300f : 0f));
        }

        public void ResizeFast() => m_ResizeFast = true;

        private void UpdateMainPanelHeight()
        {
            m_TargetPanelHeight = 146f + DescriptionFrame.rect.height + CodeManager.Height;
            if (CodeManager.IsInserting) m_TargetPanelHeight += 75f;
            m_TargetPanelHeight = Mathf.Max(m_TargetPanelHeight, 1200f);
            if (CodeManager.IsModifying) m_TargetPanelHeight += 50f;

            if (PanelHeight != m_TargetPanelHeight)
            {
                PanelHeight = Mathf.MoveTowards(PanelHeight, m_TargetPanelHeight, m_ResizeFast ? 3000f : 600f * Time.unscaledDeltaTime);
            }
            else m_ResizeFast = false;
        }

        private void UpdateMainPanelPosition()
        {
            if (!m_InEntranced) return;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            m_IsHovering = mousePos.x / Screen.width >= .754f;
            if (m_IsDragging)
            {
                float lowestY = 1200f - PanelHeight;
                m_OffsetY = m_DragStartMouseYAddOffsetY - mousePos.y;
                if (m_OffsetY < lowestY)
                {
                    float overflow = lowestY - m_OffsetY;
                    m_OffsetY = lowestY - Mathf.Sqrt(overflow) * 5f;
                }
                m_OffsetY = Mathf.Min(m_OffsetY, 60f);
                MainPanel.localPosition = m_StartPos + m_OffsetY * Vector3.down;
            }
            else
            {
                if (m_IsHovering && !CodeManager.IsModifying && !CodeManager.IsExitingModifying && !CodeExecutor.Ins.IsRunning) m_OffsetY += Mouse.current.scroll.ReadValue().y * 200f;

                if (CodeManager.IsInserting)
                {
                    float y = Mathf.Clamp01(mousePos.y / Screen.height);
                    if (y >= .9f) m_OffsetY += (y - .9f) * 100f;
                    else if (y <= .1f) m_OffsetY -= (.1f - y) * 100f;
                }

                m_OffsetY = Mathf.Clamp(m_OffsetY, 1200f - PanelHeight, 0f);
                Lerp(MainPanel, m_StartPos + (m_OffsetY - (CodeManager.IsModifying ? 25f : 0f)) * Vector3.down);
            }
        }

        private void Lerp(Transform transform, Vector3 destination) => transform.localPosition = Vector3.Lerp(transform.localPosition, destination, 10f * Time.unscaledDeltaTime);

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (CodeExecutor.Ins.IsRunning || CodeExecutor.Ins.IsPassed) return;
            m_IsDragging = true;
            m_DragStartMouseYAddOffsetY = Mouse.current.position.ReadValue().y + m_OffsetY;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            m_IsDragging = false;
        }

        public void OnStartInsert()
        {
            m_TargetPanelHeight = 146f + DescriptionFrame.rect.height + CodeManager.Height + 75f;
            m_TargetPanelHeight = Mathf.Max(m_TargetPanelHeight, 1200f);
        }

        public void RefreshOperations()
        {
            for (int i = OperationSocket.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying) Destroy(OperationSocket.GetChild(i).gameObject);
                else DestroyImmediate(OperationSocket.GetChild(i).gameObject);
            }

            int availableCount = 0;
            foreach (OperationType type in System.Enum.GetValues(typeof(OperationType)))
            {
                if (type == OperationType.Label) continue;

                OperationConfig cfg = LevelManager.Ins.GetOperationConfig(type);
                if (!cfg.Enabled) continue;

                Instantiate(OperationPrefabs[(int)type], OperationSocket);
                availableCount++;
            }

            float height = Mathf.Max(81f * availableCount + 45f, 207f);
            OperationFrame.sizeDelta = new Vector2(276f, height);
        }

        public void ScrollToCodeItem(float itemY)
        {
            if (itemY >= 70f && itemY <= 1370f)
                return;

            float targetY = Mathf.Clamp(itemY, 70f, 1370f);
            float neededMove = targetY - itemY;

            m_OffsetY -= neededMove;
            m_OffsetY = Mathf.Clamp(m_OffsetY, 1200f - PanelHeight, 0f);
        }

        #region Page
        public void InitPage()
        {
            foreach (var page in Pages)
            {
                page.sprite = PageUnSelected;
                page.SetNativeSize();
            }

            CodeManager.InitPage();
            Pages[CodeManager.CurrPageIndex].sprite = PageSelected;
            Pages[CodeManager.CurrPageIndex].SetNativeSize();
        }

        public void LoadPage(int idx)
        {
            if (idx < 0 || idx > 2 || !CodeExecutor.Ins.IsStopped) return;

            Pages[CodeManager.CurrPageIndex].sprite = PageUnSelected;
            Pages[CodeManager.CurrPageIndex].SetNativeSize();
            CodeManager.LoadPage(idx);
            Pages[CodeManager.CurrPageIndex].sprite = PageSelected;
            Pages[CodeManager.CurrPageIndex].SetNativeSize();
        }
        #endregion
    }
}