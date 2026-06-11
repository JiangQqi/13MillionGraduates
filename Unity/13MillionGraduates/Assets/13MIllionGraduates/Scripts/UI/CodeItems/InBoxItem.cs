namespace Game.UI
{
    public class InBoxItem : CodeItem
    {
        private InBoxOperation m_Operation = new InBoxOperation();

        public override IOperation Operation => m_Operation;
        public override OperationType OperationType => m_Operation.OperationType;
        public override bool HasParams => false;
        public override bool ShowLineNum => true;
        public override bool Addressable => false;
    }
}