using UnityEngine;

namespace Game
{
    public enum StateType
    {
        None,

        Idle,
        Walk,

        InBox,
        InBox_GrabDataCube,

        OutBox,
        OutBox_DropDataCube,

        CopyTo,
        CopyTo_CopyTo,

        CopyFrom,
        CopyFrom_CopyFrom,

        Arithmetic,
        Arithmetic_Arithmetic,

        DropDataCube,
    }
}