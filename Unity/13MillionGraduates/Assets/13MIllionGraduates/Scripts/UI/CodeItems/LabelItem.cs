using UnityEngine;

namespace Game.UI
{
    public class LabelItem : CodeItem
    {
        private LabelOperation m_Operation = new LabelOperation();

        public override IOperation Operation => m_Operation;
        public override OperationType OperationType => m_Operation.OperationType;
        public override bool HasParams => false;
        public override bool ShowLineNum => false;
        public override bool Addressable => false;

        private JumpItem m_JumpItem;

        public JumpItem JumpItem
        {
            get => m_JumpItem;
            set => m_JumpItem = value;
        }
    }
}