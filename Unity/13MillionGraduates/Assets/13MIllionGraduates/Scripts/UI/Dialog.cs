using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Game.UI
{
    public class Dialog : MonoBehaviour
    {
        public static Dialog Ins => _ins == null ? _ins = FindFirstObjectByType<Dialog>(FindObjectsInactive.Include) : _ins;
        private static Dialog _ins;

        /// <summary>
        /// Buttons[0]代表确认按钮，Buttons[1]代表取消按钮
        /// </summary>
        [Header("References")]
        public TextMeshProUGUI DialogText;
        public Transform[] Buttons = new Transform[2];
        public TextMeshProUGUI ConfrimText;

        private UnityAction[] m_Actions = new UnityAction[2];
        private bool[] m_IsHovering = new bool[2] { false, false };

        private Animator m_Anim;

        private void Start()
        {
            m_Anim = GetComponent<Animator>();
        }

        public void ShowDialog(string text, bool showCancal = false, UnityAction confrimAction = null, UnityAction cancelAction = null)
        {
            gameObject.SetActive(true);

            DialogText.text = text;
            ConfrimText.text = showCancal ? "确定!" : "好的";
            Buttons[1].gameObject.SetActive(showCancal);

            m_Actions[0] = confrimAction;
            m_Actions[1] = cancelAction;
        }

        public void OnPointerDownButton(int idx)
        {
            m_Actions[idx]?.Invoke();

            m_Anim.SetTrigger("Out");
        }

        private void Update()
        {
            for (int i = 0; i < 2; i++) 
            {
                if (!m_IsHovering[i]) Buttons[i].localScale = Vector3.Lerp(Buttons[i].localScale, Vector3.one, 20f * Time.unscaledDeltaTime);
            }
        }

        public void OnPointerEnterButton(int idx)
        {
            Buttons[idx].localScale = Vector3.one * 1.1f;
            m_IsHovering[idx] = true;
        }

        public void OnPointerExitButton(int idx)
        {
            m_IsHovering[idx] = false;
        }

        public void OutAniEvent()
        {
            gameObject.SetActive(false);
        }
    }
}