using UnityEngine;

namespace Game
{
    public class PlayerAnimationEvnetAgent : MonoBehaviour
    {
        ///<summary>
        ///因子物体Animator无法调用父物体Player的方法作为AniEvent
        ///故以此脚本做代理器
        ///</summary>
        
        private PlayerController m_Player;

        private void Awake()
        {
            m_Player = GetComponentInParent<PlayerController>();
        }

        public void InBox_GrabDataCube_AniEvent01() => m_Player.InBox_GrabDataCube_AniEvent01();

        public void InBox_GrabDataCube_AniEvent02() => m_Player.InBox_GrabDataCube_AniEvent02();

        public void DropDataCube_AniEvent01() => m_Player.DropDataCube_AniEvent01();

        public void OutBox_DropDataCube_AniEvent01() => m_Player.OutBox_DropDateCube_AniEvent01();

        public void CopyTo_CopyTo_AniEvent01() => m_Player.CopyTo_CopyTo_AniEvent01();

        public void CopyTo_CopyTo_AniEvent02() => m_Player.CopyTo_CopyTo_AniEvent02();

        public void CopyFrom_CopyFrom_AniEnvent01() => m_Player.CopyFrom_CopyFrom_AniEvent01();

        public void CopyFrom_CopyFrom_AniEnvent02() => m_Player.CopyFrom_CopyFrom_AniEvent02();

        public void Arithmetic_Arithmetic_AniEvent01() => m_Player.Arithmetic_Arithmetic_AniEvent01();

        public void Arithmetic_Arithmetic_AniEvent02() => m_Player.Arithmetic_Arithmetic_AniEvent02();

        public void Arithmetic_Arithmetic_AniEvent03() => m_Player.Arithmetic_Arithmetic_AniEvent03();

        public void Arithmeic_Bump_AniEvent01() => m_Player.Arithmetic_Bump_AniEvent01();

        public void Arithmeic_Bump_AniEvent02() => m_Player.Arithmetic_Bump_AniEvent02();

        public void Arithmeic_Bump_AniEvent03() => m_Player.Arithmetic_Bump_AniEvent03();

        public void Arithmetic_Bump_WithCube_AniEvent01() => m_Player.Arithmetic_Bump_WithCube_AniEvent01();

        public void Arithmetic_Bump_WithCube_AniEvent02() => m_Player.Arithmetic_Bump_WithCube_AniEvent02();

        public void Arithmetic_Bump_WithCube_AniEvent03() => m_Player.Arithmetic_Bump_WithCube_AniEvent03();

        public void Arithmetic_Bump_WithCube_AniEvent04() => m_Player.Arithmetic_Bump_WithCube_AniEvent04();
    }
}