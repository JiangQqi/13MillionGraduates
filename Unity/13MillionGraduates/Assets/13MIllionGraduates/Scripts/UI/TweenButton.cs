using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    public class TweenButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        [Header("Hover")]
        public float HoverScale = 1.1f;
        public float LerpSpeed = 10f;

        [Header("Interactable Sprite Swap (Optional)")]
        public Image TargetImage;
        public Sprite EnabledSprite;
        public Sprite DisabledSprite;
        [Range(0f, 1f)]
        public float DisabledAlpha = 0.5f;

        [Header("Click")]
        public UnityEvent OnClick;

        private bool m_IsHovering;
        private bool m_IsInteractable = true;
        private Vector3 m_DefaultScale;

        private void Awake()
        {
            m_DefaultScale = transform.localScale;
            if (TargetImage == null)
                TargetImage = GetComponent<Image>();
        }

        public bool Interactable
        {
            get => m_IsInteractable;
            set
            {
                if (m_IsInteractable == value) return;
                m_IsInteractable = value;

                if (TargetImage != null)
                {
                    if (EnabledSprite != null && DisabledSprite != null)
                        TargetImage.sprite = value ? EnabledSprite : DisabledSprite;

                    Color c = TargetImage.color;
                    c.a = value ? 1f : DisabledAlpha;
                    TargetImage.color = c;
                }

                if (!value)
                {
                    m_IsHovering = false;
                    transform.localScale = m_DefaultScale;
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!m_IsInteractable) return;
            transform.localScale = m_DefaultScale * HoverScale;
            m_IsHovering = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            m_IsHovering = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!m_IsInteractable) return;
            OnClick?.Invoke();
        }

        private void Update()
        {
            if (!m_IsHovering)
            {
                Vector3 current = transform.localScale;
                if (current != m_DefaultScale)
                    transform.localScale = Vector3.Lerp(current, m_DefaultScale, LerpSpeed * Time.unscaledDeltaTime);
            }
        }
    }
}
