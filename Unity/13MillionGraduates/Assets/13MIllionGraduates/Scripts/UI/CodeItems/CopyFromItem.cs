using TMPro;
using UnityEngine;

namespace Game.UI
{
    public class CopyFromItem : CodeItem
    {
        [Header("Copy From")]
        public TextMeshProUGUI IndexText;
        private CopyFromOperation m_Operation = new CopyFromOperation();

        public override IOperation Operation => m_Operation;
        public override OperationType OperationType => m_Operation.OperationType;
        public override bool HasParams => true;
        public override bool ShowLineNum => true;
        public override bool Addressable => true;

        public int AreaId => m_Operation.Param1;
        public int Index => m_Operation.Param2;
        public bool IsAddress => m_Operation.Param3;

        /// <summary>
        /// [传递返回值] 行为本身只是为了可以使用Switch类型匹配
        /// </summary>
        public int SetIndex(int idx)
        {
            if (idx < 0 || idx >= GameManager.Ins.Carpet.Size) return -1;

            m_Operation.Param2 = idx;
            IndexText.text = GetIndexText();
            return idx;
        }

        public void SetIsAddress(bool isAddress)
        {
            m_Operation.Param3 = isAddress;
            IndexText.text = GetIndexText();
        }

        public override void OnAddressButtonDown()
        {
            m_Operation.Param3 = !m_Operation.Param3;
            IndexText.text = GetIndexText();
        }

        private string GetIndexText()
        {
            string idx = m_Operation.Param2.ToString();
            return m_Operation.Param3 ? $"[{idx}]" : idx;
        }
    }
}