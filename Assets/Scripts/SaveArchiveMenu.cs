using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SaveArchiveMenu : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject archivePanel;
    [SerializeField] private GameObject mainMenuPanel;

    [Header("Transition Settings")]
    [SerializeField] private CanvasGroup archiveCanvasGroup;
    [SerializeField] private CanvasGroup mainMenuCanvasGroup;
    [SerializeField] private float transitionDuration = 0.28f;

    [Header("Header & Pagination")]
    [SerializeField] private TextMeshProUGUI archiveTitleText;
    [SerializeField] private TextMeshProUGUI pageSubtitleText;
    [SerializeField] private Button page1Button;
    [SerializeField] private Button page2Button;
    [SerializeField] private TextMeshProUGUI page1Text;
    [SerializeField] private TextMeshProUGUI page2Text;
    [SerializeField] private Button prevPageButton;
    [SerializeField] private Button nextPageButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button loadMemoryTabButton;
    [SerializeField] private bool isSaveMode = false;

    public bool IsOpen => archivePanel != null && archivePanel.activeInHierarchy && (archiveCanvasGroup == null || archiveCanvasGroup.alpha > 0.1f);

    [Header("Save Cards (6 per page)")]
    [SerializeField] private Sprite cardFrameSprite;
    [SerializeField] private Button[] cardButtons = new Button[6];
    [SerializeField] private Image[] cardImages = new Image[6];
    [SerializeField] private TextMeshProUGUI[] cardTitleTexts = new TextMeshProUGUI[6];
    [SerializeField] private TextMeshProUGUI[] cardTimeTexts = new TextMeshProUGUI[6];

    [Header("Page 1 Sprites & Metadata")]
    [SerializeField] private Sprite[] page1Sprites = new Sprite[6];
    [SerializeField] private string[] page1Titles = new string[6]
    {
        "01. The First Encounter",
        "02. A Fading Promise",
        "03. The Silent Hall",
        "04. An Unspoken Truth",
        "05. Where It Began",
        "06. The Forgotten Night"
    };
    [SerializeField] private string[] page1Times = new string[6]
    {
        "2026.10.15 21:44",
        "2026.10.16 17:33",
        "2026.10.17 15:44",
        "2026.10.18 22:06",
        "2026.10.19 09:35",
        "2026.10.20 11:08"
    };

    [Header("Page 2 Sprites & Metadata")]
    [SerializeField] private Sprite[] page2Sprites = new Sprite[6];
    [SerializeField] private string[] page2Titles = new string[6]
    {
        "07. Echoes of the Past",
        "08. Shattered Reflection",
        "09. Whispering Shadows",
        "10. Sunken Cathedral",
        "11. Bonds of Starlight",
        "12. Beyond the Horizon"
    };
    [SerializeField] private string[] page2Times = new string[6]
    {
        "2026.10.21 14:15",
        "2026.10.22 18:50",
        "2026.10.23 20:30",
        "2026.10.24 10:22",
        "2026.10.25 16:45",
        "2026.10.26 23:10"
    };

    [Header("Audio & Feedback")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private TextMeshProUGUI notificationText;

    private int currentPage = 1; // 1 or 2
    private bool isInitialized = false;
    private Coroutine notificationCoroutine;
    private Coroutine transitionCoroutine;

    // Per-card delete buttons and loaded save thumbnails (created at runtime)
    private Button[] deleteButtons = new Button[6];
    private Texture2D[] cardThumbTextures = new Texture2D[6];
    private Sprite[] cardThumbSprites = new Sprite[6];

    // Delete confirmation dialog (created at runtime)
    private GameObject deleteConfirmRoot;
    private TextMeshProUGUI deleteConfirmMessage;
    private int pendingDeleteSlot = -1;
    public bool IsDeleteConfirmOpen => deleteConfirmRoot != null && deleteConfirmRoot.activeSelf;

    private readonly Color activePageColor = new Color(0.25f, 0.88f, 1f, 1f);
    private readonly Color inactivePageColor = new Color(0.6f, 0.72f, 0.88f, 0.65f);

    public const string PREF_SELECTED_SLOT = "Continue_SaveSlot";
    public const string PREF_CHAPTER_TITLE = "Continue_ChapterTitle";
    public const string PREF_TIMESTAMP = "Continue_Timestamp";

    private void Awake()
    {
        InitIfNeeded();
    }

    private void Start()
    {
        SetPage(1);
    }

    private void Update()
    {
        if (archivePanel != null && archivePanel.activeInHierarchy && archiveCanvasGroup != null && archiveCanvasGroup.alpha > 0.5f)
        {
            bool escapePressed = false;
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                escapePressed = true;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                escapePressed = true;
            }
#endif
            if (escapePressed)
            {
                if (IsDeleteConfirmOpen)
                {
                    HideDeleteConfirm();
                }
                else
                {
                    CloseArchive();
                }
            }
        }
    }

    private void InitIfNeeded()
    {
        if (isInitialized) return;
        isInitialized = true;

        foreach (Text t in GetComponentsInChildren<Text>(true))
        {
            if (t != null && (t.text == "Button" || t.gameObject.name.Contains("Text (Legacy)") || t.gameObject.name == "Text"))
            {
                Destroy(t.gameObject);
            }
        }

        if (cardFrameSprite == null)
        {
            cardFrameSprite = Resources.Load<Sprite>("ornate_card_frame");
        }

        InitializeButtons();
        EnsureCanvasGroups();
        BuildDeleteButtons();
        BuildDeleteConfirmDialog();
    }

    private void OnDestroy()
    {
        for (int i = 0; i < cardThumbTextures.Length; i++)
        {
            ReleaseCardThumb(i);
        }
    }

    private void EnsureCanvasGroups()
    {
        if (archiveCanvasGroup == null)
        {
            GameObject target = archivePanel != null ? archivePanel : gameObject;
            archiveCanvasGroup = target.GetComponent<CanvasGroup>();
            if (archiveCanvasGroup == null) archiveCanvasGroup = target.AddComponent<CanvasGroup>();
        }

        if (mainMenuCanvasGroup == null && mainMenuPanel != null)
        {
            mainMenuCanvasGroup = mainMenuPanel.GetComponent<CanvasGroup>();
            if (mainMenuCanvasGroup == null) mainMenuCanvasGroup = mainMenuPanel.AddComponent<CanvasGroup>();
        }
    }

    private void InitializeButtons()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(CloseArchive);
        }

        if (page1Button != null)
        {
            page1Button.onClick.RemoveAllListeners();
            page1Button.onClick.AddListener(() => SetPage(1));
        }

        if (page2Button != null)
        {
            page2Button.onClick.RemoveAllListeners();
            page2Button.onClick.AddListener(() => SetPage(2));
        }

        if (prevPageButton != null)
        {
            prevPageButton.onClick.RemoveAllListeners();
            prevPageButton.onClick.AddListener(PrevPage);
        }

        if (nextPageButton != null)
        {
            nextPageButton.onClick.RemoveAllListeners();
            nextPageButton.onClick.AddListener(NextPage);
        }

        for (int i = 0; i < cardButtons.Length; i++)
        {
            int slotIdx = i;
            if (cardButtons[slotIdx] != null)
            {
                cardButtons[slotIdx].onClick.RemoveAllListeners();
                cardButtons[slotIdx].onClick.AddListener(() => OnCardClicked(slotIdx));
            }
        }
    }

    public void OpenArchiveInSaveMode()
    {
        isSaveMode = true;
        OpenArchiveInternal();
    }

    public void OpenArchive()
    {
        isSaveMode = false;
        OpenArchiveInternal();
    }

    private void OpenArchiveInternal()
    {
        if (archivePanel != null)
        {
            archivePanel.SetActive(true);
        }
        else
        {
            gameObject.SetActive(true);
        }

        InitIfNeeded();

        if (archiveTitleText != null)
        {
            archiveTitleText.text = isSaveMode ? "\u2726  Save Memory  \u2726" : "\u2726  Load Memory  \u2726";
        }

        if (archiveCanvasGroup != null)
        {
            archiveCanvasGroup.alpha = 0f;
            archiveCanvasGroup.interactable = false;
            archiveCanvasGroup.blocksRaycasts = false;
        }

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(TransitionToArchive());
    }

    public void CloseArchive()
    {
        PlaySound(clickSound);
        InitIfNeeded();

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(TransitionToMainMenu());
    }

    private IEnumerator TransitionToArchive()
    {
        if (archivePanel != null)
        {
            archivePanel.transform.localScale = new Vector3(0.97f, 0.97f, 1f);
        }

        if (mainMenuCanvasGroup != null)
        {
            mainMenuCanvasGroup.interactable = false;
            mainMenuCanvasGroup.blocksRaycasts = false;
        }

        SetPage(1);

        float elapsed = 0f;
        float startMenuAlpha = (mainMenuCanvasGroup != null) ? mainMenuCanvasGroup.alpha : 1f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float smoothT = t * t * (3f - 2f * t);

            if (mainMenuCanvasGroup != null)
            {
                mainMenuCanvasGroup.alpha = Mathf.Lerp(startMenuAlpha, 0f, smoothT);
            }

            if (archiveCanvasGroup != null)
            {
                archiveCanvasGroup.alpha = Mathf.Lerp(0f, 1f, smoothT);
            }

            if (archivePanel != null)
            {
                archivePanel.transform.localScale = Vector3.Lerp(new Vector3(0.97f, 0.97f, 1f), Vector3.one, smoothT);
            }

            yield return null;
        }

        if (mainMenuCanvasGroup != null)
        {
            mainMenuCanvasGroup.alpha = 0f;
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }

        if (archiveCanvasGroup != null)
        {
            archiveCanvasGroup.alpha = 1f;
            archiveCanvasGroup.interactable = true;
            archiveCanvasGroup.blocksRaycasts = true;
        }

        if (archivePanel != null)
        {
            archivePanel.transform.localScale = Vector3.one;
        }

        transitionCoroutine = null;
    }

    private IEnumerator TransitionToMainMenu()
    {
        if (archiveCanvasGroup != null)
        {
            archiveCanvasGroup.interactable = false;
            archiveCanvasGroup.blocksRaycasts = false;
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
            if (mainMenuCanvasGroup != null)
            {
                mainMenuCanvasGroup.alpha = 0f;
                mainMenuCanvasGroup.interactable = false;
                mainMenuCanvasGroup.blocksRaycasts = false;
            }
        }

        float elapsed = 0f;
        float startArchiveAlpha = (archiveCanvasGroup != null) ? archiveCanvasGroup.alpha : 1f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float smoothT = t * t * (3f - 2f * t);

            if (archiveCanvasGroup != null)
            {
                archiveCanvasGroup.alpha = Mathf.Lerp(startArchiveAlpha, 0f, smoothT);
            }

            if (archivePanel != null)
            {
                archivePanel.transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(0.97f, 0.97f, 1f), smoothT);
            }

            if (mainMenuCanvasGroup != null)
            {
                mainMenuCanvasGroup.alpha = Mathf.Lerp(0f, 1f, smoothT);
            }

            yield return null;
        }

        if (archiveCanvasGroup != null)
        {
            archiveCanvasGroup.alpha = 0f;
            archiveCanvasGroup.interactable = false;
            archiveCanvasGroup.blocksRaycasts = false;
        }

        if (mainMenuCanvasGroup != null)
        {
            mainMenuCanvasGroup.alpha = 1f;
            mainMenuCanvasGroup.interactable = true;
            mainMenuCanvasGroup.blocksRaycasts = true;
        }

        if (archivePanel != null)
        {
            archivePanel.SetActive(false);
            archivePanel.transform.localScale = Vector3.one;
        }

        transitionCoroutine = null;
    }

    public static bool HasSaveData(int globalSlot)
    {
        return PlayerPrefs.GetInt($"SaveSlot_{globalSlot}_Exists", 0) == 1
            || PlayerPrefs.HasKey($"SaveSlot_{globalSlot}_Title");
    }

    public void SetPage(int page)
    {
        currentPage = Mathf.Clamp(page, 1, 2);

        // Update Subtitle: "MEMORY ARCHIVE  —  PAGE 01" / "PAGE 02"
        if (pageSubtitleText != null)
        {
            pageSubtitleText.text = $"MEMORY ARCHIVE  \u2014  PAGE 0{currentPage}";
        }

        // Update page numbers styling
        if (page1Text != null)
        {
            page1Text.color = (currentPage == 1) ? activePageColor : inactivePageColor;
            page1Text.fontStyle = (currentPage == 1) ? FontStyles.Bold : FontStyles.Normal;
        }

        if (page2Text != null)
        {
            page2Text.color = (currentPage == 2) ? activePageColor : inactivePageColor;
            page2Text.fontStyle = (currentPage == 2) ? FontStyles.Bold : FontStyles.Normal;
        }

        // Update arrow interactability
        if (prevPageButton != null)
        {
            prevPageButton.interactable = (currentPage > 1);
        }

        if (nextPageButton != null)
        {
            nextPageButton.interactable = (currentPage < 2);
        }

        Sprite[] activeSprites = (currentPage == 1) ? page1Sprites : page2Sprites;
        string[] activeTitles = (currentPage == 1) ? page1Titles : page2Titles;
        string[] activeTimes = (currentPage == 1) ? page1Times : page2Times;

        if (cardFrameSprite != null)
        {
            for (int b = 0; b < cardButtons.Length; b++)
            {
                if (cardButtons[b] != null)
                {
                    Image bg = cardButtons[b].GetComponent<Image>();
                    if (bg != null && bg.sprite != cardFrameSprite)
                    {
                        bg.sprite = cardFrameSprite;
                        bg.type = Image.Type.Simple;
                        bg.preserveAspect = false;
                        bg.color = Color.white;
                    }
                }
            }
        }

        for (int i = 0; i < 6; i++)
        {
            int globalSlot = (currentPage - 1) * 6 + i;
            bool hasSave = HasSaveData(globalSlot);

            if (hasSave)
            {
                // Has save progress: Show illustration (captured screen or default), chapter title, and timestamp
                if (cardImages != null && i < cardImages.Length && cardImages[i] != null)
                {
                    cardImages[i].gameObject.SetActive(true);
                    cardImages[i].color = Color.white;
                    cardImages[i].preserveAspect = false;

                    ReleaseCardThumb(i);
                    Texture2D thumb = SaveThumbnail.LoadForSlot(globalSlot);
                    if (thumb != null)
                    {
                        cardThumbTextures[i] = thumb;
                        cardThumbSprites[i] = CreateCroppedSprite(thumb);
                        cardImages[i].sprite = cardThumbSprites[i];
                    }
                    else if (activeSprites != null && i < activeSprites.Length && activeSprites[i] != null)
                    {
                        cardImages[i].sprite = activeSprites[i];
                    }
                }

                if (deleteButtons != null && i < deleteButtons.Length && deleteButtons[i] != null)
                {
                    deleteButtons[i].gameObject.SetActive(true);
                    deleteButtons[i].transform.SetAsLastSibling();
                }

                if (cardTitleTexts != null && i < cardTitleTexts.Length && cardTitleTexts[i] != null)
                {
                    string title = PlayerPrefs.GetString($"SaveSlot_{globalSlot}_Title",
                        (activeTitles != null && i < activeTitles.Length) ? activeTitles[i] : $"Chapter {globalSlot + 1}");
                    cardTitleTexts[i].text = title;
                    cardTitleTexts[i].color = new Color(0.88f, 0.95f, 1f, 1f);
                    cardTitleTexts[i].fontSize = 28;

                    RectTransform ctRect = cardTitleTexts[i].GetComponent<RectTransform>();
                    ctRect.anchorMin = new Vector2(0.04f, 0.12f);
                    ctRect.anchorMax = new Vector2(0.96f, 0.27f);
                }

                if (cardTimeTexts != null && i < cardTimeTexts.Length && cardTimeTexts[i] != null)
                {
                    cardTimeTexts[i].gameObject.SetActive(true);
                    string time = PlayerPrefs.GetString($"SaveSlot_{globalSlot}_Time",
                        (activeTimes != null && i < activeTimes.Length) ? activeTimes[i] : "");
                    cardTimeTexts[i].text = time;

                    // Position timestamp on bottom-left to leave bottom-right for the inside Delete button
                    RectTransform ctimeRect = cardTimeTexts[i].GetComponent<RectTransform>();
                    if (ctimeRect != null)
                    {
                        ctimeRect.anchorMin = new Vector2(0.05f, 0.01f);
                        ctimeRect.anchorMax = new Vector2(0.68f, 0.12f);
                        cardTimeTexts[i].alignment = TextAlignmentOptions.MidlineLeft;
                        cardTimeTexts[i].fontSize = 17;
                    }
                }
            }
            else
            {
                // NO save progress: NO image, "Empty Slot" in center, no timestamp, hide delete button
                if (cardImages != null && i < cardImages.Length && cardImages[i] != null)
                {
                    ReleaseCardThumb(i);
                    cardImages[i].gameObject.SetActive(false);
                }

                if (deleteButtons != null && i < deleteButtons.Length && deleteButtons[i] != null)
                {
                    deleteButtons[i].gameObject.SetActive(false);
                }

                if (cardTitleTexts != null && i < cardTitleTexts.Length && cardTitleTexts[i] != null)
                {
                    cardTitleTexts[i].text = "Empty Slot";
                    cardTitleTexts[i].color = new Color(0.55f, 0.70f, 0.88f, 0.65f);
                    cardTitleTexts[i].fontSize = 36;
                    cardTitleTexts[i].alignment = TextAlignmentOptions.Center;

                    RectTransform ctRect = cardTitleTexts[i].GetComponent<RectTransform>();
                    ctRect.anchorMin = new Vector2(0.05f, 0.35f);
                    ctRect.anchorMax = new Vector2(0.95f, 0.65f);
                }

                if (cardTimeTexts != null && i < cardTimeTexts.Length && cardTimeTexts[i] != null)
                {
                    cardTimeTexts[i].gameObject.SetActive(false);
                    cardTimeTexts[i].text = "";
                }
            }
        }
    }

    public void NextPage()
    {
        if (currentPage < 2)
        {
            PlaySound(clickSound);
            SetPage(currentPage + 1);
        }
    }

    public void PrevPage()
    {
        if (currentPage > 1)
        {
            PlaySound(clickSound);
            SetPage(currentPage - 1);
        }
    }

    public void OnCardClicked(int slotIndex)
    {
        int globalSlot = (currentPage - 1) * 6 + slotIndex;

        if (isSaveMode)
        {
            SchoolPrologueController prologue = Object.FindAnyObjectByType<SchoolPrologueController>();
            int lineIdx = prologue != null ? prologue.CurrentLineIndex : 0;
            string chapterName = prologue != null ? prologue.CurrentSaveChapterName : "First Day of School";
            string slotPrefix = (globalSlot + 1).ToString("D2");
            string saveTitle = $"{slotPrefix}. {chapterName}";
            string timestamp = System.DateTime.Now.ToString("yyyy.MM.dd  HH:mm");
            string activeScene = SceneManager.GetActiveScene().name;

            string playerName = PlayerPrefs.GetString(OpeningSceneController.PLAYER_NAME_PREF_KEY, "Player");

            // Persist the captured game screen as this slot's thumbnail
            SaveThumbnail.WritePendingToSlot(globalSlot);

            PlayerPrefs.SetInt($"SaveSlot_{globalSlot}_Exists", 1);
            PlayerPrefs.SetString($"SaveSlot_{globalSlot}_Title", saveTitle);
            PlayerPrefs.SetString($"SaveSlot_{globalSlot}_Time", timestamp);
            PlayerPrefs.SetString($"SaveSlot_{globalSlot}_Scene", activeScene);
            PlayerPrefs.SetInt($"SaveSlot_{globalSlot}_LineIndex", lineIdx);
            PlayerPrefs.SetString($"SaveSlot_{globalSlot}_PlayerName", playerName);

            PlayerPrefs.SetInt(PREF_SELECTED_SLOT, globalSlot);
            PlayerPrefs.SetString(PREF_CHAPTER_TITLE, saveTitle);
            PlayerPrefs.SetString(PREF_TIMESTAMP, timestamp);
            PlayerPrefs.Save();

            SetPage(currentPage);
            PlaySound(clickSound);
            ShowNotification($"Saved Progress to Slot {globalSlot + 1} \u2014 {saveTitle}");
            Debug.Log($"<color=cyan>[SaveArchive] Saved progress to Slot {globalSlot + 1}: {saveTitle} ({timestamp})</color>");
            return;
        }

        bool hasSave = HasSaveData(globalSlot);

        if (hasSave)
        {
            string title = PlayerPrefs.GetString($"SaveSlot_{globalSlot}_Title",
                (currentPage == 1 && slotIndex < page1Titles.Length) ? page1Titles[slotIndex] : $"Chapter {globalSlot + 1}");
            string time = PlayerPrefs.GetString($"SaveSlot_{globalSlot}_Time",
                (currentPage == 1 && slotIndex < page1Times.Length) ? page1Times[slotIndex] : "");
            string targetScene = PlayerPrefs.GetString($"SaveSlot_{globalSlot}_Scene", "SchoolScene");
            int savedLineIdx = PlayerPrefs.GetInt($"SaveSlot_{globalSlot}_LineIndex", 0);
            string savedPlayerName = PlayerPrefs.GetString($"SaveSlot_{globalSlot}_PlayerName",
                PlayerPrefs.GetString(OpeningSceneController.PLAYER_NAME_PREF_KEY, "Player"));

            // Save selected progress to PlayerPrefs
            PlayerPrefs.SetInt(PREF_SELECTED_SLOT, globalSlot);
            PlayerPrefs.SetString(PREF_CHAPTER_TITLE, title);
            PlayerPrefs.SetString(PREF_TIMESTAMP, time);
            PlayerPrefs.SetString(OpeningSceneController.PLAYER_NAME_PREF_KEY, savedPlayerName);
            PlayerPrefs.SetInt("IsLoadingFromSave", 1);
            PlayerPrefs.SetInt("LoadSavedLineIndex", savedLineIdx);
            PlayerPrefs.Save();

            Debug.Log($"<color=cyan>[SaveArchive] Continuing story from Slot {globalSlot + 1}: {title} ({time})</color>");

            PlaySound(clickSound);
            ShowNotification($"Continuing Story \u2014 {title}");

            if (SceneManager.GetActiveScene().name != targetScene)
            {
                SceneTransitionManager.LoadScene(targetScene, 0.6f);
            }
        }
        else
        {
            PlaySound(clickSound);
            ShowNotification($"Slot {globalSlot + 1} is empty \u2014 No save progress");
        }
    }

    public void SaveProgressToSlot(int globalSlot, string chapterTitle, string timestamp)
    {
        SaveThumbnail.WritePendingToSlot(globalSlot);
        PlayerPrefs.SetInt($"SaveSlot_{globalSlot}_Exists", 1);
        PlayerPrefs.SetString($"SaveSlot_{globalSlot}_Title", chapterTitle);
        PlayerPrefs.SetString($"SaveSlot_{globalSlot}_Time", timestamp);
        PlayerPrefs.SetInt(PREF_SELECTED_SLOT, globalSlot);
        PlayerPrefs.Save();

        SetPage(currentPage);
    }

    public void ClearSaveSlot(int globalSlot)
    {
        PlayerPrefs.DeleteKey($"SaveSlot_{globalSlot}_Exists");
        PlayerPrefs.DeleteKey($"SaveSlot_{globalSlot}_Title");
        PlayerPrefs.DeleteKey($"SaveSlot_{globalSlot}_Time");
        PlayerPrefs.DeleteKey($"SaveSlot_{globalSlot}_Scene");
        PlayerPrefs.DeleteKey($"SaveSlot_{globalSlot}_LineIndex");
        PlayerPrefs.DeleteKey($"SaveSlot_{globalSlot}_PlayerName");

        SaveThumbnail.DeleteForSlot(globalSlot);

        if (PlayerPrefs.GetInt(PREF_SELECTED_SLOT, -1) == globalSlot)
        {
            PlayerPrefs.DeleteKey(PREF_SELECTED_SLOT);
            PlayerPrefs.DeleteKey(PREF_CHAPTER_TITLE);
            PlayerPrefs.DeleteKey(PREF_TIMESTAMP);
        }

        PlayerPrefs.Save();
        SetPage(currentPage);
    }

    private void ShowNotification(string message)
    {
        if (notificationText != null)
        {
            if (notificationCoroutine != null)
            {
                StopCoroutine(notificationCoroutine);
            }
            notificationCoroutine = StartCoroutine(AnimateNotification(message));
        }
    }

    private IEnumerator AnimateNotification(string message)
    {
        notificationText.text = message;
        notificationText.gameObject.SetActive(true);
        notificationText.alpha = 1f;

        yield return new WaitForSeconds(2.5f);

        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            notificationText.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.5f);
            yield return null;
        }

        notificationText.gameObject.SetActive(false);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null && audioSource.isActiveAndEnabled)
        {
            float sfxVol = PlayerPrefs.GetFloat("SFX_Volume", 0.8f);
            audioSource.PlayOneShot(clip, sfxVol);
        }
    }

    private void ReleaseCardThumb(int cardIndex)
    {
        if (cardIndex < 0 || cardIndex >= cardThumbTextures.Length) return;
        if (cardThumbSprites[cardIndex] != null)
        {
            Destroy(cardThumbSprites[cardIndex]);
            cardThumbSprites[cardIndex] = null;
        }
        if (cardThumbTextures[cardIndex] != null)
        {
            Destroy(cardThumbTextures[cardIndex]);
            cardThumbTextures[cardIndex] = null;
        }
    }

    private Sprite CreateCroppedSprite(Texture2D tex)
    {
        if (tex == null) return null;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private void BuildDeleteButtons()
    {
        TMP_FontAsset font = pageSubtitleText != null ? pageSubtitleText.font : null;

        for (int i = 0; i < cardButtons.Length; i++)
        {
            if (cardButtons[i] == null) continue;

            Transform existing = cardButtons[i].transform.Find("DeleteButton");
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }

            GameObject delObj = new GameObject("DeleteButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
            delObj.transform.SetParent(cardButtons[i].transform, false);

            RectTransform delRect = delObj.GetComponent<RectTransform>();
            delRect.anchorMin = new Vector2(1f, 0f);
            delRect.anchorMax = new Vector2(1f, 0f);
            delRect.pivot = new Vector2(1f, 0f);
            delRect.anchoredPosition = new Vector2(-16f, 10f);
            delRect.sizeDelta = new Vector2(84f, 30f);

            Image delImg = delObj.GetComponent<Image>();
            delImg.color = new Color(0.48f, 0.10f, 0.12f, 0.95f);

            Outline outline = delObj.GetComponent<Outline>();
            outline.effectColor = new Color(1f, 0.40f, 0.40f, 0.90f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(delObj.transform, false);

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = "Delete";
            if (font != null) tmp.font = font;
            tmp.fontSize = 16;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            Button btn = delObj.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.3f, 1.15f, 1.15f, 1f);
            cb.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            btn.colors = cb;

            int slotIdx = i;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnDeleteCardClicked(slotIdx));

            deleteButtons[i] = btn;
            delObj.SetActive(false);
        }
    }

    private void OnDeleteCardClicked(int slotIndex)
    {
        PlaySound(clickSound);
        int globalSlot = (currentPage - 1) * 6 + slotIndex;
        if (!HasSaveData(globalSlot)) return;
        ShowDeleteConfirm(globalSlot);
    }

    private void BuildDeleteConfirmDialog()
    {
        if (deleteConfirmRoot != null) return;

        TMP_FontAsset titleFont = archiveTitleText != null ? archiveTitleText.font : null;
        TMP_FontAsset bodyFont = pageSubtitleText != null ? pageSubtitleText.font : titleFont;

        GameObject rootTarget = archivePanel != null ? archivePanel : gameObject;

        deleteConfirmRoot = new GameObject("DeleteConfirmModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        deleteConfirmRoot.transform.SetParent(rootTarget.transform, false);
        deleteConfirmRoot.transform.SetAsLastSibling();

        RectTransform rootRect = deleteConfirmRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image rootImg = deleteConfirmRoot.GetComponent<Image>();
        rootImg.color = new Color(0f, 0f, 0f, 0.75f);
        rootImg.raycastTarget = true;

        GameObject boxObj = new GameObject("DialogBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        boxObj.transform.SetParent(deleteConfirmRoot.transform, false);

        RectTransform boxRect = boxObj.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(600f, 300f);

        Image boxImg = boxObj.GetComponent<Image>();
        boxImg.color = new Color(0.04f, 0.07f, 0.14f, 0.98f);

        Outline boxOutline = boxObj.GetComponent<Outline>();
        boxOutline.effectColor = new Color(0.25f, 0.75f, 1f, 0.85f);
        boxOutline.effectDistance = new Vector2(2f, -2f);

        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(boxObj.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.05f, 0.72f);
        titleRect.anchorMax = new Vector2(0.95f, 0.95f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "\u2726  Delete Saved Memory?  \u2726";
        if (titleFont != null) titleTmp.font = titleFont;
        titleTmp.fontSize = 28;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(1f, 0.45f, 0.45f, 1f);

        GameObject msgObj = new GameObject("Message", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        msgObj.transform.SetParent(boxObj.transform, false);
        RectTransform msgRect = msgObj.GetComponent<RectTransform>();
        msgRect.anchorMin = new Vector2(0.06f, 0.32f);
        msgRect.anchorMax = new Vector2(0.94f, 0.72f);
        msgRect.offsetMin = Vector2.zero;
        msgRect.offsetMax = Vector2.zero;

        deleteConfirmMessage = msgObj.GetComponent<TextMeshProUGUI>();
        deleteConfirmMessage.text = "Are you sure you want to delete this save?";
        if (bodyFont != null) deleteConfirmMessage.font = bodyFont;
        deleteConfirmMessage.fontSize = 20;
        deleteConfirmMessage.alignment = TextAlignmentOptions.Center;
        deleteConfirmMessage.color = new Color(0.85f, 0.92f, 1f, 0.95f);

        GameObject cancelBtnObj = new GameObject("CancelButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
        cancelBtnObj.transform.SetParent(boxObj.transform, false);
        RectTransform cancelRect = cancelBtnObj.GetComponent<RectTransform>();
        cancelRect.anchorMin = new Vector2(0.12f, 0.10f);
        cancelRect.anchorMax = new Vector2(0.45f, 0.28f);
        cancelRect.offsetMin = Vector2.zero;
        cancelRect.offsetMax = Vector2.zero;

        Image cancelImg = cancelBtnObj.GetComponent<Image>();
        cancelImg.color = new Color(0.12f, 0.16f, 0.24f, 1f);

        Outline cancelOutline = cancelBtnObj.GetComponent<Outline>();
        cancelOutline.effectColor = new Color(0.45f, 0.65f, 0.85f, 0.8f);
        cancelOutline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject cancelTextObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        cancelTextObj.transform.SetParent(cancelBtnObj.transform, false);
        RectTransform ctRect = cancelTextObj.GetComponent<RectTransform>();
        ctRect.anchorMin = Vector2.zero;
        ctRect.anchorMax = Vector2.one;
        ctRect.offsetMin = Vector2.zero;
        ctRect.offsetMax = Vector2.zero;

        TextMeshProUGUI cancelTmp = cancelTextObj.GetComponent<TextMeshProUGUI>();
        cancelTmp.text = "Cancel";
        if (bodyFont != null) cancelTmp.font = bodyFont;
        cancelTmp.fontSize = 20;
        cancelTmp.alignment = TextAlignmentOptions.Center;
        cancelTmp.color = Color.white;

        Button cancelBtn = cancelBtnObj.GetComponent<Button>();
        cancelBtn.onClick.AddListener(HideDeleteConfirm);

        GameObject delBtnObj = new GameObject("ConfirmDeleteButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
        delBtnObj.transform.SetParent(boxObj.transform, false);
        RectTransform delRect = delBtnObj.GetComponent<RectTransform>();
        delRect.anchorMin = new Vector2(0.55f, 0.10f);
        delRect.anchorMax = new Vector2(0.88f, 0.28f);
        delRect.offsetMin = Vector2.zero;
        delRect.offsetMax = Vector2.zero;

        Image delImg = delBtnObj.GetComponent<Image>();
        delImg.color = new Color(0.58f, 0.10f, 0.10f, 1f);

        Outline delOutline = delBtnObj.GetComponent<Outline>();
        delOutline.effectColor = new Color(1f, 0.35f, 0.35f, 0.85f);
        delOutline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject delTextObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        delTextObj.transform.SetParent(delBtnObj.transform, false);
        RectTransform dtRect = delTextObj.GetComponent<RectTransform>();
        dtRect.anchorMin = Vector2.zero;
        dtRect.anchorMax = Vector2.one;
        dtRect.offsetMin = Vector2.zero;
        dtRect.offsetMax = Vector2.zero;

        TextMeshProUGUI delTmp = delTextObj.GetComponent<TextMeshProUGUI>();
        delTmp.text = "Delete";
        if (bodyFont != null) delTmp.font = bodyFont;
        delTmp.fontSize = 20;
        delTmp.alignment = TextAlignmentOptions.Center;
        delTmp.color = Color.white;

        Button confirmBtn = delBtnObj.GetComponent<Button>();
        confirmBtn.onClick.AddListener(ConfirmDelete);

        deleteConfirmRoot.SetActive(false);
    }

    private void ShowDeleteConfirm(int globalSlot)
    {
        pendingDeleteSlot = globalSlot;
        if (deleteConfirmMessage != null)
        {
            string title = PlayerPrefs.GetString($"SaveSlot_{globalSlot}_Title", $"Slot {globalSlot + 1}");
            deleteConfirmMessage.text = $"Delete saved progress in Slot {globalSlot + 1}?\n<color=#88c0ff>{title}</color>\n\nThis box will become empty.";
        }
        if (deleteConfirmRoot != null)
        {
            deleteConfirmRoot.SetActive(true);
            deleteConfirmRoot.transform.SetAsLastSibling();
        }
    }

    public void HideDeleteConfirm()
    {
        PlaySound(clickSound);
        pendingDeleteSlot = -1;
        if (deleteConfirmRoot != null)
        {
            deleteConfirmRoot.SetActive(false);
        }
    }

    private void ConfirmDelete()
    {
        if (pendingDeleteSlot >= 0)
        {
            int slot = pendingDeleteSlot;
            pendingDeleteSlot = -1;
            if (deleteConfirmRoot != null) deleteConfirmRoot.SetActive(false);

            ClearSaveSlot(slot);
            PlaySound(clickSound);
            ShowNotification($"Deleted save in Slot {slot + 1} \u2014 Box is now empty");
        }
    }
}