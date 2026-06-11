using System.Collections;
using UnityEngine;

namespace Game.UI
{
    public class CameraShake : MonoBehaviour
    {
        [Header("Shake Setting")]
        public float Intensity = 1f;
        public float Frequency = 8f;

        private Vector3 m_OriginalPos;
        private Coroutine m_ShakeRoutine;

        private void Awake()
        {
            m_OriginalPos = transform.position;

            GameManager.Ins.Boss.OnAngry += (string text, bool b) => Shake();
        }

        private void Shake()
        {
            if (m_ShakeRoutine != null) StopCoroutine(m_ShakeRoutine);
            m_ShakeRoutine = StartCoroutine(DoShake(Intensity, Frequency));
        }

        private IEnumerator DoShake(float intensity, float frequency)
        {
            float elapsed = 0f;
            float duration = .6f;
            float seed = Random.Range(0f, 100f);

            while (elapsed < duration) 
            {
                elapsed += Time.unscaledDeltaTime;
                float decay = 1f - elapsed / duration;

                float t = elapsed * frequency + seed;
                float x = (Mathf.PerlinNoise(t, 0f) * 2f - 1f) * intensity * decay;
                float y = (Mathf.PerlinNoise(0f, t) * 2f - 1f) * intensity * decay;
                transform.position = m_OriginalPos + new Vector3(x, y, 0);

                yield return null;
            }

            transform.position = m_OriginalPos;
        }
    }
}