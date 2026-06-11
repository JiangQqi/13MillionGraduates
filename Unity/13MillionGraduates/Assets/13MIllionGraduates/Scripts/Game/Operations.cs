using System;

namespace Game
{
    public enum OperationType
    {
        InBox,
        OutBox,
        CopyTo,
        CopyFrom,
        Arithmetic,

        Jump,
        Label,
    }

    public enum JumpType
    {
        Any,
        IfZero,
        IfNegative,
    }

    public interface IOperation
    {
        void Execute(PlayerController player);
        OperationType OperationType { get; }
    }

    [Serializable]
    public abstract class BaseEmptyOperation : IOperation
    {
        public abstract OperationType OperationType { get; }
        public abstract void Execute(PlayerController player);
    }

    [Serializable]
    public abstract class BaseSingleParamOperation<T> : IOperation
    {
        public T Param;
        public abstract OperationType OperationType { get; }
        public abstract void Execute(PlayerController player);
    }

    [Serializable]
    public abstract class BaseDoubleParamOperation<T1, T2> : IOperation
    {
        public T1 Param1;
        public T2 Param2;
        public abstract OperationType OperationType { get; }
        public abstract void Execute(PlayerController player);
    }

    [Serializable]
    public abstract class BaseTripleParamOperation<T1, T2, T3> : IOperation
    {
        public T1 Param1;
        public T2 Param2;
        public T3 Param3;
        public abstract OperationType OperationType { get; }
        public abstract void Execute(PlayerController player);
    }

    [Serializable]
    public abstract class BaseQuadParamOperation<T1, T2, T3, T4> : IOperation
    {
        public T1 Param1;
        public T2 Param2;
        public T3 Param3;
        public T4 Param4;
        public abstract OperationType OperationType { get; }
        public abstract void Execute(PlayerController player);
    }

    [Serializable]
    public class InBoxOperation : BaseEmptyOperation
    {
        public override OperationType OperationType => OperationType.InBox;
        public override void Execute(PlayerController player)
        {
            player.StateMachine.ChangeState(StateType.InBox);
        }
    }

    [Serializable]
    public class OutBoxOperation : BaseEmptyOperation
    {
        public override OperationType OperationType => OperationType.OutBox;
        public override void Execute(PlayerController player)
        {
            player.StateMachine.ChangeState(StateType.OutBox);
        }
    }

    [Serializable]
    public class CopyToOperation : BaseTripleParamOperation<int, int, bool>
    {
        public override OperationType OperationType => OperationType.CopyTo;
        public override void Execute(PlayerController player)
        {
            CopyToState copyTo = player.StateMachine.GetStateByType(StateType.CopyTo) as CopyToState;
            copyTo.SetDestination(Param1, Param2, Param3);
            player.StateMachine.ChangeState(StateType.CopyTo);
        }
    }

    [Serializable]
    public class CopyFromOperation : BaseTripleParamOperation<int, int, bool>
    {
        public override OperationType OperationType => OperationType.CopyFrom;
        public override void Execute(PlayerController player)
        {
            CopyFromState copyFrom = player.StateMachine.GetStateByType(StateType.CopyFrom) as CopyFromState;
            copyFrom.SetDestinationDataCube(Param1, Param2, Param3);
            player.StateMachine.ChangeState(StateType.CopyFrom);
        }
    }

    [Serializable]
    public class ArithmeticOperation : BaseQuadParamOperation<int, int, ArithmeticType, bool>
    {
        public override OperationType OperationType => OperationType.Arithmetic;
        public override void Execute(PlayerController player)
        {
            ArithmeticState arithmetic = player.StateMachine.GetStateByType(StateType.Arithmetic) as ArithmeticState;
            arithmetic.SetDestinationDataCubeAndType(Param1, Param2, Param3, Param4);
            player.StateMachine.ChangeState(StateType.Arithmetic);
        }
    }

    [Serializable]
    public class JumpOperation : BaseDoubleParamOperation<LabelOperation, JumpType>
    {
        public override OperationType OperationType => OperationType.Jump;
        public override void Execute(PlayerController player)
        {
            //throw new NotImplementedException();
        }
    }

    [Serializable]
    public class LabelOperation : BaseEmptyOperation
    {
        public override OperationType OperationType => OperationType.Label;
        public override void Execute(PlayerController player)
        {
            //throw new NotImplementedException();
        }
    }
}