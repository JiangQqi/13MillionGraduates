using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game
{
    public class LetterboxManager : MonoBehaviour
    {
        private const float TargetAspect = 16f / 9f;
        public  const float RefWidth     = 2560f;
        public  const float RefHeight    = 1440f;
        private const int   BlackBarSortOrder = 32767;

        [SerializeField] private int m_MaxWindowHeight = 1080;

        private static readonly (int w, int h)[] s_Resolutions =
        {
            (2560, 1440), (1920, 1080), (1600, 900), (1280, 720), (960, 540),
        };

        private static LetterboxManager s_Ins;
        public  static bool IsFullscreen => Screen.fullScreen;

        private Canvas        m_BlackBarCanvas;
        private RectTransform m_BarLeft, m_BarRight, m_BarTop, m_BarBottom;
        private Vector2Int    m_LastScreenSize;

        public static Vector2 MousePosition
        {
            get
            {
                Vector2 sp = Mouse.current.position.ReadValue();
                var cam = Camera.main;
                if (cam != null)
                {
                    Rect r = cam.rect;
                    if (r.width  < 1f) sp.x = (sp.x / Screen.width  - r.x) / r.width  * Screen.width;
                    if (r.height < 1f) sp.y = (sp.y / Screen.height - r.y) / r.height * Screen.height;
                }
                return new Vector2(sp.x / Screen.width * RefWidth, sp.y / Screen.height * RefHeight);
            }
        }

        public static Vector3 MouseWorldPosition => (Vector3)MousePosition;
        public static float   WorldToCanvasY(Vector3 wp) => wp.y;

        public static void ToggleFullscreen() => SetFullscreen(!IsFullscreen);

        public static void SetFullscreen(bool fullscreen)
        {
            if (fullscreen)
            {
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                s_Ins?.StartCoroutine(s_Ins.DelayedApply());
            }
            else
            {
                SetWindowed();
            }
        }

        public static void SetWindowed()
        {
            var size = GetBestWindowSize();
            Screen.SetResolution(size.x, size.y, FullScreenMode.Windowed);
            s_Ins?.StartCoroutine(s_Ins.DelayedApply());
        }

        private IEnumerator DelayedApply()
        {
            yield return null;
            Apply();
        }

        public static Vector2Int GetBestWindowSize()
        {
            int screenW = Display.main.systemWidth;
            int screenH = Display.main.systemHeight;
            int maxH    = s_Ins != null && s_Ins.m_MaxWindowHeight > 0
                ? Mathf.Min(screenH - 80, s_Ins.m_MaxWindowHeight)
                : screenH - 80;

            foreach (var (w, h) in s_Resolutions)
                if (h <= maxH && w <= screenW - 20)
                    return new Vector2Int(w, h);

            int fallbackH = Mathf.Min(maxH, 540);
            return new Vector2Int(fallbackH * 16 / 9, fallbackH);
        }

        private void Awake()
        {
            if (s_Ins != null) { Destroy(gameObject); return; }
            s_Ins = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;

            m_LastScreenSize = new Vector2Int(Screen.width, Screen.height);

            CreateBlackBarOverlay();
            SetWindowed();
        }

        private void OnDestroy()
        {
            if (s_Ins == this) SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Update()
        {
            var current = new Vector2Int(Screen.width, Screen.height);
            if (current == m_LastScreenSize) return;
            m_LastScreenSize = current;
            Apply();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Apply();

        private void Apply()
        {
            var cam = Camera.main;
            if (cam == null) return;

            float current = (float)Screen.width / Screen.height;
            Rect targetRect;

            if (Mathf.Approximately(current, TargetAspect) || current <= 0f)
            {
                targetRect = new Rect(0f, 0f, 1f, 1f);
            }
            else if (current > TargetAspect)
            {
                float scale = TargetAspect / current;
                float bar   = (1f - scale) * 0.5f;
                targetRect = new Rect(bar, 0f, scale, 1f);
            }
            else
            {
                float scale = current / TargetAspect;
                float bar   = (1f - scale) * 0.5f;
                targetRect = new Rect(0f, bar, 1f, scale);
            }

            cam.rect = targetRect;

            var camData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (camData != null)
            {
                foreach (var overlay in camData.cameraStack)
                    if (overlay != null) overlay.rect = targetRect;
            }

            Canvas.ForceUpdateCanvases();
            UpdateBlackBars(targetRect);
        }

        private void CreateBlackBarOverlay()
        {
            var go = new GameObject("BlackBarOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform);
            go.layer = LayerMask.NameToLayer("UI");

            m_BlackBarCanvas = go.GetComponent<Canvas>();
            m_BlackBarCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_BlackBarCanvas.sortingOrder = BlackBarSortOrder;
            m_BlackBarCanvas.GetComponent<GraphicRaycaster>().enabled = false;

            m_BarLeft   = CreateBar();
            m_BarRight  = CreateBar();
            m_BarTop    = CreateBar();
            m_BarBottom = CreateBar();
        }

        private RectTransform CreateBar()
        {
            var go = new GameObject("Bar", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(m_BlackBarCanvas.transform, false);
            var rt = go.GetComponent<RectTransform>();
            go.GetComponent<Image>().color = Color.black;
            go.GetComponent<Image>().raycastTarget = false;
            return rt;
        }

        private void UpdateBlackBars(Rect camRect)
        {
            if (m_BlackBarCanvas == null) return;

            int sw = Screen.width;
            int sh = Screen.height;

            if (Mathf.Approximately(camRect.width, 1f) && Mathf.Approximately(camRect.height, 1f))
            {
                m_BarLeft.sizeDelta = m_BarRight.sizeDelta = Vector2.zero;
                m_BarTop.sizeDelta  = m_BarBottom.sizeDelta = Vector2.zero;
                return;
            }

            if (camRect.width < 1f)
            {
                float barPx = camRect.x * sw;
                m_BarLeft.localPosition = new Vector2((barPx - sw) / 2, 0);
                m_BarLeft.sizeDelta = new Vector2(barPx, sh);
                m_BarRight.localPosition = new Vector2((sw - barPx) / 2, 0);
                m_BarRight.sizeDelta = new Vector2(barPx, sh);

                m_BarTop.sizeDelta = m_BarBottom.sizeDelta = Vector2.zero;
            }
            else
            {
                float barPx = camRect.y * sh;
                m_BarTop.localPosition = new Vector2(0f, (sh - barPx) / 2);
                m_BarTop.sizeDelta = new Vector2(sw, barPx);
                m_BarBottom.localPosition = new Vector2(0f, (barPx - sh) / 2);
                m_BarBottom.sizeDelta = new Vector2(sw, barPx);

                m_BarLeft.sizeDelta = m_BarRight.sizeDelta = Vector2.zero;
            }
        }
    }
}
