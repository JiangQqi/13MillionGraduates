using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Game.UI
{
    public class CodeManager : MonoBehaviour
    {
        private enum CodeManagerState
        {
            Idle,
            Inserting,
            Modifying,
            ExitingModify,
        }

        [Header("Code Field")]
        public Transform CodeFrame;
        public Transform CodeLine;
        public Transform UpLeftTransform;
        public Transform CodeSocket;
        public Transform InsertingCodeSocket;

        [Header("Edit")]
        public TweenButton[] Edits;

        [Header("Jump Color")]
        public Color StartColor;
        public Color EndColor;
        private float m_CurrJumpColorFactor = .5f;

        [Header("Arrow")]
        public Transform Arrow;

        [Header("Prefab")]
        public TextMeshProUGUI LineNumPrefab;

        private CodeManagerState m_State;

        private List<CodeItem> m_Codes = new List<CodeItem>();
        private int m_PageIndex;
        private int m_LevelId;
        private List<Transform> m_LineNums = new List<Transform>();
        private List<Vector3> m_LineNumDestinations = new List<Vector3>();

        public IEnumerable<IOperation> Instructions => Codes.Select(item => item.Operation);

        public bool IsEmpty => Codes.Count <= 0;

        public bool IsInserting => m_State == CodeManagerState.Inserting;
        public bool IsModifying => m_State == CodeManagerState.Modifying;
        public bool IsExitingModifying => m_State == CodeManagerState.ExitingModify;
        private List<CodeItem> Codes => m_Codes;
        public int CurrPageIndex => m_PageIndex;
        private Vector3 UpLeft => UpLeftTransform.position;

        private CodeItem m_InsertingCodeItem;
        private CodeItem m_ModifyingCodeitem;

        private bool m_IsHovering = false;
        private bool m_IsLastHovering = false;
        private int m_InsertIndex;
        private int m_LastInsertIndex;
        private int m_ModifyingIndex;

        private JumpItem m_HighLightJumpItem;

        private float m_ArrowDesitinationY;

        public event UnityAction<CodeItem> OnStartModify;

        public float Height => Codes.Count * 75f + 250f;

        private void Awake()
        {
            m_PageIndex = 0;

            Edits[0].OnClick.AddListener(OnUndoButtonDown);
            Edits[1].OnClick.AddListener(OnCopyButtonDown);
            Edits[2].OnClick.AddListener(OnPasteButtonDown);
            Edits[3].OnClick.AddListener(OnClearButtonDown);

            GameManager.Ins.GetLineCount = GetPageLineCount;

            CodeExecutor.Ins.OnStateChanged += OnStateChanged;
            CodeExecutor.Ins.OnOperationChanged += OnOperationChanged;
        }

        private void Update()
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            m_IsHovering = mousePos.x / Screen.width >= .754f;

            switch (m_State)
            {
                case CodeManagerState.Inserting:
                    float yOffset = UpLeft.y - mousePos.y;
                    m_InsertIndex = (int)((yOffset - 2.5f) / 75f);
                    m_InsertIndex = Math.Clamp(m_InsertIndex, 0, Codes.Count);

                    if (!m_IsHovering)
                    {
                        if (m_IsLastHovering) RefreshCurrPage();
                    }
                    else
                    {
                        if (!m_IsLastHovering || m_InsertIndex != m_LastInsertIndex) RefreshCurrPageOnInserting();
                    }
                    m_LastInsertIndex = m_InsertIndex;
                    break;

                case CodeManagerState.ExitingModify:
                    if (!m_ModifyingCodeitem.IsExitingModify) ChangeState(CodeManagerState.Idle);
                    break;
            }
            m_IsLastHovering = m_IsHovering;

            if (Mouse.current.leftButton.wasReleasedThisFrame) OnMouseReleased();

            for (int i = 0; i < m_LineNums.Count; i++)
                m_LineNums[i].localPosition = Vector3.Lerp(m_LineNums[i].localPosition, m_LineNumDestinations[i], 20 * Time.unscaledDeltaTime);

            RefreshEditInteractables();

            CodeSocket.transform.position = transform.position;

            UpdateArrow();
        }

        private void OnMouseReleased()
        {
            if (m_State == CodeManagerState.Inserting)
            {
                if (m_IsHovering)
                {
                    if (m_InsertingCodeItem is JumpItem jumpItem && jumpItem.Label == null)
                    {
                        CodeItem label = Instantiate(NotePad.Ins.CodeItemPrefabs[(int)OperationType.Label]);
                        label.transform.position = jumpItem.transform.position;
                        LabelItem labelItem = label as LabelItem;

                        jumpItem.Label = labelItem;
                        labelItem.JumpItem = jumpItem;

                        jumpItem.SetColor(GetJumpLineColor());

                        Insert(m_InsertIndex, label);
                        Insert(m_InsertIndex + 1, m_InsertingCodeItem);
                    }
                    else Insert(m_InsertIndex, m_InsertingCodeItem);
                }
                else
                {
                    if (m_InsertingCodeItem is JumpItem jump && jump.Label != null) PopAndDestory(jump.Label);
                    else if (m_InsertingCodeItem is LabelItem label && label.JumpItem != null) PopAndDestory(label.JumpItem);

                    Destroy(m_InsertingCodeItem.gameObject);
                }

                if (!IsModifying) ChangeState(CodeManagerState.Idle);
            }
        }

        private void ChangeState(CodeManagerState state)
        {
            OnStateExit(m_State);
            m_State = state;
            OnStateEnter(m_State);
        }

        private void OnStateEnter(CodeManagerState state)
        {
            switch (m_State)
            {
                case CodeManagerState.Idle:
                    RefreshCurrPage();
                    break;

                case CodeManagerState.Inserting:
                    m_InsertingCodeItem.transform.SetParent(InsertingCodeSocket);
                    m_InsertingCodeItem.StartLerpingMouse();

                    m_LastInsertIndex = -1;
                    break;

                case CodeManagerState.Modifying:
                    m_ModifyingIndex = Codes.IndexOf(m_ModifyingCodeitem);
                    RefreshCurrPageOnModifying();
                    OnStartModify?.Invoke(m_ModifyingCodeitem);
                    break;

                case CodeManagerState.ExitingModify:
                    RefreshCurrPageOnExitingModify();
                    break;
            }
        }

        private void OnStateExit(CodeManagerState state)
        {
            switch (state)
            {
                case CodeManagerState.ExitingModify:
                    m_ModifyingCodeitem = null;
                    break;
            }
        }

        public void SetInsertingCodeItem(CodeItem item)
        {
            if (item == null) return;
            m_InsertingCodeItem = item;
            ChangeState(CodeManagerState.Inserting);

            if ((item is JumpItem jump && jump.Label != null)) SetHighLightJumpItem(jump);
            else if (item is LabelItem label) SetHighLightJumpItem(label.JumpItem);
            else if (m_HighLightJumpItem != null) ClearHighLightJumpItem();
        }

        public void SetModifyingCodeItem(CodeItem item)
        {
            if (item == null || !Codes.Contains(item)) return;
            m_ModifyingCodeitem = item;
            ChangeState(CodeManagerState.Modifying);
        }

        public void ExitModify()
        {
            ChangeState(CodeManagerState.ExitingModify);
        }

        public void Insert(int index, CodeItem item)
        {
            if (Codes.Contains(item)) return;

            Codes.Insert(index, item);
            item.transform.SetParent(CodeSocket);
            item.transform.SetSiblingIndex(index);

            item.OnSelecting += PopAndSelecting;
            item.OnModifying += SetModifyingCodeItem;

            if (!item.IsInitialized) item.Init();

            RefreshEditInteractables();
        }

        private void PopAndSelecting(CodeItem item)
        {
            if (!Codes.Contains(item)) return;

            SetInsertingCodeItem(item);
            Codes.Remove(item);

            item.OnSelecting -= PopAndSelecting;
            item.OnModifying -= SetModifyingCodeItem;

            RefreshEditInteractables();
        }

        private void PopAndDestory(CodeItem item)
        {
            if (!Codes.Contains(item)) return;

            Codes.Remove(item);

            item.OnSelecting -= PopAndSelecting;
            item.OnModifying -= SetModifyingCodeItem;

            Destroy(item.gameObject);

            RefreshEditInteractables();
        }

        #region Layout
        private void RefreshCurrPage()
        {
            int lineNum = 1;

            for (int i = 0; i < Codes.Count; i++)
            {
                StartLerping(Codes[i], i);
                if (Codes[i].ShowLineNum) ShowLineNum(lineNum++, i);
            }

            for (int i = lineNum - 1; i < m_LineNums.Count; i++) m_LineNums[i].gameObject.SetActive(false);
        }

        private void RefreshCurrPageOnInserting()
        {
            int lineNum = 1;

            for (int i = 0; i < m_InsertIndex; i++)
            {
                StartLerping(Codes[i], i);
                if (Codes[i].ShowLineNum) ShowLineNum(lineNum++, i);
            }

            if (m_InsertingCodeItem.ShowLineNum) ShowLineNum(lineNum++, m_InsertIndex);

            for (int i = m_InsertIndex; i < Codes.Count; i++)
            {
                StartLerping(Codes[i], i + 1);
                if (Codes[i].ShowLineNum) ShowLineNum(lineNum++, i + 1);
            }

            for (int i = lineNum - 1; i < m_LineNums.Count; i++) m_LineNums[i].gameObject.SetActive(false);
        }

        private void RefreshCurrPageOnModifying()
        {
            int lineNum = 1;

            for (int i = 0; i < m_ModifyingIndex; i++)
            {
                StartLerping(Codes[i], i);
                if (Codes[i].ShowLineNum) ShowLineNum(lineNum++, i);
            }

            StartLerping(Codes[m_ModifyingIndex], m_ModifyingIndex, worldPos: true);
            ShowLineNum(lineNum++, m_ModifyingIndex);

            for (int i = m_ModifyingIndex + 1; i < Codes.Count; i++)
            {
                StartLerping(Codes[i], i, 50f);
                if (Codes[i].ShowLineNum) ShowLineNum(lineNum++, i, 50f);
            }

            for (int i = lineNum - 1; i < m_LineNums.Count; i++) m_LineNums[i].gameObject.SetActive(false);
        }

        private void RefreshCurrPageOnExitingModify()
        {
            int lineNum = 1;

            for (int i = 0; i < m_ModifyingIndex; i++)
            {
                StartLerping(Codes[i], i);
                if (Codes[i].ShowLineNum) ShowLineNum(lineNum++, i);
            }

            ShowLineNum(lineNum++, m_ModifyingIndex);

            for (int i = m_ModifyingIndex + 1; i < Codes.Count; i++)
            {
                StartLerping(Codes[i], i);
                if (Codes[i].ShowLineNum) ShowLineNum(lineNum++, i);
            }

            for (int i = lineNum - 1; i < m_LineNums.Count; i++) m_LineNums[i].gameObject.SetActive(false);
        }

        private void StartLerping(CodeItem item, int i, float offset = 0, bool worldPos = false)
        {
            item.SetDestination(GetPos(i) + Vector3.right * item.HalfWidth + Vector3.down * offset, worldPos);
            item.StartLerping();
        }

        private void ShowLineNum(int num, int height, float offset = 0)
        {
            while (num > m_LineNums.Count)
            {
                TextMeshProUGUI text = Instantiate(LineNumPrefab, CodeLine);
                text.text = (m_LineNums.Count + 1).ToString("D2");
                text.transform.localPosition = new Vector3(42f, -36f - 75f * height - offset);
                m_LineNums.Add(text.transform);
                m_LineNumDestinations.Add(new Vector3());
            }

            m_LineNums[num - 1].gameObject.SetActive(true);
            m_LineNumDestinations[num - 1] = new Vector3(42f, -36f - 75f * height - offset);
        }

        private Vector3 GetPos(int i) => UpLeft + Vector3.down * (75f * i + 40f);
        #endregion

        private void OnStateChanged()
        {
            var exe = CodeExecutor.Ins;

            if (exe.IsStopped) Arrow.gameObject.SetActive(false);
            else if (exe.IsRunning) Arrow.gameObject.SetActive(true);
        }

        #region Edits
        private void RefreshEditInteractables()
        {
            if (!CodeExecutor.Ins.IsStopped)
            {
                for (int i = 0; i < 4; i++) Edits[i].Interactable = false;
            }
            else
            {
                Edits[0].Interactable = false;
                Edits[1].Interactable = Codes.Count > 0;
                Edits[2].Interactable = !string.IsNullOrEmpty(GUIUtility.systemCopyBuffer);
                Edits[3].Interactable = Codes.Count > 0;
            }
        }

        public void OnUndoButtonDown()
        {
        }

        public void OnCopyButtonDown()
        {
            Dialog.Ins.ShowDialog(_ButtonDialogs[1], false, () => GUIUtility.systemCopyBuffer = CodeSerializer.Export(Codes));
        }

        public void OnPasteButtonDown()
        {
            if (Codes.Count > 0) Dialog.Ins.ShowDialog(_ButtonDialogs[2], true, () => PasteCode(GUIUtility.systemCopyBuffer));
            else PasteCode(GUIUtility.systemCopyBuffer);
        }

        public void OnClearButtonDown()
        {
            Dialog.Ins.ShowDialog(_ButtonDialogs[3], true, () => ClearPage());
        }

        private string[] _ButtonDialogs = new string[]
        {
            "",
            "你的程序现在已经被复制到剪贴板里了。\n试着把它粘贴到其他房间，电子邮件，记事本里，或是其他什么地方。\n哪都可以！没有什么不可能的。",
            "你确定要从剪贴板粘贴代码吗？\n当前便签上所有内容将被覆盖。",
            "你确定要清空当前代码吗？\n该项操作将无法撤销。",
        };
        #endregion

        #region Code Page
        public void InitPage()
        {
            m_LevelId = LevelManager.Ins.LevelId;
            CodeSocket.transform.position = transform.position;

            int lastPage = SaveManager.LoadLastPageIndex(m_LevelId);
            if (lastPage >= 0 && lastPage < 3) m_PageIndex = lastPage;

            string code = SaveManager.LoadPage(m_LevelId, m_PageIndex);
            if (!string.IsNullOrEmpty(code)) PasteCode(code, false);
        }

        private void SavePage()
        {
            SaveManager.SavePage(m_LevelId, m_PageIndex, CodeSerializer.Export(Codes));
        }

        public void LoadPage(int newPageIdx)
        {
            if (newPageIdx < 0 || newPageIdx >= 3) return;
            if (newPageIdx == m_PageIndex) return;

            SavePage();
            ClearPage();

            m_PageIndex = newPageIdx;
            SaveManager.SaveLastPageIndex(m_LevelId, m_PageIndex);

            string code = SaveManager.LoadPage(m_LevelId, m_PageIndex);
            if (string.IsNullOrEmpty(code)) return;

            PasteCode(code, false);
        }

        private void PasteCode(string codes, bool handleError = true)
        {
            if (string.IsNullOrEmpty(codes)) return;

            List<CodeItem> items = CodeSerializer.Import(codes, out ImportError error);
            if (items == null || items.Count == 0)
            {
                if (handleError)
                {
                    string msg = error == ImportError.InstructionLocked
                        ? "你不能粘贴这段代码！\n这段代码里适用了这层楼未开放的命令。"
                        : "你不能粘贴到这！\n它似乎不是一个有效的程序！";
                    GameManager.Ins.Boss.Angry(msg, true);
                }
                return;
            }

            ClearPage(false);

            for (int i = 0; i < items.Count; i++)
            {
                CodeItem item = items[i];

                if (item is JumpItem jump && jump.Label != null)
                    jump.SetColor(GetJumpLineColor());

                Vector3 dest = GetPos(i) + Vector3.right * item.HalfWidth;
                item.transform.position = dest;

                Insert(i, item);
            }

            RefreshCurrPage();
            NotePad.Ins.ResizeFast();
        }

        private void ClearPage(bool refresh = true)
        {
            for (int i = Codes.Count - 1; i >= 0; i--) PopAndDestory(Codes[i]);

            if (!refresh) return;
            RefreshCurrPage();
            NotePad.Ins.ResizeFast();
        }

        public int GetPageLineCount()
        {
            int cnt = 0;
            foreach (var code in Codes)
                if (code.ShowLineNum) cnt++;
            return cnt;
        }
        #endregion

        #region Jump Line高亮显示
        private Color GetJumpLineColor()
        {
            m_CurrJumpColorFactor = (m_CurrJumpColorFactor + .618f) % 1f;
            return Color.Lerp(StartColor, EndColor, m_CurrJumpColorFactor);
        }

        private void SetHighLightJumpItem(JumpItem jump)
        {
            m_HighLightJumpItem = jump;

            jump.StartFadeInLine();
            foreach (CodeItem item in Codes)
            {
                if (item is JumpItem jumpItem && jumpItem != jump)
                {
                    jumpItem.StartFadeOutLine();
                }
            }
        }

        private void ClearHighLightJumpItem()
        {
            m_HighLightJumpItem = null;

            foreach (CodeItem item in Codes)
            {
                if (item is JumpItem jumpItem)
                {
                    jumpItem.StartFadeInLine();
                }
            }
        }
        #endregion

        #region Arrow
        private void OnOperationChanged(int idx)
        {
            if (idx < 0 || m_Codes.Count == 0) return;

            if (idx >= m_Codes.Count)
            {
                Vector3 pos = m_Codes[m_Codes.Count - 1].transform.position + Vector3.down * 75f;
                m_ArrowDesitinationY = transform.InverseTransformPoint(pos).y;
                NotePad.Ins.ScrollToCodeItem(pos.y);
            }
            else
            {
                m_ArrowDesitinationY = transform.InverseTransformPoint(m_Codes[idx].transform.position).y;
                NotePad.Ins.ScrollToCodeItem(m_Codes[idx].transform.position.y);
            }
        }

        private void UpdateArrow()
        {
            Vector3 dest = new Vector3(-330f, m_ArrowDesitinationY, 0f);
            Arrow.localPosition = Vector3.Lerp(Arrow.localPosition, dest, 10f * Time.unscaledDeltaTime);
        }
        #endregion

        private void OnDestroy()
        {
            SavePage();
            SaveManager.SaveLastPageIndex(m_LevelId, m_PageIndex);
        }
    }
}