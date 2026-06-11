using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

namespace Game
{
    public enum VFXType
    {
        DropDataCube,
        Arithmetic,
        Arithmetic_Bump,
    }

    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Ins => _ins == null ? _ins = FindFirstObjectByType<VFXManager>() : _ins;
        private static VFXManager _ins;

        [Header("VFX")]
        public VisualEffect DropDataCube;
        public VisualEffect Arithmetic;
        public VisualEffect ArithmeticBump;

        private Dictionary<VFXType, VisualEffect> m_Map;

        [Header("Volume")]
        public Volume Volume;
        private DepthOfField m_Dof;

        private void Awake()
        {
            m_Map = new Dictionary<VFXType, VisualEffect>()
            {
                { VFXType.DropDataCube ,DropDataCube},
                { VFXType.Arithmetic, Arithmetic},
                { VFXType.Arithmetic_Bump, ArithmeticBump},
            };

            Volume.profile.TryGet(out m_Dof);
        }

        #region VisualEffect
        public VisualEffect Play(VFXType type, Vector3 pos)
        {
            VisualEffect vfx = Instantiate(m_Map[type], pos, Quaternion.identity);
            vfx.Play();

            Destroy(vfx.gameObject, GetDuration(type));

            return vfx;
        }

        private float GetDuration(VFXType type)
        {
            return type switch
            {
                VFXType.DropDataCube => 1f,
                VFXType.Arithmetic => 1f,
                VFXType.Arithmetic_Bump => 1f,
                _ => 1f,
            };
        }
        #endregion

        #region Volume
        public void SetBlur(float instenvity)
        {
            if (instenvity == 0f) m_Dof.active = false;
            else
            {
                m_Dof.active = true;
                m_Dof.focalLength.value = instenvity;
            }
        }

        public void FadeBlur(float instenvity, float duration)
        {
            StartCoroutine(LerpBlur(instenvity, duration));
        }

        private IEnumerator LerpBlur(float instenvity, float duration)
        {
            float elasped = 0f;
            while (elasped < duration)
            {
                elasped += Time.unscaledDeltaTime;
                m_Dof.active = true;
                m_Dof.focalLength.value = Mathf.Lerp(100f, instenvity, elasped / duration);
                yield return null;
            }
        }
        #endregion
    }
}