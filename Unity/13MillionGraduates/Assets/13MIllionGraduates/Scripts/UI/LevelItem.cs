using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class LevelItem : MonoBehaviour
    {
        private enum State
        {
            Locked,
            Active,
            Passed
        };

        [Header("References")]
        public Transform Circle;
        public TextMeshProUGUI LevelNum;
        public Image StateImage;
        public Image AdvanceImage;
        public TextMeshProUGUI Title;

        [Header("Level")]
        public int LevelId;

        private LevelConfig m_Cfg;
        private State m_State;

        private bool m_IsHovering;
        private float m_Timer;

        public void Init(LevelConfig cfg, int state, Sprite stateSprite, Sprite advance, string title)
        {
            m_Cfg = cfg;
            m_State = (State)state;

            if (m_State == State.Locked) LevelNum.gameObject.SetActive(false);
            LevelNum.text = LevelId.ToString();

            StateImage.sprite = stateSprite;
            StateImage.SetNativeSize();

            AdvanceImage.sprite = advance;

            Title.text = title;
        }

        private void Update()
        {
            if (m_State != State.Active || m_IsHovering) return;

            m_Timer += Time.unscaledDeltaTime;
            float t = Mathf.Sin(2 * m_Timer * Mathf.PI) * .15f + 1f;
            Circle.localScale = Vector3.one * t;
        }

        public void OnPointerEnter()
        {
            m_IsHovering = true;
            Circle.localScale = Vector3.one * 1.15f;
        }

        public void OnPointerExit()
        {
            m_IsHovering = false;
            if (m_State == State.Active) m_Timer = .25f;
            else Circle.localScale = Vector3.one;
        }

        public void OnPointerDown()
        {
            if (m_State == State.Locked) return;

            Elevator.Ins.PendingLevelConfig = m_Cfg;
            Elevator.Ins.SetTitle(m_Cfg.LevelTitle, LevelId);
            Elevator.Ins.LoadScene("DefaultLevel_N");
        }
    }
}