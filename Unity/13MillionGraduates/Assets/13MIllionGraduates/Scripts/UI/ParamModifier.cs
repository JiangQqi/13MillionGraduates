using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 为ModifyManager提供元素被点击事件
    /// </summary>
    public class ParamModifier : MonoBehaviour, IPointerDownHandler
    {
        private enum ParamModifierType
        {
            Arithmetic,
            Jump,
            Address
        };

        [Header("References")]
        public Image ArithmeticSprite;
        public TextMeshProUGUI JumpTMP;
        public GameObject AddressTrigger;

        private ParamModifierType m_ModifierType;
        private ModifyManager m_Manager;
        private int m_Type;

        public void Init(ModifyManager mgr, ArithmeticType type)
        {
            m_ModifierType = ParamModifierType.Arithmetic;
            m_Manager = mgr;
            m_Type = (int)type;
        }

        public void Init(ModifyManager mgr, JumpType type)
        {
            m_ModifierType = ParamModifierType.Jump;
            m_Manager = mgr;
            m_Type = (int)type;
        }

        public void Init(ModifyManager mgr)
        {
            m_ModifierType = ParamModifierType.Address;
            m_Manager = mgr;
        }

        public void OnPointerDown(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left) return;

            switch (m_ModifierType)
            {
                case ParamModifierType.Arithmetic:
                    m_Manager.SetArithmeticType((ArithmeticType)m_Type);
                    break;

                case ParamModifierType.Jump:
                    m_Manager.SetJumpType((JumpType)m_Type);
                    break;

                case ParamModifierType.Address:
                    m_Manager.OnAddressButtonDown();
                    break;
            }

            ExecuteEvents.ExecuteHierarchy<IPointerDownHandler>(
                transform.parent.gameObject,
                data,
                (handler, eventData) => handler.OnPointerDown(data)
            );
        }
    }
}