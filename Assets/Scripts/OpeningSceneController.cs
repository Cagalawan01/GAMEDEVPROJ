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

    [Header("Name Input Prompt")]
    [SerializeField] private CanvasGroup namePromptCanvasGroup;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button confirmNameButton;

    [Header("Beat 1 Script Lines")]
    [SerializeField] private string[] beat1Lines = new string[]
    {
        "October 14th.",
        "One year ago today, a girl named Elara Veyne was last seen alive.",
        "Nobody agrees on what happened after that."
    };

    [SerializeField] private string gameTitle = "RESME";
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

    public const string PLAYER_NAME_PREF_KEY = "PlayerName";

    private bool tapRequested = false;
    private bool isAskingName = false;
    private bool nameConfirmed = false;
    private bool isTransitioningOut = false;

    private void Awake()
    {
        RemoveDefaultButtonTexts();
        ApplyTitleStyling();
    }

    private void RemoveDefaultButtonTexts()
    {
        Text[] legacyTexts = GetComponentsInChildren<Text>(true);
        foreach (Text t in legacyTexts)
        {
            if (t != null && (t.text == "Button" || t.gameObject.name.Contains("Text (Legacy)")))
            {
                Destroy(t.gameObject);
            }
        }
    }

    private void ApplyTitleStyling()
    {
        if (string.IsNullOrEmpty(gameTitle) || gameTitle == "The Day the Blue Flowers Died")
        {
            gameTitle = "RESME";
        }

        if (titleText != null)
        {
            titleText.text = gameTitle;
            titleText.fontSize = 160f;
            titleText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            titleText.color = Color.white;
            titleText.enableVertexGradient = true;
            Color bottomGrey = new Color(0.6981132f, 0.6981132f, 0.6981132f, 1f);
            titleText.colorGradient = new VertexGradient(Color.white, Color.white, bottomGrey, bottomGrey);
            titleText.textWrappingMode = TextWrappingModes.NoWrap;
            titleText.overflowMode = TextOverflowModes.Overflow;
        }
    }

    private void Start()
    {
        RemoveDefaultButtonTexts();
        ApplyTitleStyling();

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
        if (namePromptCanvasGroup != null)
        {
            namePromptCanvasGroup.alpha = 0f;
            namePromptCanvasGroup.interactable = false;
            namePromptCanvasGroup.blocksRaycasts = false;
            namePromptCanvasGroup.gameObject.SetActive(false);
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
        if (isAskingName)
        {
            bool enterPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                (UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame ||
                 UnityEngine.InputSystem.Keyboard.current.numpadEnterKey.wasPressedThisFrame))
            {
                enterPressed = true;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                enterPressed = true;
            }
#endif
            if (enterPressed && nameInputField != null && !string.IsNullOrWhiteSpace(nameInputField.text))
            {
                OnConfirmNameClicked();
            }
            return;
        }

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
        if (isAskingName) return;
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

        // Hold on title card, then fade out
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

        // Ask the player for their name throughout the story before redirecting to SchoolScene
        yield return StartCoroutine(PromptPlayerNameRoutine());

        RedirectToSchoolScene();
    }

    private static Sprite LoadResourceSprite(string resourceName)
    {
        Sprite s = Resources.Load<Sprite>(resourceName);
        if (s != null) return s;

        Sprite[] all = Resources.LoadAll<Sprite>(resourceName);
        if (all != null && all.Length > 0) return all[0];

        Texture2D tex = Resources.Load<Texture2D>(resourceName);
        if (tex != null)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
        return null;
    }

    private void EnsureNamePromptUI()
    {
        Sprite panelSprite = LoadResourceSprite("name_prompt_panel");
        Sprite inputFrameSprite = LoadResourceSprite("name_input_frame");
        Sprite confirmBtnSprite = LoadResourceSprite("name_confirm_button");

        if (namePromptCanvasGroup != null && nameInputField != null && confirmNameButton != null)
        {
            Image existingPanelImg = namePromptCanvasGroup.GetComponent<Image>();
            if (existingPanelImg != null && existingPanelImg.sprite == null && panelSprite != null)
            {
                existingPanelImg.sprite = panelSprite;
                existingPanelImg.color = Color.white;
            }

            Image existingInputImg = nameInputField.GetComponent<Image>();
            if (existingInputImg != null && existingInputImg.sprite == null && inputFrameSprite != null)
            {
                existingInputImg.sprite = inputFrameSprite;
                existingInputImg.color = Color.white;
            }

            Image existingBtnImg = confirmNameButton.GetComponent<Image>();
            if (existingBtnImg != null && existingBtnImg.sprite == null && confirmBtnSprite != null)
            {
                existingBtnImg.sprite = confirmBtnSprite;
                existingBtnImg.color = Color.white;
            }

            confirmNameButton.onClick.RemoveAllListeners();
            confirmNameButton.onClick.AddListener(OnConfirmNameClicked);
            return;
        }

        TMP_FontAsset defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (defaultFont == null)
        {
            defaultFont = subtitleText != null ? subtitleText.font : (narrativeText != null ? narrativeText.font : null);
        }
        TMP_FontAsset headerFont = titleText != null ? titleText.font : defaultFont;

        // Outer Ornate Gothic Panel Container (matching reference proportions & corner filigree)
        GameObject promptRoot = new GameObject("NamePromptContainer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        promptRoot.transform.SetParent(transform, false);
        RectTransform rootRect = promptRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.22f, 0.24f);
        rootRect.anchorMax = new Vector2(0.78f, 0.76f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image panelImg = promptRoot.GetComponent<Image>();
        if (panelSprite != null)
        {
            panelImg.sprite = panelSprite;
            panelImg.type = Image.Type.Simple;
            panelImg.preserveAspect = false;
            panelImg.color = Color.white;
        }
        else
        {
            panelImg.color = new Color(0.04f, 0.09f, 0.18f, 0.96f);
        }

        namePromptCanvasGroup = promptRoot.GetComponent<CanvasGroup>();
        namePromptCanvasGroup.alpha = 0f;
        namePromptCanvasGroup.interactable = false;
        namePromptCanvasGroup.blocksRaycasts = false;

        // 1. Prompt Question Text ("What will your name be?" in Gothic Header Font)
        GameObject labelObj = new GameObject("NamePromptLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(promptRoot.transform, false);
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.10f, 0.68f);
        labelRect.anchorMax = new Vector2(0.90f, 0.88f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI labelTmp = labelObj.GetComponent<TextMeshProUGUI>();
        labelTmp.text = "What will your name be?";
        if (headerFont != null) labelTmp.font = headerFont;
        labelTmp.fontSize = 52f;
        labelTmp.alignment = TextAlignmentOptions.Center;
        labelTmp.color = new Color(0.93f, 0.97f, 1f, 1f);
        labelTmp.raycastTarget = false;

        // 2. Ornate Input Field Frame (with diamond side medallions & top/bottom crests)
        GameObject inputObj = new GameObject("NameInputField", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
        inputObj.transform.SetParent(promptRoot.transform, false);
        RectTransform inputRect = inputObj.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.16f, 0.37f);
        inputRect.anchorMax = new Vector2(0.84f, 0.63f);
        inputRect.offsetMin = Vector2.zero;
        inputRect.offsetMax = Vector2.zero;

        Image inputBg = inputObj.GetComponent<Image>();
        if (inputFrameSprite != null)
        {
            inputBg.sprite = inputFrameSprite;
            inputBg.type = Image.Type.Simple;
            inputBg.preserveAspect = false;
            inputBg.color = Color.white;
        }
        else
        {
            inputBg.color = new Color(0.05f, 0.12f, 0.22f, 0.95f);
        }

        // Text Area (Viewport inside the ornate frame)
        GameObject textAreaObj = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        textAreaObj.transform.SetParent(inputObj.transform, false);
        RectTransform textAreaRect = textAreaObj.GetComponent<RectTransform>();
        textAreaRect.anchorMin = new Vector2(0.08f, 0.18f);
        textAreaRect.anchorMax = new Vector2(0.92f, 0.82f);
        textAreaRect.offsetMin = Vector2.zero;
        textAreaRect.offsetMax = Vector2.zero;

        // Placeholder Text (Default readable font)
        GameObject placeholderObj = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        placeholderObj.transform.SetParent(textAreaObj.transform, false);
        RectTransform phRect = placeholderObj.GetComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.offsetMin = Vector2.zero;
        phRect.offsetMax = Vector2.zero;

        TextMeshProUGUI phTmp = placeholderObj.GetComponent<TextMeshProUGUI>();
        phTmp.text = "Enter your name...";
        if (defaultFont != null) phTmp.font = defaultFont;
        phTmp.fontSize = 30f;
        phTmp.fontStyle = FontStyles.Italic;
        phTmp.alignment = TextAlignmentOptions.Center;
        phTmp.color = new Color(0.55f, 0.70f, 0.86f, 0.55f);
        phTmp.raycastTarget = false;

        // Input Text (Default readable font so player name is clear & easy to read)
        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(textAreaObj.transform, false);
        RectTransform txtRect = textObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;

        TextMeshProUGUI txtTmp = textObj.GetComponent<TextMeshProUGUI>();
        txtTmp.text = "";
        if (defaultFont != null) txtTmp.font = defaultFont;
        txtTmp.fontSize = 34f;
        txtTmp.alignment = TextAlignmentOptions.Center;
        txtTmp.color = new Color(0.94f, 0.98f, 1f, 1f);
        txtTmp.raycastTarget = false;

        nameInputField = inputObj.GetComponent<TMP_InputField>();
        nameInputField.textViewport = textAreaRect;
        nameInputField.textComponent = txtTmp;
        nameInputField.placeholder = phTmp;
        nameInputField.fontAsset = defaultFont;
        nameInputField.characterLimit = 24;
        nameInputField.contentType = TMP_InputField.ContentType.Standard;
        nameInputField.lineType = TMP_InputField.LineType.SingleLine;
        nameInputField.customCaretColor = true;
        nameInputField.caretColor = new Color(0.75f, 0.90f, 1f, 1f);
        nameInputField.caretWidth = 2;

        // 3. Ornate Confirm Button (Scalloped diamond-ended frame matching reference)
        GameObject btnObj = new GameObject("ConfirmNameButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(promptRoot.transform, false);
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.31f, 0.11f);
        btnRect.anchorMax = new Vector2(0.69f, 0.29f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;

        Image btnImg = btnObj.GetComponent<Image>();
        if (confirmBtnSprite != null)
        {
            btnImg.sprite = confirmBtnSprite;
            btnImg.type = Image.Type.Simple;
            btnImg.preserveAspect = false;
            btnImg.color = Color.white;
        }
        else
        {
            btnImg.color = new Color(0.08f, 0.18f, 0.34f, 0.96f);
        }

        GameObject btnTextObj = new GameObject("Text (TMP)", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        btnTextObj.transform.SetParent(btnObj.transform, false);
        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI btnTmp = btnTextObj.GetComponent<TextMeshProUGUI>();
        btnTmp.text = "Confirm";
        if (headerFont != null) btnTmp.font = headerFont;
        btnTmp.fontSize = 38f;
        btnTmp.alignment = TextAlignmentOptions.Center;
        btnTmp.color = new Color(0.93f, 0.97f, 1f, 1f);
        btnTmp.raycastTarget = false;

        confirmNameButton = btnObj.GetComponent<Button>();
        ColorBlock cb = confirmNameButton.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.18f, 1.28f, 1.40f, 1f);
        cb.pressedColor = new Color(0.75f, 0.88f, 1f, 1f);
        confirmNameButton.colors = cb;
        confirmNameButton.onClick.RemoveAllListeners();
        confirmNameButton.onClick.AddListener(OnConfirmNameClicked);
    }

    private IEnumerator PromptPlayerNameRoutine()
    {
        EnsureNamePromptUI();

        isAskingName = true;
        nameConfirmed = false;

        // Disable full-screen AdvanceTrigger while player interacts with the input field
        if (advanceButton != null)
        {
            advanceButton.gameObject.SetActive(false);
        }

        if (namePromptCanvasGroup != null)
        {
            namePromptCanvasGroup.gameObject.SetActive(true);
            namePromptCanvasGroup.interactable = true;
            namePromptCanvasGroup.blocksRaycasts = true;
        }

        if (nameInputField != null)
        {
            string existingName = PlayerPrefs.GetString(PLAYER_NAME_PREF_KEY, "");
            nameInputField.text = existingName;
        }

        yield return StartCoroutine(FadeCanvasGroup(namePromptCanvasGroup, 0f, 1f, 0.8f, allowSkip: false));

        if (nameInputField != null)
        {
            nameInputField.Select();
            nameInputField.ActivateInputField();
        }

        while (!nameConfirmed)
        {
            yield return null;
        }

        if (namePromptCanvasGroup != null)
        {
            namePromptCanvasGroup.interactable = false;
            namePromptCanvasGroup.blocksRaycasts = false;
        }

        yield return StartCoroutine(FadeCanvasGroup(namePromptCanvasGroup, 1f, 0f, 0.6f, allowSkip: false));

        isAskingName = false;
    }

    public void OnConfirmNameClicked()
    {
        if (!isAskingName || nameConfirmed) return;

        string enteredName = nameInputField != null ? nameInputField.text.Trim() : "";
        if (string.IsNullOrEmpty(enteredName))
        {
            enteredName = "Protagonist";
        }

        PlayerPrefs.SetString(PLAYER_NAME_PREF_KEY, enteredName);
        PlayerPrefs.Save();

        Debug.Log($"<color=cyan>[OpeningSceneController] Player name set to: {enteredName}</color>");
        nameConfirmed = true;
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
