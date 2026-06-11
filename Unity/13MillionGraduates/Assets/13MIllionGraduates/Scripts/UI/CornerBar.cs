using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class CornerBar : MonoBehaviour
    {
        [Header("Buttons")]
        public TweenButton ReturnButton;
        public TweenButton VolumeButton;

        [Header("References")]
        public Sprite[] VolumeSprites = new Sprite[2];
        private Image m_VolumeImage;

        private bool m_IsMuted = false;

        private void Awake()
        {
            m_VolumeImage = VolumeButton.GetComponent<Image>();

            SetVolume(SaveManager.IsBgmMuted());
        }

        private void Start()
        {
            ReturnButton.OnClick.AddListener(OnReturn);
            VolumeButton.OnClick.AddListener(OnVolume);
        }

        private void OnReturn()
        {
            Elevator.Ins.LoadScene("LevelNotebook");
        }

        private void OnVolume()
        {
            SetVolume(!m_IsMuted);
            SaveManager.SetBgmMuted(m_IsMuted);
        }

        private void SetVolume(bool muted)
        {
            if (m_IsMuted == muted) return;
            m_IsMuted = muted;

            AudioManager.Ins.SetBgmMuted(muted);

            m_VolumeImage.sprite = VolumeSprites[muted ? 1 : 0];
        }
    }
}
