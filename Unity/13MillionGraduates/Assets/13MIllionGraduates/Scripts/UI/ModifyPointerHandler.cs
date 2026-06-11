using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI
{
    public class ModifyPointerHandler : MonoBehaviour, IPointerDownHandler
    {
        private CodeItem m_CodeItem;

        private void Awake()
        {
            m_CodeItem = transform.parent.GetComponent<CodeItem>();
        }

        /// <summary>
        /// 可改为InteractBlockCount，通过技术来解耦输入阻断
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || CodeExecutor.Ins.IsRunning || CodeExecutor.Ins.IsPassed || GameManager.Ins.Boss.WaitForClick) return;

            if (CodeExecutor.Ins.IsPaused || CodeExecutor.Ins.IsErrored) GameManager.Ins.Restart();

            m_CodeItem.StartModify();
        }
    }
}