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
                CloseArchive();
            }
        }
    }

    private void InitIfNeeded()
    {
        if (isInitialized) return;
        isInitialized = true;

        if (cardFrameSprite == null)
        {
            cardFrameSprite = Resources.Load<Sprite>("ornate_card_frame");
        }

        InitializeButtons();
        EnsureCanvasGroups();
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
                // Has save progress: Show illustration, chapter title, and timestamp
                if (cardImages != null && i < cardImages.Length && cardImages[i] != null)
                {
                    cardImages[i].gameObject.SetActive(true);
                    if (activeSprites != null && i < activeSprites.Length && activeSprites[i] != null)
                    {
                        cardImages[i].sprite = activeSprites[i];
                    }
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
                }
            }
            else
            {
                // NO save progress: NO image, "Empty Slot" in center, no timestamp
                if (cardImages != null && i < cardImages.Length && cardImages[i] != null)
                {
                    cardImages[i].gameObject.SetActive(false);
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

            PlayerPrefs.SetInt($"SaveSlot_{globalSlot}_Exists", 1);
            PlayerPrefs.SetString($"SaveSlot_{globalSlot}_Title", saveTitle);
            PlayerPrefs.SetString($"SaveSlot_{globalSlot}_Time", timestamp);
            PlayerPrefs.SetString($"SaveSlot_{globalSlot}_Scene", activeScene);
            PlayerPrefs.SetInt($"SaveSlot_{globalSlot}_LineIndex", lineIdx);

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

            // Save selected progress to PlayerPrefs
            PlayerPrefs.SetInt(PREF_SELECTED_SLOT, globalSlot);
            PlayerPrefs.SetString(PREF_CHAPTER_TITLE, title);
            PlayerPrefs.SetString(PREF_TIMESTAMP, time);
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
}
