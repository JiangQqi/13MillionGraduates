using UnityEngine;
using Game;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Game.UI
{
    public class OperationItem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private enum OperationItemState
        {
            Unselected,
            Near,
            Far,
        }

        [Header("Setting")]
        [SerializeReference]
        public OperationType OperationType;

        private Transform m_UIPanel;

        private OperationItemState m_State;
        private Vector3 m_MouseDownPos;

        private void Start()
        {
            m_UIPanel = transform.GetChild(0);

            m_State = OperationItemState.Unselected;
        }

        private void Update()
        {
            switch (m_State)
            {
                case OperationItemState.Near:
                    Vector3 mouse = Mouse.current.position.ReadValue();
                    Vector3 offset = mouse - m_MouseDownPos;
                    Lerp(offset);

                    if (offset.sqrMagnitude >= 900f) OnFar();
                    break;

                default:
                    Lerp(Vector3.zero);
                    break;
            }
        }


        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            m_MouseDownPos = eventData.position;
            OnNear();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            OnUnselected();
        }

        private void OnUnselected() => m_State = OperationItemState.Unselected;

        private void OnNear() => m_State = OperationItemState.Near;

        private void OnFar()
        {
            CodeItem item = Instantiate(NotePad.Ins.CodeItemPrefabs[(int)OperationType]);
            item.transform.position = m_UIPanel.position;
            NotePad.Ins.CodeManager.SetInsertingCodeItem(item);

            m_State = OperationItemState.Far;
        }

        private void Lerp(Vector3 end) => m_UIPanel.localPosition = Vector3.Lerp(m_UIPanel.localPosition, end, 10f * Time.unscaledDeltaTime);
    }
}