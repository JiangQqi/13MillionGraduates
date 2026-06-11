using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Game.UI
{
    [Serializable]
    public abstract class CodeItem : MonoBehaviour, IPointerDownHandler
    {
        private enum CodeItemState
        {
            Lerping,
            LerpingMouse,
            BindingMouse,
            Modifying,
            ExitingModify,
        }

        [Header("Modify")]
        [Tooltip("Param UI,不带Param则忽略")]
        public Transform ParamTransform;
        [Tooltip("Modify UI,不带Param则忽略")]
        public Transform ModifyFrameTransform;

        private RectTransform m_Rect;

        private bool m_IsInitialized = false;
        private CodeItemState m_State;
        private float m_LerpProgress;
        private Vector3 m_StartPos;
        private Vector3 m_Destination;
        private float m_LerpingMouseSpeed;
        private Vector3 m_ModifyFrameLerpStartScale = new Vector3(.7f, 0f, 1f);

        public event UnityAction<CodeItem> OnSelecting;
        public event UnityAction<CodeItem> OnModifying;

        public bool IsExitingModify => m_State == CodeItemState.ExitingModify;
        public bool IsInitialized => m_IsInitialized;
        public float HalfWidth => m_Rect.rect.width / 2f;
        public Vector3 Destination => m_Destination;

        public abstract IOperation Operation { get; }
        public abstract OperationType OperationType { get; }
        public abstract bool HasParams { get; }
        public abstract bool ShowLineNum { get; }
        public abstract bool Addressable { get; }

        private void Awake()
        {
            m_Rect = GetComponent<RectTransform>();
        }

        /// <summary>
        /// 当CodeItem第一次被Insert时调用Init
        /// </summary>
        public virtual void Init()
        {
            m_IsInitialized = true;

            if (HasParams)
            {
                ParamTransform.gameObject.SetActive(true);
                StartModify();
            }
        }

        /// <summary>
        /// 静默初始化：显示参数 UI 但不进入编辑模式。
        /// 用于 Paste 等批量创建场景，避免触发 Modify 状态。
        /// </summary>
        public void InitSilently()
        {
            m_IsInitialized = true;

            if (HasParams)
            {
                ParamTransform.gameObject.SetActive(true);
            }
        }
        
        protected virtual void Update()
        {
            m_LerpProgress += Time.unscaledDeltaTime / .3f;
            float t = NotePad.Ins.LerpCurve.Evaluate(Mathf.Clamp01(m_LerpProgress));

            Vector3 offsetDest = m_Destination + Vector3.left * 70f;

            Vector3 mouse = Mouse.current.position.ReadValue();
            switch (m_State)
            {
                case CodeItemState.Lerping:
                    transform.localPosition = Vector3.LerpUnclamped(m_StartPos, m_Destination, t);
                    break;

                case CodeItemState.LerpingMouse:
                    m_LerpingMouseSpeed += 50f * Time.unscaledDeltaTime;
                    transform.position = Vector3.Lerp(transform.position, mouse, m_LerpingMouseSpeed * Time.unscaledDeltaTime);

                    if ((transform.position - mouse).sqrMagnitude <= 1f) StartBindingMouse();
                    break;

                case CodeItemState.BindingMouse:
                    transform.position = mouse;
                    break;

                case CodeItemState.Modifying:
                    transform.position = Vector3.LerpUnclamped(m_StartPos, offsetDest, t);
                    transform.localScale = Vector3.LerpUnclamped(Vector3.one, Vector3.one * 1.6f, t);

                    ModifyFrameTransform.position = offsetDest;
                    ModifyFrameTransform.localScale = Vector3.LerpUnclamped(m_ModifyFrameLerpStartScale, Vector3.one, t);
                    break;

                case CodeItemState.ExitingModify:
                    transform.position = Vector3.LerpUnclamped(offsetDest, m_Destination, t);
                    transform.localScale = Vector3.LerpUnclamped(Vector3.one * 1.6f, Vector3.one, t);

                    ModifyFrameTransform.position = offsetDest;
                    if (m_LerpProgress < .5f) ModifyFrameTransform.localScale = Vector3.Lerp(Vector3.one, m_ModifyFrameLerpStartScale, t);

                    if (m_LerpProgress >= 1) EndModify();
                    break;
            }
        }

        public void SetDestination(Vector3 pos, bool worldPos = false)
        {
            m_Destination = worldPos ? pos : transform.parent.InverseTransformPoint(pos);
            m_StartPos = worldPos ? transform.position : transform.localPosition;
        }

        public void StartLerping(bool force = false)
        {
            if ((m_State is CodeItemState.Modifying or CodeItemState.ExitingModify) && !force) return;

            m_LerpProgress = 0f;
            m_State = CodeItemState.Lerping;
        }

        public void StartLerpingMouse() 
        {
            m_LerpingMouseSpeed = 10f;
            m_State = CodeItemState.LerpingMouse;
        }

        public void StartBindingMouse() => m_State = CodeItemState.BindingMouse;

        /// <summary>
        /// Modify State依靠CodeManager设置的Desitination进行Lerp
        /// </summary>
        public void StartModify()
        {
            if (!HasParams || NotePad.Ins.CodeManager.IsExitingModifying) return;

            m_LerpProgress = 0f;
            ModifyFrameTransform.gameObject.SetActive(true);
            OnModifying?.Invoke(this);

            m_State = CodeItemState.Modifying;
        }

        public void ExitModify()
        {
            m_LerpProgress = 0f;

            m_State = CodeItemState.ExitingModify;
        }

        public void EndModify()
        {
            m_StartPos = transform.parent.InverseTransformPoint(m_Destination);
            ModifyFrameTransform.gameObject.SetActive(false);

            StartLerping(true);
        }

        public virtual void OnAddressButtonDown()
        {
            if (!Addressable) return;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || CodeExecutor.Ins.IsRunning || CodeExecutor.Ins.IsPassed || GameManager.Ins.Boss.WaitForClick) return;

            if (CodeExecutor.Ins.IsPaused || CodeExecutor.Ins.IsPassed || CodeExecutor.Ins.IsErrored) GameManager.Ins.Restart();

            OnSelecting?.Invoke(this);
        }
    }
}