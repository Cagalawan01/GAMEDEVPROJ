using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SchoolPrologueController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject speakerPlateObject;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI chapterBadgeText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button settingsIconButton;
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private GameObject dialoguePanel;

    [Header("Popups & Menus")]
    [SerializeField] private SettingsMenu settingsMenu;
    [SerializeField] private SaveArchiveMenu saveArchiveMenu;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip textBeepClip;
    [SerializeField] private AudioClip clickClip;

    [Header("Typewriter Settings")]
    [SerializeField] private float typingSpeed = 0.035f;

    private readonly string[] scriptLines = new string[]
    {
        "The gentle morning breeze carries the sweet scent of spring cherry blossoms through the air...",
        "Today marks the grand entrance ceremony at Hanamizuki High School.",
        "Flurries of pink sakura petals swirl across the stone path leading up to the school gates.",
        "Whispers and cheerful laughter of high school girls in their fresh uniforms echo all around.",
        "Holding my school bag tight against my chest, I take a deep breath...",
        "\"A new school... and a brand new chapter of my life begins today!\""
    };

    private readonly string[] speakerNames = new string[]
    {
        "Narrator",
        "Narrator",
        "Narrator",
        "Narrator",
        "Protagonist",
        "Protagonist"
    };

    private int currentLineIndex = 0;
    private bool isTyping = false;
    private Coroutine typingCoroutine;

    public int CurrentLineIndex => currentLineIndex;
    public string CurrentSaveChapterName => "Under the Cherry Blossoms";

    private void Awake()
    {
        EnsureSceneSprites();
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

    private void EnsureSceneSprites()
    {
        Transform bgTf = transform.Find("CherryBlossomBackground");
        if (bgTf != null)
        {
            Image bgImg = bgTf.GetComponent<Image>();
            if (bgImg != null && bgImg.sprite == null)
            {
                bgImg.sprite = LoadResourceSprite("cherry_blossom_school");
                bgImg.color = Color.white;
            }
        }

        if (dialoguePanel != null)
        {
            Image boxImg = dialoguePanel.GetComponent<Image>();
            if (boxImg != null && boxImg.sprite == null)
            {
                Sprite s = LoadResourceSprite("vn_dialogue_box");
                if (s != null)
                {
                    boxImg.sprite = s;
                    boxImg.color = Color.white;
                }
            }
        }

        if (speakerPlateObject != null)
        {
            Image plateImg = speakerPlateObject.GetComponent<Image>();
            if (plateImg != null && plateImg.sprite == null)
            {
                Sprite s = LoadResourceSprite("vn_speaker_plate");
                if (s != null)
                {
                    plateImg.sprite = s;
                    plateImg.color = Color.white;
                }
            }
        }

        if (settingsIconButton != null)
        {
            Image iconImg = settingsIconButton.GetComponent<Image>();
            if (iconImg != null && iconImg.sprite == null)
            {
                Sprite s = LoadResourceSprite("settings_gear_icon");
                if (s != null)
                {
                    iconImg.sprite = s;
                    iconImg.color = Color.white;
                }
            }
        }
    }

    private void Start()
    {
        EnsureSceneSprites();

        if (chapterBadgeText != null)
        {
            chapterBadgeText.text = "PROLOGUE  \u2014  FIRST DAY OF SCHOOL";
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextClicked);
        }

        if (settingsIconButton != null)
        {
            settingsIconButton.onClick.RemoveAllListeners();
            settingsIconButton.onClick.AddListener(OnSettingsButtonClicked);
        }

        if (backToMenuButton != null && backToMenuButton != settingsIconButton)
        {
            backToMenuButton.onClick.RemoveAllListeners();
            backToMenuButton.onClick.AddListener(OnSettingsButtonClicked);
        }

        if (settingsMenu == null)
        {
            settingsMenu = Object.FindAnyObjectByType<SettingsMenu>(FindObjectsInactive.Include);
        }

        if (saveArchiveMenu == null)
        {
            saveArchiveMenu = Object.FindAnyObjectByType<SaveArchiveMenu>(FindObjectsInactive.Include);
        }

        if (PlayerPrefs.GetInt("IsLoadingFromSave", 0) == 1)
        {
            PlayerPrefs.SetInt("IsLoadingFromSave", 0);
            PlayerPrefs.Save();
            currentLineIndex = Mathf.Clamp(PlayerPrefs.GetInt("LoadSavedLineIndex", 0), 0, scriptLines.Length - 1);
        }
        else
        {
            currentLineIndex = 0;
        }

        ShowLine(currentLineIndex);
    }

    private bool IsPopupOpen()
    {
        if (saveArchiveMenu != null && saveArchiveMenu.IsOpen)
        {
            return true;
        }

        if (settingsMenu != null && settingsMenu.gameObject.activeInHierarchy)
        {
            CanvasGroup cg = settingsMenu.GetComponent<CanvasGroup>();
            if (cg == null || cg.alpha > 0.05f)
            {
                return true;
            }
        }

        return false;
    }

    private void Update()
    {
        if (IsPopupOpen())
        {
            return;
        }

        bool advance = false;
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame ||
                UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame)
            {
                advance = true;
            }
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            advance = true;
        }
#endif
        if (advance)
        {
            OnNextClicked();
        }
    }

    public void OnNextClicked()
    {
        if (IsPopupOpen())
        {
            return;
        }

        if (isTyping)
        {
            // Skip typing and show full text immediately
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            dialogueText.text = scriptLines[currentLineIndex];
            isTyping = false;
            return;
        }

        if (currentLineIndex < scriptLines.Length - 1)
        {
            currentLineIndex++;
            ShowLine(currentLineIndex);
        }
        else
        {
            if (dialogueText != null)
            {
                dialogueText.text = "<color=#99E8FF><i>To be continued in Chapter 2... Thank you for playing!</i></color>";
            }
        }
    }

    private void ShowLine(int index)
    {
        string speaker = (index >= 0 && index < speakerNames.Length) ? speakerNames[index] : "Narrator";
        bool hasSpeaker = !string.IsNullOrEmpty(speaker);

        if (speakerPlateObject != null)
        {
            speakerPlateObject.SetActive(hasSpeaker);
        }

        if (speakerNameText != null)
        {
            speakerNameText.text = hasSpeaker ? $"<b>{speaker}</b>" : "";
            speakerNameText.gameObject.SetActive(hasSpeaker);
        }

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeLineRoutine(scriptLines[index]));
    }

    private IEnumerator TypeLineRoutine(string line)
    {
        isTyping = true;
        dialogueText.text = "";

        for (int i = 0; i < line.Length; i++)
        {
            dialogueText.text += line[i];
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        typingCoroutine = null;
    }

    public void OnSettingsButtonClicked()
    {
        if (clickClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickClip, SettingsMenu.GlobalSoundVolume);
        }

        if (settingsMenu == null)
        {
            settingsMenu = Object.FindAnyObjectByType<SettingsMenu>(FindObjectsInactive.Include);
        }

        if (settingsMenu != null)
        {
            settingsMenu.ToggleSettings();
        }
    }

    public void OnBackToMenuClicked()
    {
        if (clickClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickClip, SettingsMenu.GlobalSoundVolume);
        }

        SceneTransitionManager.LoadScene("MenuScreen", 0.5f);
    }
}
