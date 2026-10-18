using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        Instance = null;
    }

    [Header("Transition Configuration")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private Color fadeColor = Color.black;

    private Canvas transitionCanvas;
    private CanvasGroup transitionCanvasGroup;
    private Image fadeImage;
    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SetupOverlay();
    }

    private void SetupOverlay()
    {
        if (transitionCanvas != null) return;

        // Create overlay canvas on top of everything
        GameObject canvasObj = new GameObject("SceneTransitionOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasObj.transform.SetParent(transform, false);

        transitionCanvas = canvasObj.GetComponent<Canvas>();
        transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transitionCanvas.sortingOrder = 32767; // Topmost layer

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        transitionCanvasGroup = canvasObj.GetComponent<CanvasGroup>();
        transitionCanvasGroup.alpha = 0f;
        transitionCanvasGroup.interactable = false;
        transitionCanvasGroup.blocksRaycasts = false;

        // Create full-screen black/tint image
        GameObject imgObj = new GameObject("FadeImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imgObj.transform.SetParent(canvasObj.transform, false);

        RectTransform rt = imgObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        fadeImage = imgObj.GetComponent<Image>();
        fadeImage.color = fadeColor;
        fadeImage.raycastTarget = false;
    }

    public static void LoadScene(string sceneName, float duration = -1f)
    {
        if (Instance == null)
        {
            GameObject managerObj = new GameObject("SceneTransitionManager");
            Instance = managerObj.AddComponent<SceneTransitionManager>();
        }

        float dur = duration > 0f ? duration : Instance.fadeDuration;
        Instance.StartSceneTransition(sceneName, dur);
    }

    public void StartSceneTransition(string sceneName, float duration = -1f)
    {
        if (isTransitioning) return;
        float dur = duration > 0f ? duration : fadeDuration;
        StartCoroutine(TransitionRoutine(sceneName, dur));
    }

    private IEnumerator TransitionRoutine(string sceneName, float duration)
    {
        isTransitioning = true;
        SetupOverlay();

        if (transitionCanvasGroup != null)
        {
            transitionCanvasGroup.blocksRaycasts = true;
        }

        // 1. Fade Out (0 -> 1)
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smooth = t * t * (3f - 2f * t);

            if (transitionCanvasGroup != null)
            {
                transitionCanvasGroup.alpha = smooth;
            }
            yield return null;
        }

        if (transitionCanvasGroup != null)
        {
            transitionCanvasGroup.alpha = 1f;
        }

        // Small pause at full black for smooth feeling
        yield return new WaitForSecondsRealtime(0.1f);

        // 2. Load Scene Asynchronously
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        if (asyncLoad != null)
        {
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }

        // Brief delay for new scene cameras/lighting to initialize
        yield return new WaitForSecondsRealtime(0.12f);

        // 3. Fade In (1 -> 0)
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smooth = t * t * (3f - 2f * t);

            if (transitionCanvasGroup != null)
            {
                transitionCanvasGroup.alpha = 1f - smooth;
            }
            yield return null;
        }

        if (transitionCanvasGroup != null)
        {
            transitionCanvasGroup.alpha = 0f;
            transitionCanvasGroup.blocksRaycasts = false;
        }

        isTransitioning = false;
    }
}
