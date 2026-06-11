using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.VFX;

namespace Game.UI
{
    public class ModifyManager : MonoBehaviour, IPointerDownHandler
    {
        [Header("References")]
        public GameObject Mask;
        public ParamModifier ParamModifierPrafab;

        [Header("Index")]
        public Transform IndexUI;
        public Animator IndexFrame;
        public VisualEffect IndexInVFX;
        public VisualEffect IndexOutVFX;
        public VisualEffect IndexSetVFX;

        /// <summary>
        /// Arith和Jump的两个弹窗跳出只做了从下方弹出的动画，暂无从上方弹出的效果
        /// </summary>
        [Header("Arithmetic")]
        public RectTransform ArithmeticScroll;
        public RectTransform ArithmeticTypeContent;
        public RectTransform ArithmeticSelected;
        private Dictionary<ArithmeticType, int> m_ArithmeticMap = new Dictionary<ArithmeticType, int>();
        private int m_ArithmeticTypeCount;
        private float m_ArithmeticScrollHeight;

        [Header("Jump")]
        public RectTransform JumpScroll;
        public RectTransform JumpTypeContent;
        public RectTransform JumpSelected;
        private Dictionary<JumpType, int> m_JumpMap = new Dictionary<JumpType, int>();
        private int m_JumpTypeCount;
        private float m_JumpScrollHeight;

        private CodeItem m_ModifyingCodeItem;

        private HashSet<OperationType> m_SettableIndexType = new HashSet<OperationType>() {
            OperationType.CopyTo,
            OperationType.CopyFrom,
            OperationType.Arithmetic
        };

        private Transform m_AddressTrigger;
        private bool m_CopyFromAddressabled = false;
        private bool m_CopyToAddressabled = false;
        private bool m_ArithmeticAddressabled = false;

        private void Start()
        {
            LevelManager.Ins.OnLevelInitialized += () =>
            {
                InitArithmeticType();
                InitJumpType();
                InitAddressTrigger();
            };

            NotePad.Ins.CodeManager.OnStartModify += OnStartModify;
        }

        #region Init Arithmetic And Jump And Address TypeScroll Or Trigger
        /// <summary>
        /// 先确定Panel高度再生成各个Type
        /// </summary>
        private void InitArithmeticType()
        {
            OperationConfig cfg = LevelManager.Ins.GetOperationConfig(OperationType.Arithmetic);
            if (!cfg.Enabled) return;

            m_ArithmeticTypeCount = 0;

            if (cfg.Arithmetic_Basic) m_ArithmeticTypeCount += 4;
            if (cfg.Arithmetic_Unary) m_ArithmeticTypeCount += 2;
            if (cfg.Arithmetic_Compare) m_ArithmeticTypeCount += 6;
             m_ArithmeticScrollHeight = Mathf.Min(480f, m_ArithmeticTypeCount * 80f);
            ArithmeticScroll.sizeDelta = new Vector2(153.6f, m_ArithmeticScrollHeight);
            ArithmeticTypeContent.sizeDelta = new Vector2(0f, m_ArithmeticTypeCount * 80f);

            m_ArithmeticTypeCount = 0;
            if (cfg.Arithmetic_Basic)
            {
                SpawnArithmeticType(ArithmeticType.Add);
                SpawnArithmeticType(ArithmeticType.Subtract);
                SpawnArithmeticType(ArithmeticType.Multiply);
                SpawnArithmeticType(ArithmeticType.Divide);
            }
            if (cfg.Arithmetic_Unary)
            {
                SpawnArithmeticType(ArithmeticType.Inc);
                SpawnArithmeticType(ArithmeticType.Dec);
            }
            if (cfg.Arithmetic_Compare)
            {
                SpawnArithmeticType(ArithmeticType.Equal);
                SpawnArithmeticType(ArithmeticType.NotEqual);
                SpawnArithmeticType(ArithmeticType.Greater);
                SpawnArithmeticType(ArithmeticType.Less);
                SpawnArithmeticType(ArithmeticType.GreaterOrEqual);
                SpawnArithmeticType(ArithmeticType.LessOrEqual);
            }
        }

        private void SpawnArithmeticType(ArithmeticType type)
        {
            m_ArithmeticMap[type] = m_ArithmeticTypeCount++;

            ParamModifier arith = Instantiate(ParamModifierPrafab, ArithmeticTypeContent);
            arith.transform.localPosition = new Vector3(53.3f, 40f - 80f * (m_ArithmeticTypeCount), 0f);
            arith.ArithmeticSprite.gameObject.SetActive(true);
            arith.ArithmeticSprite.sprite = NotePad.Ins.ArithmeticSprites[(int)type];
            arith.Init(this, type);
        }

        private void InitJumpType()
        {
            OperationConfig cfg = LevelManager.Ins.GetOperationConfig(OperationType.Jump);
            if (!cfg.Enabled) return;

            m_JumpTypeCount = 0;
            if (cfg.Jump_Always) m_JumpTypeCount += 1;
            if (cfg.Jump_IfZero) m_JumpTypeCount += 1;
            if (cfg.Jump_IfNegative) m_JumpTypeCount += 1;
            m_JumpScrollHeight = Mathf.Min(240f, m_JumpTypeCount * 80f);
            JumpScroll.sizeDelta = new Vector2(166.4f, m_JumpScrollHeight);
            JumpTypeContent.sizeDelta = new Vector2(0f, m_JumpTypeCount * 80f);

            m_JumpTypeCount = 0;
            if (cfg.Jump_Always) SpawnJumpType(JumpType.Any);
            if (cfg.Jump_IfZero) SpawnJumpType(JumpType.IfZero);
            if (cfg.Jump_IfNegative) SpawnJumpType(JumpType.IfNegative);
        }

        private void SpawnJumpType(JumpType type)
        {
            m_JumpMap[type] = m_JumpTypeCount++;

            ParamModifier jump = Instantiate(ParamModifierPrafab, JumpTypeContent);
            jump.transform.localPosition = new Vector3(83.2f, 40f - 80f * (m_JumpTypeCount), 0f);
            jump.JumpTMP.gameObject.SetActive(true);
            jump.JumpTMP.text = type switch
            {
                JumpType.Any => "any",
                JumpType.IfZero => "zro",
                JumpType.IfNegative => "neg",
                _ => "?"
            };
            jump.Init(this, type);
        }

        private void InitAddressTrigger()
        {
            ParamModifier address = Instantiate(ParamModifierPrafab, transform);
            address.AddressTrigger.SetActive(true);
            m_AddressTrigger = address.transform;
            m_AddressTrigger.gameObject.name = "AddressTrigger";
            m_AddressTrigger.gameObject.SetActive(false);
            address.Init(this);

            var copyTo = LevelManager.Ins.GetOperationConfig(OperationType.CopyTo);
            m_CopyToAddressabled = copyTo.Addressabled;
            var copyFrom = LevelManager.Ins.GetOperationConfig(OperationType.CopyFrom);
            m_CopyFromAddressabled = copyFrom.Addressabled;
            var arith = LevelManager.Ins.GetOperationConfig(OperationType.Arithmetic);
            m_ArithmeticAddressabled = arith.Addressabled;
        }
        #endregion

        private void OnStartModify(CodeItem item)
        {
            m_ModifyingCodeItem = item;

            Mask.SetActive(true);

            int idx = -1;
            switch (LevelManager.Ins.GameMode)
            {
                case GameMode.Classic:
                    idx = item switch
                    {
                        CopyToItem copyTo => copyTo.Index,
                        CopyFromItem copyFrom => copyFrom.Index,
                        ArithmeticItem arithmetic => arithmetic.Index,
                        _ => -1,
                    };
                    if (idx >= 0)
                    {
                        IndexUI.position = GameManager.Ins.Carpet.GetCellCenter(idx);
                        IndexFrame.gameObject.SetActive(true);
                        IndexFrame.Play("Modify_Index_Frame_In");
                        IndexInVFX.Play();
                    }
                    break;

                case GameMode.Advanced:
                    break;
            }

            float codeY = m_ModifyingCodeItem.Destination.y;
            if (m_ModifyingCodeItem.OperationType == OperationType.Arithmetic)
            {
                ArithmeticScroll.gameObject.SetActive(true);

                float tgtY =
                    codeY >= m_ArithmeticScrollHeight + 100f ?
                    codeY - 60f :
                    codeY + 60f + m_ArithmeticScrollHeight;

                ArithmeticScroll.position = new Vector3(2317f, tgtY, 0f);
                ArithmeticTypeContent.localPosition = Vector3.zero;

                ArithmeticItem arith = m_ModifyingCodeItem as ArithmeticItem;
                if (m_ArithmeticMap.TryGetValue(arith.ArithmeticType, out int arithIdx))
                    ArithmeticSelected.localPosition = Vector3.down * (40f + 80f * arithIdx);
            }
            else if (m_ModifyingCodeItem.OperationType == OperationType.Jump) 
            {
                JumpScroll.gameObject.SetActive(true);

                float tgtY =
                    codeY >= m_JumpScrollHeight + 100f ?
                    codeY - 60f :
                    codeY + 60f + m_JumpScrollHeight;

                JumpScroll.position = new Vector3(2224.5f, tgtY, 0f);
                JumpTypeContent.localPosition = Vector3.zero;

                JumpItem jump = m_ModifyingCodeItem as JumpItem;
                if (m_JumpMap.TryGetValue(jump.JumpType, out int jumpIdx))
                    JumpSelected.localPosition = new Vector2(83.2f, -40f - 80f * jumpIdx);
            }

            m_AddressTrigger.gameObject.SetActive(false);
            if (m_ModifyingCodeItem.OperationType == OperationType.CopyTo && m_CopyToAddressabled) 
            {
                m_AddressTrigger.gameObject.SetActive(true);
                m_AddressTrigger.position = new Vector3(2222.2f, codeY, 0f);
            }
            else if (m_ModifyingCodeItem.OperationType == OperationType.CopyFrom && m_CopyFromAddressabled) 
            {
                m_AddressTrigger.gameObject.SetActive(true);
                m_AddressTrigger.position = new Vector3(2309.6f, codeY, 0f);
            }
            else if (m_ModifyingCodeItem.OperationType == OperationType.Arithmetic && m_ArithmeticAddressabled)
            {
                m_AddressTrigger.gameObject.SetActive(true);
                m_AddressTrigger.position = new Vector3(2430.75f, codeY, 0f);
            }
        }

        public void OnPointerDown(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left) return;

            if (m_SettableIndexType.Contains(m_ModifyingCodeItem.OperationType))
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit, 100f, 1 << 6))
                {
                    SetIndex(GameManager.Ins.Carpet.SnapToCellIndex(hit.point));
                    IndexFrame.SetTrigger("Set");
                    IndexSetVFX.Play();
                }
                else
                {
                    IndexFrame.SetTrigger("Out");
                    IndexOutVFX.Play();
                }
            }

            ExitModify();
        }

        private void SetIndex(int idx)
        {
            switch (LevelManager.Ins.GameMode)
            {
                case GameMode.Classic:
                    var a = m_ModifyingCodeItem switch
                    {
                        CopyToItem copyTo => copyTo.SetIndex(idx),
                        CopyFromItem copyFrom => copyFrom.SetIndex(idx),
                        ArithmeticItem arithmetic => arithmetic.SetIndex(idx),
                        _ => -1
                    };

                    IndexUI.position = GameManager.Ins.Carpet.GetCellCenter(idx);
                    break;

                case GameMode.Advanced:
                    break;
            }
        }

        public void SetArithmeticType(ArithmeticType type)
        {
            if (m_ModifyingCodeItem is not ArithmeticItem arithmetic) return;

            // 校验当前配置是否允许此类型
            OperationConfig cfg = LevelManager.Ins.GetOperationConfig(OperationType.Arithmetic);
            bool allowed = type switch
            {
                ArithmeticType.Add or ArithmeticType.Subtract or ArithmeticType.Multiply or ArithmeticType.Divide => cfg.Arithmetic_Basic,
                ArithmeticType.Inc or ArithmeticType.Dec => cfg.Arithmetic_Unary,
                ArithmeticType.Equal or ArithmeticType.NotEqual or ArithmeticType.Greater
                    or ArithmeticType.Less or ArithmeticType.GreaterOrEqual or ArithmeticType.LessOrEqual => cfg.Arithmetic_Compare,
                _ => false,
            };
            if (!allowed) return;

            arithmetic.SetArithmeticType(type);

            if (m_ArithmeticMap.TryGetValue(type, out int idx))
                ArithmeticSelected.localPosition = Vector3.down * (40f + 80f * idx);
        }

        public void SetJumpType(JumpType type)
        {
            if (m_ModifyingCodeItem is not JumpItem jump) return;

            // 校验当前配置是否允许此类型
            OperationConfig cfg = LevelManager.Ins.GetOperationConfig(OperationType.Jump);
            bool allowed = type switch
            {
                JumpType.Any => cfg.Jump_Always,
                JumpType.IfZero => cfg.Jump_IfZero,
                JumpType.IfNegative => cfg.Jump_IfNegative,
                _ => false,
            };
            if (!allowed) return;

            jump.SetJumpType(type);

            // 更新选中标记位置
            if (m_JumpMap.TryGetValue(type, out int idx))
                JumpSelected.localPosition = new Vector2(83.2f, -40f - 80f * idx);
        }

        public void OnAddressButtonDown()
        {
            if (!m_ModifyingCodeItem.Addressable) return;

            m_ModifyingCodeItem.OnAddressButtonDown();
        }

        private void ExitModify()
        {
            m_ModifyingCodeItem.ExitModify();
            m_ModifyingCodeItem = null;
            NotePad.Ins.CodeManager.ExitModify();

            Mask.SetActive(false);
            ArithmeticScroll.gameObject.SetActive(false);
            JumpScroll.gameObject.SetActive(false);
            m_AddressTrigger.gameObject.SetActive(false);
        }
    }
}