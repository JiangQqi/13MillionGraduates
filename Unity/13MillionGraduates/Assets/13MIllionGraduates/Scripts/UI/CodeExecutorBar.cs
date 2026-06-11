using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class CodeExecutorBar : MonoBehaviour
    {
        [Header("Buttons")]
        public TweenButton StopButton;
        public TweenButton StepPrevButton;
        public TweenButton RunButton;
        public TweenButton StepNextButton;

        [Header("Handler")]
        public RectTransform Handler;
        public Sprite[] HandleSprites = new Sprite[2];
        private Image m_HandlerImage;

        private bool m_IsHandleInteractable = true;

        private void Start()
        {
            m_HandlerImage = Handler.GetComponent<Image>();

            StopButton.OnClick.AddListener(OnStop);
            StepPrevButton.OnClick.AddListener(OnStepPrev);
            RunButton.OnClick.AddListener(OnRun);
            StepNextButton.OnClick.AddListener(OnStepNext);

            OnStateChanged();
            CodeExecutor.Ins.OnStateChanged += OnStateChanged;
        }

        private void OnStateChanged()
        {
            var exe = CodeExecutor.Ins;

            if (exe.IsPassed) SetButtonsInteractable(false, false, false, false);
            else if (exe.IsErrored) SetButtonsInteractable(true,  true,  false, false);
            else if (exe.IsStopped) SetButtonsInteractable(false, false, true,  true);
            else if (exe.IsPaused) SetButtonsInteractable(true,  true,  true,  true);
            else if (exe.IsRunning) SetButtonsInteractable(true,  true,  false, true);

            if (exe.IsStopped || exe.IsPassed) SetHandlerRatio(0);

            SetHandlerInteractable(!exe.IsPassed);
        }

        private void SetButtonsInteractable(bool stop, bool prev, bool run, bool next)
        {
            StopButton.Interactable = stop;
            StepPrevButton.Interactable = prev;
            RunButton.Interactable = run;
            StepNextButton.Interactable = next;
        }

        private void OnStop()
        {
            GameManager.Ins.Restart();
        }

        private void OnStepPrev()
        {
            // TODO
        }

        private void OnRun()
        {
            CodeExecutor.Ins.Operations = NotePad.Ins.CodeManager.Instructions.ToList();
            CodeExecutor.Ins.StartExecution();
        }

        private void OnStepNext()
        {
            // TODO
        }

        private void SetHandlerInteractable(bool interactable)
        {
            if (m_IsHandleInteractable == interactable) return;
            m_IsHandleInteractable = interactable;
            m_HandlerImage.sprite = HandleSprites[interactable ? 0 : 1];
        }

        public void OnHandlerDrag()
        {
            if (!m_IsHandleInteractable) return;

            float mouseX = LetterboxManager.MousePosition.x;
            Handler.localPosition = new Vector2(Mathf.Clamp(mouseX - 780f, 160f, 400f), 10f);

            float t = (Handler.localPosition.x - 160f) / (400f - 160f);
            GameManager.Ins.ExecutionSpeed = Mathf.Lerp(1f, 8f, t);
        }

        private void SetHandlerRatio(float t)
        {
            Handler.localPosition = new Vector2(t * 240f + 160f, 10f);
            GameManager.Ins.ExecutionSpeed = Mathf.Lerp(1f, 8f, t);
        }

        private void Update()
        {
            bool hide = NotePad.Ins.CodeManager.IsEmpty || !GameManager.Ins.IsInitialized;
            Vector2 tgtPos = new Vector2(transform.position.x, hide ? -180f : 0f);
            transform.position = Vector2.Lerp(transform.position, tgtPos, 20f * Time.unscaledDeltaTime);
        }
    }
}
