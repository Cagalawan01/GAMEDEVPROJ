using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SchoolPrologueController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI chapterBadgeText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private GameObject dialoguePanel;

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
        "",
        "Narrator",
        "Narrator",
        "Narrator",
        "Protagonist",
        "Protagonist"
    };

    private int currentLineIndex = 0;
    private bool isTyping = false;
    private Coroutine typingCoroutine;

    private void Start()
    {
        if (chapterBadgeText != null)
        {
            chapterBadgeText.text = "PROLOGUE  \u2014  FIRST DAY OF SCHOOL";
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextClicked);
        }

        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.RemoveAllListeners();
            backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
        }

        // Auto-save prologue milestone into Save Archive Slot 1
        PlayerPrefs.SetInt("SaveSlot_0_Exists", 1);
        PlayerPrefs.SetString("SaveSlot_0_Title", "01. Under the Cherry Blossoms");
        PlayerPrefs.SetString("SaveSlot_0_Time", System.DateTime.Now.ToString("yyyy.MM.dd  HH:mm"));
        PlayerPrefs.Save();

        ShowLine(0);
    }

    private void Update()
    {
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
        if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            advance = true;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
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
            // End of prologue: option to loop or prompt
            if (dialogueText != null)
            {
                dialogueText.text = "<color=#99E8FF><i>To be continued in Chapter 2... Thank you for playing!</i></color>";
            }
        }
    }

    private void ShowLine(int index)
    {
        if (speakerNameText != null)
        {
            string speaker = speakerNames[index];
            speakerNameText.text = string.IsNullOrEmpty(speaker) ? "" : $"<b>{speaker}</b>";
            speakerNameText.gameObject.SetActive(!string.IsNullOrEmpty(speaker));
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

    public void OnBackToMenuClicked()
    {
        if (clickClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickClip, SettingsMenu.GlobalSoundVolume);
        }

        SceneTransitionManager.LoadScene("MenuScreen", 0.5f);
    }
}
