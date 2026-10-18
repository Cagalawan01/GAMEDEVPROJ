using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OpeningSceneController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup narrativeCanvasGroup;
    [SerializeField] private TextMeshProUGUI narrativeText;
    [SerializeField] private CanvasGroup titleCardCanvasGroup;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private CanvasGroup tapPromptCanvasGroup;
    [SerializeField] private Button advanceButton;
    [SerializeField] private Button backToMenuButton;

    [Header("Beat 1 Script Lines")]
    [SerializeField] private string[] beat1Lines = new string[]
    {
        "October 14th.",
        "One year ago today, a girl named Elara Veyne was last seen alive.",
        "Nobody agrees on what happened after that."
    };

    [SerializeField] private string gameTitle = "The Day the Blue Flowers Died";
    [SerializeField] private string chapterSubtitle = "Chapter I  \u2014  The Long Walk In";

    [Header("Timing Settings (Beat 1: Title Card)")]
    [SerializeField] private float initialBlackPause = 0.8f;
    [SerializeField] private float lineFadeInDuration = 1.5f;
    [SerializeField] private float lineHoldDuration = 2.6f;
    [SerializeField] private float lineFadeOutDuration = 1.2f;
    [SerializeField] private float pauseAfterLine = 2.0f;
    [SerializeField] private float holdBeforeTitleDuration = 3.0f;
    [SerializeField] private float titleFadeInDuration = 2.2f;
    [SerializeField] private float titleHoldDuration = 2.8f;
    [SerializeField] private float titleFadeOutDuration = 1.4f;
    [SerializeField] private string nextSceneName = "SchoolScene";

    [Header("Ambient Audio (No Music Until Greenhouse)")]
    [SerializeField] private AudioSource windAudioSource;
    [SerializeField] private AudioSource bellAudioSource;
    [SerializeField] private AudioClip windClip;
    [SerializeField] private AudioClip distantSchoolBellClip;

    private bool tapRequested = false;
    private bool isTransitioningOut = false;

    private void Start()
    {
        if (advanceButton != null)
        {
            advanceButton.onClick.RemoveAllListeners();
            advanceButton.onClick.AddListener(OnAdvancePressed);
        }

        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.RemoveAllListeners();
            backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
        }

        // Start from pure black screen at runtime
        if (narrativeCanvasGroup != null) narrativeCanvasGroup.alpha = 0f;
        if (titleCardCanvasGroup != null) titleCardCanvasGroup.alpha = 0f;
        if (tapPromptCanvasGroup != null) tapPromptCanvasGroup.alpha = 0f;

        if (titleText != null && !string.IsNullOrEmpty(gameTitle))
        {
            titleText.text = gameTitle;
        }

        if (subtitleText != null && !string.IsNullOrEmpty(chapterSubtitle))
        {
            subtitleText.text = chapterSubtitle;
        }

        SetupAmbientAudio();
        StartCoroutine(RunBeat1Sequence());
    }

    private void Update()
    {
        bool advanceInput = false;
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame ||
                UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame)
            {
                advanceInput = true;
            }
        }
        if (UnityEngine.InputSystem.Mouse.current != null &&
            UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            advanceInput = true;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
        {
            advanceInput = true;
        }
#endif
        if (advanceInput)
        {
            OnAdvancePressed();
        }
    }

    public void OnAdvancePressed()
    {
        tapRequested = true;
    }

    private IEnumerator RunBeat1Sequence()
    {
        // Initial pause on black screen while wind and distant school bell begin
        yield return StartCoroutine(WaitOrTap(initialBlackPause));

        // Trigger the distant school bell fading far away
        if (bellAudioSource != null && bellAudioSource.clip != null)
        {
            bellAudioSource.volume = 0.35f * SettingsMenu.GlobalSoundVolume;
            bellAudioSource.Play();
        }

        // Each line fades in alone and holds before the next
        for (int i = 0; i < beat1Lines.Length; i++)
        {
            if (narrativeText != null)
            {
                narrativeText.text = beat1Lines[i];
            }

            tapRequested = false;

            // Fade in the current line
            yield return StartCoroutine(FadeCanvasGroup(narrativeCanvasGroup, 0f, 1f, lineFadeInDuration, allowSkip: true));

            // Show subtle prompt while holding; advances after lineHoldDuration OR immediately on tap/click
            tapRequested = false;
            Coroutine promptFade = null;
            if (tapPromptCanvasGroup != null)
            {
                promptFade = StartCoroutine(FadeCanvasGroup(tapPromptCanvasGroup, 0f, 0.55f, 0.6f, allowSkip: false));
            }

            yield return StartCoroutine(WaitOrTap(lineHoldDuration));

            if (promptFade != null) StopCoroutine(promptFade);
            if (tapPromptCanvasGroup != null) tapPromptCanvasGroup.alpha = 0f;

            // Fade out the current line
            tapRequested = false;
            yield return StartCoroutine(FadeCanvasGroup(narrativeCanvasGroup, 1f, 0f, lineFadeOutDuration, allowSkip: true));

            // 2-second pause after every line
            if (i < beat1Lines.Length - 1)
            {
                yield return StartCoroutine(WaitOrTap(pauseAfterLine));
            }
        }

        // "(Hold for three seconds, then the title fades in.)"
        yield return StartCoroutine(WaitOrTap(holdBeforeTitleDuration));

        // Fade in the Title Card
        tapRequested = false;
        yield return StartCoroutine(FadeCanvasGroup(titleCardCanvasGroup, 0f, 1f, titleFadeInDuration, allowSkip: true));

        // Hold on title card, then fade out and redirect to SchoolScene
        tapRequested = false;
        if (tapPromptCanvasGroup != null)
        {
            StartCoroutine(FadeCanvasGroup(tapPromptCanvasGroup, 0f, 0.55f, 0.6f, allowSkip: false));
        }

        yield return StartCoroutine(WaitOrTap(titleHoldDuration));

        if (tapPromptCanvasGroup != null) tapPromptCanvasGroup.alpha = 0f;

        // Fade out title card to black
        tapRequested = false;
        yield return StartCoroutine(FadeCanvasGroup(titleCardCanvasGroup, 1f, 0f, titleFadeOutDuration, allowSkip: true));

        RedirectToSchoolScene();
    }

    private void RedirectToSchoolScene()
    {
        if (isTransitioningOut) return;
        isTransitioningOut = true;

        string target = string.IsNullOrEmpty(nextSceneName) ? "SchoolScene" : nextSceneName;
        Debug.Log($"<color=cyan>[OpeningSceneController] Opening scene finished. Redirecting to {target}...</color>");
        SceneTransitionManager.LoadScene(target, 0.8f);
    }

    private IEnumerator WaitOrTap(float duration)
    {
        tapRequested = false;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (tapRequested)
            {
                tapRequested = false;
                yield break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration, bool allowSkip)
    {
        if (cg == null) yield break;

        cg.alpha = from;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (allowSkip && tapRequested)
            {
                tapRequested = false;
                break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
            float smooth = t * t * (3f - 2f * t);
            cg.alpha = Mathf.Lerp(from, to, smooth);
            yield return null;
        }

        cg.alpha = to;
    }

    private void SetupAmbientAudio()
    {
        float sfxVol = SettingsMenu.GlobalSoundVolume;

        if (windAudioSource == null)
        {
            GameObject windObj = new GameObject("AmbientWindAudio");
            windObj.transform.SetParent(transform, false);
            windAudioSource = windObj.AddComponent<AudioSource>();
        }

        if (windClip == null)
        {
            windClip = CreateProceduralWindClip(8f);
        }

        windAudioSource.clip = windClip;
        windAudioSource.loop = true;
        windAudioSource.volume = 0.22f * sfxVol;
        windAudioSource.Play();

        if (bellAudioSource == null)
        {
            GameObject bellObj = new GameObject("DistantSchoolBellAudio");
            bellObj.transform.SetParent(transform, false);
            bellAudioSource = bellObj.AddComponent<AudioSource>();
        }

        if (distantSchoolBellClip == null)
        {
            distantSchoolBellClip = CreateDistantSchoolBellClip(6.5f);
        }

        bellAudioSource.clip = distantSchoolBellClip;
        bellAudioSource.loop = false;
        bellAudioSource.playOnAwake = false;
    }

    private AudioClip CreateProceduralWindClip(float durationSeconds)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * durationSeconds);
        float[] samples = new float[sampleCount];

        float lp1 = 0f;
        float lp2 = 0f;
        System.Random rng = new System.Random(1014);

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp1 = lp1 * 0.985f + white * 0.015f;
            lp2 = lp2 * 0.96f + lp1 * 0.04f;

            float swell = 0.65f + 0.35f * Mathf.Sin(t * Mathf.PI * 2f / durationSeconds);
            samples[i] = Mathf.Clamp(lp2 * 2.5f * swell, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("ProceduralAutumnWind", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateDistantSchoolBellClip(float durationSeconds)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * durationSeconds);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float val = 0f;

            if (t >= 0f)
            {
                float env1 = Mathf.Exp(-t * 1.15f) * Mathf.Clamp01(t / 0.04f);
                val += Mathf.Sin(2f * Mathf.PI * 329.63f * t) * 0.45f * env1;
                val += Mathf.Sin(2f * Mathf.PI * 659.25f * t) * 0.15f * Mathf.Exp(-t * 1.8f);
            }

            if (t >= 1.6f)
            {
                float t2 = t - 1.6f;
                float env2 = Mathf.Exp(-t2 * 0.85f) * Mathf.Clamp01(t2 / 0.05f);
                val += Mathf.Sin(2f * Mathf.PI * 261.63f * t2) * 0.38f * env2;
                val += Mathf.Sin(2f * Mathf.PI * 523.25f * t2) * 0.12f * Mathf.Exp(-t2 * 1.5f);
            }

            samples[i] = Mathf.Clamp(val * 0.5f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("DistantSchoolBell", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public void OnBackToMenuClicked()
    {
        SceneTransitionManager.LoadScene("MenuScreen", 0.6f);
    }
}
