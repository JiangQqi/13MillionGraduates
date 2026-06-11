using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Game
{
    public class Elevator : MonoBehaviour
    {
        public static Elevator Ins => _ins == null ? _ins = FindFirstObjectByType<Elevator>() : _ins;
        private static Elevator _ins;

        [Header("References")]
        public Animator Animator;
        public TextMeshProUGUI Title;
        public TextMeshProUGUI LevelNum;

        [Header("Level")]
        public LevelConfig PendingLevelConfig;

        public event UnityAction OnElevatorOpened;
        public event UnityAction OnElevatorClosed;

        private bool m_IsLoading;

        private void Awake()
        {
            if (_ins != null && _ins != this) 
            {
                Destroy(this.gameObject);
                return;
            }
            _ins = this;
            DontDestroyOnLoad(gameObject);

            StartCoroutine(Init());
        }

        private IEnumerator Init()
        {
            yield return new WaitForSeconds(.5f);
            OnElevatorOpened?.Invoke();
        }

        public void LoadScene(string name)
        {
            if (m_IsLoading) return;
            StartCoroutine(LoadSceneCoroutine(name));
        }

        private IEnumerator LoadSceneCoroutine(string name)
        {
            m_IsLoading = true;

            Animator.SetTrigger("Close");
            OnElevatorClosed?.Invoke();
            yield return new WaitForSeconds(.5f);

            var asyncLoad = SceneManager.LoadSceneAsync(name, LoadSceneMode.Single);
            yield return asyncLoad;

            Animator.SetTrigger(name == "DefaultLevel_N" ? "Open_WithTitle" : "Open");
            yield return new WaitForSeconds(name == "DefaultLevel_N" ? 2.7f : .5f);

            OnElevatorOpened?.Invoke();
            m_IsLoading = false;
        }

        public void SetTitle(string title, int levelid)
        {
            Title.text = title;
            LevelNum.text = $"第{levelid}章";
        }
    }
}