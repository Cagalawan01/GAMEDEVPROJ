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
        TMP_FontAsset defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (defaultFont == null)
        {
            defaultFont = subtitleText != null ? subtitleText.font : (narrativeText != null ? narrativeText.font : null);
        }
        TMP_FontAsset headerFont = titleText != null ? titleText.font : defaultFont;

        if (namePromptCanvasGroup != null && nameInputField != null && confirmNameButton != null)
        {
            // 1. Fullscreen pure black container
            RectTransform existingRootRect = namePromptCanvasGroup.GetComponent<RectTransform>();
            if (existingRootRect != null)
            {
                existingRootRect.anchorMin = Vector2.zero;
                existingRootRect.anchorMax = Vector2.one;
                existingRootRect.offsetMin = Vector2.zero;
                existingRootRect.offsetMax = Vector2.zero;
            }

            Image existingPanelImg = namePromptCanvasGroup.GetComponent<Image>();
            if (existingPanelImg != null)
            {
                existingPanelImg.sprite = null;
                existingPanelImg.color = Color.black;
            }

            // 2. Center Prompt Label ("What will your name be?")
            TextMeshProUGUI existingLabel = namePromptCanvasGroup.GetComponentInChildren<TextMeshProUGUI>();
            if (existingLabel != null && existingLabel.gameObject.name.Contains("Label"))
            {
                RectTransform lr = existingLabel.GetComponent<RectTransform>();
                lr.anchorMin = new Vector2(0.20f, 0.58f);
                lr.anchorMax = new Vector2(0.80f, 0.68f);
                lr.offsetMin = Vector2.zero;
                lr.offsetMax = Vector2.zero;
                existingLabel.text = "What will your name be?";
                existingLabel.fontSize = 44f;
                existingLabel.color = Color.white;
                existingLabel.alignment = TextAlignmentOptions.Center;
            }

            // 3. Black Input Box
            RectTransform existingInputRect = nameInputField.GetComponent<RectTransform>();
            if (existingInputRect != null)
            {
                existingInputRect.anchorMin = new Vector2(0.33f, 0.46f);
                existingInputRect.anchorMax = new Vector2(0.67f, 0.54f);
                existingInputRect.offsetMin = Vector2.zero;
                existingInputRect.offsetMax = Vector2.zero;
            }

            Image existingInputImg = nameInputField.GetComponent<Image>();
            if (existingInputImg != null)
            {
                existingInputImg.sprite = null;
                existingInputImg.color = new Color(0.02f, 0.02f, 0.02f, 1f);
            }

            ColorBlock inCb = nameInputField.colors;
            inCb.normalColor = Color.white;
            inCb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            inCb.selectedColor = Color.white;
            nameInputField.colors = inCb;

            Outline inOutline = nameInputField.GetComponent<Outline>();
            if (inOutline != null)
            {
                Destroy(inOutline);
            }

            if (nameInputField.textComponent != null)
            {
                nameInputField.textComponent.color = Color.white;
                nameInputField.textComponent.fontSize = 32f;
                nameInputField.textComponent.alignment = TextAlignmentOptions.Center;
            }
            if (nameInputField.placeholder is TextMeshProUGUI phTmp)
            {
                phTmp.color = new Color(0.45f, 0.45f, 0.45f, 0.6f);
                phTmp.fontSize = 28f;
                phTmp.alignment = TextAlignmentOptions.Center;
            }
            nameInputField.caretColor = Color.white;

            // 4. Clean Black Confirm Button
            RectTransform existingBtnRect = confirmNameButton.GetComponent<RectTransform>();
            if (existingBtnRect != null)
            {
                existingBtnRect.anchorMin = new Vector2(0.42f, 0.34f);
                existingBtnRect.anchorMax = new Vector2(0.58f, 0.42f);
                existingBtnRect.offsetMin = Vector2.zero;
                existingBtnRect.offsetMax = Vector2.zero;
            }

            Image existingBtnImg = confirmNameButton.GetComponent<Image>();
            if (existingBtnImg != null)
            {
                existingBtnImg.sprite = null;
                existingBtnImg.color = new Color(0.04f, 0.04f, 0.04f, 1f);
            }

            ColorBlock cb = confirmNameButton.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            cb.selectedColor = Color.white;
            confirmNameButton.colors = cb;

            confirmNameButton.onClick.RemoveAllListeners();
            confirmNameButton.onClick.AddListener(OnConfirmNameClicked);
            return;
        }

        // Fullscreen pure black background container
        GameObject promptRoot = new GameObject("NamePromptContainer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        promptRoot.transform.SetParent(transform, false);
        RectTransform rootRect = promptRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image panelImg = promptRoot.GetComponent<Image>();
        panelImg.sprite = null;
        panelImg.color = Color.black;

        namePromptCanvasGroup = promptRoot.GetComponent<CanvasGroup>();
        namePromptCanvasGroup.alpha = 0f;
        namePromptCanvasGroup.interactable = false;
        namePromptCanvasGroup.blocksRaycasts = false;

        // 1. Prompt Question Text ("What will your name be?")
        GameObject labelObj = new GameObject("NamePromptLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(promptRoot.transform, false);
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.20f, 0.58f);
        labelRect.anchorMax = new Vector2(0.80f, 0.68f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI labelTmp = labelObj.GetComponent<TextMeshProUGUI>();
        labelTmp.text = "What will your name be?";
        if (headerFont != null) labelTmp.font = headerFont;
        labelTmp.fontSize = 44f;
        labelTmp.alignment = TextAlignmentOptions.Center;
        labelTmp.color = Color.white;
        labelTmp.raycastTarget = false;

        // 2. Pure Black Input Field with subtle outline
        GameObject inputObj = new GameObject("NameInputField", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
        inputObj.transform.SetParent(promptRoot.transform, false);
        RectTransform inputRect = inputObj.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.33f, 0.46f);
        inputRect.anchorMax = new Vector2(0.67f, 0.54f);
        inputRect.offsetMin = Vector2.zero;
        inputRect.offsetMax = Vector2.zero;

        Image inputBg = inputObj.GetComponent<Image>();
        inputBg.sprite = null;
        inputBg.color = new Color(0.02f, 0.02f, 0.02f, 1f);

        GameObject textAreaObj = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        textAreaObj.transform.SetParent(inputObj.transform, false);
        RectTransform textAreaRect = textAreaObj.GetComponent<RectTransform>();
        textAreaRect.anchorMin = new Vector2(0.05f, 0.10f);
        textAreaRect.anchorMax = new Vector2(0.95f, 0.90f);
        textAreaRect.offsetMin = Vector2.zero;
        textAreaRect.offsetMax = Vector2.zero;

        GameObject placeholderObj = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        placeholderObj.transform.SetParent(textAreaObj.transform, false);
        RectTransform phRect = placeholderObj.GetComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.offsetMin = Vector2.zero;
        phRect.offsetMax = Vector2.zero;

        TextMeshProUGUI phDynTmp = placeholderObj.GetComponent<TextMeshProUGUI>();
        phDynTmp.text = "Enter your name...";
        if (defaultFont != null) phDynTmp.font = defaultFont;
        phDynTmp.fontSize = 28f;
        phDynTmp.fontStyle = FontStyles.Italic;
        phDynTmp.alignment = TextAlignmentOptions.Center;
        phDynTmp.color = new Color(0.45f, 0.45f, 0.45f, 0.6f);
        phDynTmp.raycastTarget = false;

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(textAreaObj.transform, false);
        RectTransform txtRect = textObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;

        TextMeshProUGUI txtDynTmp = textObj.GetComponent<TextMeshProUGUI>();
        txtDynTmp.text = "";
        if (defaultFont != null) txtDynTmp.font = defaultFont;
        txtDynTmp.fontSize = 32f;
        txtDynTmp.alignment = TextAlignmentOptions.Center;
        txtDynTmp.color = Color.white;
        txtDynTmp.raycastTarget = false;

        nameInputField = inputObj.GetComponent<TMP_InputField>();
        nameInputField.textViewport = textAreaRect;
        nameInputField.textComponent = txtDynTmp;
        nameInputField.placeholder = phDynTmp;
        nameInputField.fontAsset = defaultFont;
        nameInputField.characterLimit = 24;
        nameInputField.contentType = TMP_InputField.ContentType.Standard;
        nameInputField.lineType = TMP_InputField.LineType.SingleLine;
        nameInputField.customCaretColor = true;
        nameInputField.caretColor = Color.white;
        nameInputField.caretWidth = 2;

        // 3. Black Confirm Button with clean border
        GameObject btnObj = new GameObject("ConfirmNameButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
        btnObj.transform.SetParent(promptRoot.transform, false);
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.42f, 0.34f);
        btnRect.anchorMax = new Vector2(0.58f, 0.42f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;

        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.sprite = null;
        btnImg.color = new Color(0.08f, 0.08f, 0.08f, 1f);

        Outline btnOutlineDyn = btnObj.GetComponent<Outline>();
        btnOutlineDyn.effectColor = new Color(0.38f, 0.38f, 0.38f, 0.90f);
        btnOutlineDyn.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject btnTextObj = new GameObject("Text (TMP)", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        btnTextObj.transform.SetParent(btnObj.transform, false);
        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI btnDynTmp = btnTextObj.GetComponent<TextMeshProUGUI>();
        btnDynTmp.text = "Confirm";
        if (headerFont != null) btnDynTmp.font = headerFont;
        btnDynTmp.fontSize = 30f;
        btnDynTmp.alignment = TextAlignmentOptions.Center;
        btnDynTmp.color = Color.white;
        btnDynTmp.raycastTarget = false;

        confirmNameButton = btnObj.GetComponent<Button>();
        ColorBlock cbDyn = confirmNameButton.colors;
        cbDyn.normalColor = new Color(0.08f, 0.08f, 0.08f, 1f);
        cbDyn.highlightedColor = new Color(0.20f, 0.20f, 0.20f, 1f);
        cbDyn.pressedColor = new Color(0.04f, 0.04f, 0.04f, 1f);
        cbDyn.selectedColor = new Color(0.08f, 0.08f, 0.08f, 1f);
        confirmNameButton.colors = cbDyn;
        confirmNameButton.onClick.RemoveAllListeners();
        confirmNameButton.onClick.AddListener(OnConfirmNameClicked);
    }

    private IEnumerator PromptPlayerNameRoutine()
    {
        EnsureNamePromptUI();

        isAskingName = true;
        nameConfirmed = false;

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

        yield return StartCoroutine(FadeCanvasGroup(namePromptCanvasGroup, 1f, 0f, 0.7f, allowSkip: false));

        isAskingName = false;
    }

    public void OnConfirmNameClicked()
    {
        if (!isAskingName || nameConfirmed) return;
        StartCoroutine(ConfirmWithBloodSequence());
    }

    private IEnumerator ConfirmWithBloodSequence()
    {
        isAskingName = false;

        string enteredName = nameInputField != null ? nameInputField.text.Trim() : "";
        if (string.IsNullOrEmpty(enteredName))
        {
            enteredName = "Player";
            if (nameInputField != null)
            {
                nameInputField.text = "Player";
            }
        }

        PlayerPrefs.SetString(PLAYER_NAME_PREF_KEY, enteredName);
        PlayerPrefs.Save();
        Debug.Log($"<color=red>[OpeningSceneController] Player name confirmed with blood: {enteredName}</color>");

        if (nameInputField != null) nameInputField.interactable = false;
        if (confirmNameButton != null) confirmNameButton.interactable = false;

        yield return StartCoroutine(PlayConfirmButtonBloodAnimation());

        nameConfirmed = true;
    }

        private IEnumerator PlayConfirmButtonBloodAnimation()
    {
        if (confirmNameButton == null) yield break;

        RectTransform btnRect = confirmNameButton.GetComponent<RectTransform>();
        Vector2 origPos = btnRect.anchoredPosition;
        TextMeshProUGUI btnText = confirmNameButton.GetComponentInChildren<TextMeshProUGUI>();

        // 1. Play knife stab confirm audio
        AudioClip eerieClip = Resources.Load<AudioClip>("knifestabfinal");
        if (eerieClip == null) eerieClip = Resources.Load<AudioClip>("knifstabfinal");
        if (eerieClip == null) eerieClip = Resources.Load<AudioClip>("blood_confirm_custom");
        if (eerieClip == null) eerieClip = CreateEerieConfirmClip();
        if (eerieClip != null)
        {
            AudioSource sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = 1f * SettingsMenu.GlobalSoundVolume;
            sfxSource.PlayOneShot(eerieClip);
            Destroy(sfxSource, eerieClip.length + 0.5f);
        }

        Sprite bloodSlashSprite = LoadResourceSprite("blood_horizontal_slash");
        if (bloodSlashSprite == null) bloodSlashSprite = LoadResourceSprite("blood_confirm_splatter");

        // 2. Layer 1: Dark Coagulated Blood / Drop-Shadow (provides depth & contact shadow)
        GameObject shadowObj = new GameObject("BloodShadowLayer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        shadowObj.transform.SetParent(confirmNameButton.transform, false);
        shadowObj.transform.SetAsLastSibling();

        RectTransform shadowRect = shadowObj.GetComponent<RectTransform>();
        shadowRect.anchorMin = new Vector2(-0.25f, -0.68f);
        shadowRect.anchorMax = new Vector2(1.25f, 1.42f);
        shadowRect.offsetMin = Vector2.zero;
        shadowRect.offsetMax = Vector2.zero;

        Image shadowImg = shadowObj.GetComponent<Image>();
        shadowImg.sprite = bloodSlashSprite;
        shadowImg.type = Image.Type.Filled;
        shadowImg.fillMethod = Image.FillMethod.Horizontal;
        shadowImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        shadowImg.fillAmount = 0f;
        shadowImg.color = new Color(0.12f, 0.01f, 0.01f, 0.88f);
        shadowImg.raycastTarget = false;

        // 3. Layer 2: Main Rich Arterial Crimson Blood Slash (pure white tint preserves full glossy 3D artwork)
        GameObject bloodOverlayObj = new GameObject("BloodArterialLayer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bloodOverlayObj.transform.SetParent(confirmNameButton.transform, false);
        bloodOverlayObj.transform.SetAsLastSibling();

        RectTransform bloodRect = bloodOverlayObj.GetComponent<RectTransform>();
        bloodRect.anchorMin = new Vector2(-0.25f, -0.65f);
        bloodRect.anchorMax = new Vector2(1.25f, 1.45f);
        bloodRect.offsetMin = Vector2.zero;
        bloodRect.offsetMax = Vector2.zero;

        Image bloodImg = bloodOverlayObj.GetComponent<Image>();
        bloodImg.sprite = bloodSlashSprite;
        bloodImg.type = Image.Type.Filled;
        bloodImg.fillMethod = Image.FillMethod.Horizontal;
        bloodImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        bloodImg.fillAmount = 0f;
        bloodImg.color = Color.white;
        bloodImg.raycastTarget = false;

        // 4. Layer 3: Wet Specular Gloss Gleam (dynamic liquid highlight across top of slash)
        GameObject gleamObj = new GameObject("BloodGlossGleam", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        gleamObj.transform.SetParent(confirmNameButton.transform, false);
        gleamObj.transform.SetAsLastSibling();

        RectTransform gleamRect = gleamObj.GetComponent<RectTransform>();
        gleamRect.anchorMin = new Vector2(-0.25f, -0.62f);
        gleamRect.anchorMax = new Vector2(1.25f, 1.48f);
        gleamRect.offsetMin = Vector2.zero;
        gleamRect.offsetMax = Vector2.zero;

        Image gleamImg = gleamObj.GetComponent<Image>();
        gleamImg.sprite = bloodSlashSprite;
        gleamImg.type = Image.Type.Filled;
        gleamImg.fillMethod = Image.FillMethod.Horizontal;
        gleamImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        gleamImg.fillAmount = 0f;
        gleamImg.color = new Color(1f, 0.35f, 0.35f, 0.45f);
        gleamImg.raycastTarget = false;

        // 5. Horizontal arterial spray droplets bursting outward
        int dropCount = 16;
        GameObject[] droplets = new GameObject[dropCount];
        Vector2[] dropVelocities = new Vector2[dropCount];
        RectTransform[] dropRects = new RectTransform[dropCount];
        Sprite burstSprite = LoadResourceSprite("blood_burst");

        for (int i = 0; i < dropCount; i++)
        {
            GameObject dropObj = new GameObject($"BloodDrop_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dropObj.transform.SetParent(namePromptCanvasGroup.transform, false);
            dropObj.transform.position = confirmNameButton.transform.position;

            RectTransform dr = dropObj.GetComponent<RectTransform>();
            float size = Random.Range(12f, 32f);
            dr.sizeDelta = new Vector2(size, size);
            dr.anchorMin = new Vector2(0.5f, 0.38f);
            dr.anchorMax = new Vector2(0.5f, 0.38f);

            float spawnX = Random.Range(-140f, 140f);
            dr.anchoredPosition = btnRect.anchoredPosition + new Vector2(spawnX, Random.Range(-16f, 16f));

            Image dImg = dropObj.GetComponent<Image>();
            dImg.sprite = burstSprite;
            dImg.color = new Color(Random.Range(0.65f, 0.95f), 0.02f, 0.02f, 0f);
            dImg.raycastTarget = false;

            float dirX = spawnX >= 0 ? 1f : -1f;
            float speedX = Random.Range(220f, 600f) * dirX;
            float speedY = Random.Range(-140f, 190f);
            dropVelocities[i] = new Vector2(speedX, speedY);
            dropRects[i] = dr;
            droplets[i] = dropObj;
        }

        // 6. Fast razor-sharp horizontal blood strike wipe (0 to 0.22s)
        float slashDuration = 0.22f;
        float elapsed = 0f;

        while (elapsed < slashDuration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / slashDuration);

            float wipe = 1f - Mathf.Pow(1f - p, 3f);
            shadowImg.fillAmount = Mathf.Clamp01(wipe * 1.05f);
            bloodImg.fillAmount = wipe;
            gleamImg.fillAmount = wipe;
            gleamImg.color = new Color(1f, 0.35f, 0.35f, (1f - p) * 0.45f);

            // Violent button shake
            float trauma = (1f - p) * 14f;
            btnRect.anchoredPosition = origPos + new Vector2(Random.Range(-trauma, trauma), Random.Range(-trauma * 0.35f, trauma * 0.35f));

            // Cross out and blood-soak the Confirm text once the slash passes halfway
            if (btnText != null && p >= 0.30f)
            {
                btnText.fontStyle = FontStyles.Strikethrough;
                btnText.color = Color.Lerp(Color.white, new Color(0.85f, 0.08f, 0.08f, 1f), (p - 0.30f) / 0.70f);
            }

            // Spray droplets horizontally
            for (int i = 0; i < dropCount; i++)
            {
                if (dropRects[i] != null)
                {
                    dropRects[i].anchoredPosition += dropVelocities[i] * Time.deltaTime;
                    dropVelocities[i] *= Mathf.Exp(-6f * Time.deltaTime);
                    Image dImg = droplets[i].GetComponent<Image>();
                    if (dImg != null) dImg.color = new Color(dImg.color.r, 0.02f, 0.02f, Mathf.Clamp01(p / 0.15f));
                }
            }

            yield return null;
        }

        btnRect.anchoredPosition = origPos;
        shadowImg.fillAmount = 1f;
        bloodImg.fillAmount = 1f;
        gleamImg.fillAmount = 1f;
        gleamImg.color = new Color(1f, 0.35f, 0.35f, 0f);

        if (btnText != null)
        {
            btnText.fontStyle = FontStyles.Strikethrough;
            btnText.color = new Color(0.85f, 0.08f, 0.08f, 1f);
        }

        // Settle droplets
        float settleElapsed = 0f;
        while (settleElapsed < 0.45f)
        {
            settleElapsed += Time.deltaTime;
            for (int i = 0; i < dropCount; i++)
            {
                if (dropRects[i] != null)
                {
                    dropRects[i].anchoredPosition += dropVelocities[i] * Time.deltaTime;
                    dropVelocities[i] *= Mathf.Exp(-8f * Time.deltaTime);
                }
            }
            yield return null;
        }

        // Hold on the fully crossed-out bloody button while the eerie sound resonates
        float holdDuration = eerieClip != null ? Mathf.Clamp(eerieClip.length - 0.40f, 0.65f, 2.5f) : 0.75f;
        yield return new WaitForSeconds(holdDuration);
    }

    private static AudioClip CreateEerieConfirmClip()
    {
        int sampleRate = 44100;
        float duration = 1.8f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        System.Random rnd = new System.Random(666);

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float val = 0f;

            // 1. Whispering reverse breath tension swell (0.0s to 0.10s)
            if (t < 0.10f)
            {
                float st = t / 0.10f;
                float swellEnv = st * st * 0.25f;
                float white = (float)(rnd.NextDouble() * 2.0 - 1.0);
                float hiss = white * swellEnv;
                float whine = Mathf.Sin(2f * Mathf.PI * (500f + 700f * st) * t) * swellEnv * 0.35f;
                val += hiss + whine;
            }

            // 2. Visceral Strike at t = 0.10s
            if (t >= 0.10f)
            {
                float st = t - 0.10f;

                // A. Sub-bass dread heartbeat thud (48Hz down to 24Hz)
                float subFreq = 48f * Mathf.Exp(-st * 4f) + 24f;
                float subEnv = Mathf.Exp(-st * 3.5f) * 0.85f;
                float sub = Mathf.Sin(2f * Mathf.PI * subFreq * st) * subEnv;

                // B. Crisp razor flesh slice / high-bite impact
                float sliceEnv = Mathf.Exp(-st * 38f) * 0.90f;
                float noise = (float)(rnd.NextDouble() * 2.0 - 1.0) * sliceEnv;
                float blade = Mathf.Sin(2f * Mathf.PI * (1600f * Mathf.Exp(-st * 25f) + 120f) * st) * sliceEnv;

                // C. Visceral organic blood squelch
                float squelchEnv = Mathf.Exp(-st * 16f) * 0.60f;
                float fm = Mathf.Sin(2f * Mathf.PI * 60f * st) * 300f;
                float squelch = Mathf.Sin(2f * Mathf.PI * (420f + fm) * st) * squelchEnv;

                // D. Haunting Tritone Chime / Bowed Waterphone Resonance (220 Hz + 311.13 Hz Eb + metallic overtones)
                float bellEnv = Mathf.Exp(-st * 2.0f) * 0.50f;
                float detune = Mathf.Sin(2f * Mathf.PI * 2.5f * st) * 2.5f;
                float f1 = 220f + detune;
                float f2 = 311.13f - detune; // Tritone
                float f3 = 622.25f;          // Octave overtone
                float f4 = 1244.5f;          // High metallic harmonics
                float f5 = 2489f;

                float bell = (
                    Mathf.Sin(2f * Mathf.PI * f1 * st) * 0.45f +
                    Mathf.Sin(2f * Mathf.PI * f2 * st) * 0.40f +
                    Mathf.Sin(2f * Mathf.PI * f3 * st) * 0.22f +
                    Mathf.Sin(2f * Mathf.PI * f4 * st) * 0.14f * Mathf.Exp(-st * 3.0f) +
                    Mathf.Sin(2f * Mathf.PI * f5 * st) * 0.08f * Mathf.Exp(-st * 4.5f)
                ) * bellEnv;

                val += sub + noise + blade + squelch + bell;
            }

            samples[i] = Mathf.Clamp(val * 0.85f, -0.98f, 0.98f);
        }

        AudioClip clip = AudioClip.Create("EerieConfirmSFX", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
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
