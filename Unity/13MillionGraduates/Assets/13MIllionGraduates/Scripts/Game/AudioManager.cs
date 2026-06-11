using System.Collections;
using System.ComponentModel.Design;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// 音频素材不足，目前只有BGM。
    /// 「有时候，沉默比任何声音都更有力量。」
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Ins => _ins == null ? _ins = FindFirstObjectByType<AudioManager>() : _ins;
        private static AudioManager _ins;

        [Header("Referneces")]
        public AudioSource AudioSource;

        [Header("Setting")]
        public bool AlwaysPlayBgm = true;

        private void Awake()
        {
            Elevator.Ins.OnElevatorOpened += PlayBgm;
            Elevator.Ins.OnElevatorClosed += StopBgm;

            SetBgmMuted(SaveManager.IsBgmMuted());
        }

        public void SetBgmMuted(bool Muted)
        {
            AudioSource.mute = !AlwaysPlayBgm && Muted;
        }

        public void SetBgm(AudioClip bgm)
        {
            if (AudioSource == null || bgm == null) return;
            
            AudioSource.clip = bgm;
        }

        public void PlayBgm()
        {
            AudioSource.Play();
            StopAllCoroutines();
            StartCoroutine(Fade(0f, 1f, 1f));
        }

        public void StopBgm()
        {
            StopAllCoroutines();
            StartCoroutine(Fade(1f, 0f, .5f));
        }

        private IEnumerator Fade(float start, float tgt, float duration)
        {
            AudioSource.volume = start;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                AudioSource.volume = Mathf.Lerp(start, tgt, elapsed / duration);
                yield return null;
            }

            AudioSource.volume = tgt;
            if (tgt == 0f) AudioSource.Stop();
        }

        private void OnDestroy()
        {
            if (Elevator.Ins != null)
            {
                Elevator.Ins.OnElevatorOpened -= PlayBgm;
                Elevator.Ins.OnElevatorClosed -= StopBgm;
            }
        }
    }
}