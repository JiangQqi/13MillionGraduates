using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class ArithmeticItem : CodeItem
    {
        [Header("Arithmetic")]
        public Image ArithmeticTypeSprite;
        public TextMeshProUGUI IndexText;
        private ArithmeticOperation m_Operation = new ArithmeticOperation();

        public override IOperation Operation => m_Operation;
        public override OperationType OperationType => m_Operation.OperationType;
        public override bool HasParams => true;
        public override bool ShowLineNum => true;
        public override bool Addressable => true;

        private bool m_JumpTypeSetExternally;

        public int AreaId => m_Operation.Param1;
        public int Index => m_Operation.Param2;
        public ArithmeticType ArithmeticType => m_Operation.Param3;
        public bool IsAddress => m_Operation.Param4;

        private void Start()
        {
            if (m_JumpTypeSetExternally) return;

            OperationConfig cfg = LevelManager.Ins.GetOperationConfig(OperationType);
            if (cfg.Arithmetic_Basic) SetArithmeticType(ArithmeticType.Add);
            else if (cfg.Arithmetic_Unary) SetArithmeticType(ArithmeticType.Inc);
            else if (cfg.Arithmetic_Compare) SetArithmeticType(ArithmeticType.Equal);
        }
        
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

        public void SetArithmeticType(ArithmeticType type)
        {
            m_JumpTypeSetExternally = true;

            m_Operation.Param3 = type;
            ArithmeticTypeSprite.sprite = NotePad.Ins.ArithmeticSprites[(int)type];
        }

        public void SetIsAddress(bool isAddress)
        {
            m_Operation.Param4 = isAddress;
            IndexText.text = GetIndexText();
        }

        public override void OnAddressButtonDown()
        {
            m_Operation.Param4 = !m_Operation.Param4;

            IndexText.text = GetIndexText();
        }

        private string GetIndexText()
        {
            string idx = m_Operation.Param2.ToString();
            return m_Operation.Param4 ? $"[{idx}]" : idx;
        }
    }
}