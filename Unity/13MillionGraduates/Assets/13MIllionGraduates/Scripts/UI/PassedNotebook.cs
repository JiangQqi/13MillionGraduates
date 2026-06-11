using TMPro;
using UnityEngine;

namespace Game.UI
{
    public class PassedNotebook : MonoBehaviour
    {
        [Header("References")]
        public GameObject Root;
        public GameObject VFX;

        [Header("Title")]
        public TextMeshProUGUI Title;
        public TextMeshProUGUI LevelNum;

        [Header("Optimization")]
        public TextMeshProUGUI OptimizationGoal;
        public TextMeshProUGUI OptimizationCurrent;
        public TextMeshProUGUI OptimizationBest;

        [Header("Efficiency")]
        public TextMeshProUGUI EfficiencyGoal;
        public TextMeshProUGUI EfficiencyCurrent;
        public TextMeshProUGUI EfficiencyBest;

        [Header("Button")]
        public TweenButton[] Buttons = new TweenButton[2];

        private Animator m_Anim;

        private void Awake()
        {
            Root.SetActive(false);
            VFX.SetActive(false);
            m_Anim = Root.GetComponent<Animator>();

            Buttons[0].OnClick.AddListener(OnRestart);
            Buttons[1].OnClick.AddListener(OnReturn);

            GameManager.Ins.OnGamePassed += OnPassed;
        }

        private void OnPassed(int lines, int steps)
        {
            Root.SetActive(true);

            int id = LevelManager.Ins.LevelId;
            Title.text = LevelManager.Ins.LevelConfig.LevelTitle;
            LevelNum.text = $"第{id.ToString()}章";
            OptimizationGoal.text = $"最多使用{LevelManager.Ins.LevelConfig.OptimalLines}行代码。";
            OptimizationCurrent.text = $"你当前的程序使用了{lines}行代码";
            OptimizationBest.text = $"你的最优的程序使用了{SaveManager.GetBestLines(id)}行代码";
            EfficiencyGoal.text = $"最多进行{LevelManager.Ins.LevelConfig.OptimalSteps}步运算。";
            EfficiencyCurrent.text = $"你当前的程序进行了{steps}步运算";
            EfficiencyBest.text = $"你最高效的程序进行了{SaveManager.GetBestSteps(id)}步运算";

            VFX.SetActive(true);
            VFXManager.Ins.FadeBlur(200f, .4f);
        }

        private void OnRestart()
        {
            m_Anim.SetTrigger("Out");
            VFX.SetActive(false);
            VFXManager.Ins.SetBlur(0f);
            GameManager.Ins.Restart();
        }

        private void OnReturn()
        {
            Elevator.Ins.LoadScene("LevelNotebook");
        }
    }
}