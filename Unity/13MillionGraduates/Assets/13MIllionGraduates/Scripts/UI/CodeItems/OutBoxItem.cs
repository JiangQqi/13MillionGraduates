namespace Game.UI
{
    public class OutBoxItem : CodeItem
    {
        private OutBoxOperation m_Operation = new OutBoxOperation();

        public override IOperation Operation => m_Operation;
        public override OperationType OperationType => m_Operation.OperationType;
        public override bool HasParams => false;
        public override bool ShowLineNum => true;
        public override bool Addressable => false;
    }
}