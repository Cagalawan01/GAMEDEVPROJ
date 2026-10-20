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

    [Header("Interactive Items")]
    [SerializeField] private Button memorialPosterButton;
    [SerializeField] private Button noticeBoardButton;
    [SerializeField] private Button tarpLanternsButton;
    [SerializeField] private CanvasGroup memorialInspectCanvasGroup;
    [SerializeField] private Button closeInspectButton;

    [Header("Audio")]
    [SerializeField] private AudioSource schoolBgmSource;
    [SerializeField] private AudioClip schoolBgmClip;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip textBeepClip;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip hoverClip;

    [Header("Typewriter Settings")]
    [SerializeField] private float typingSpeed = 0.035f;

    private readonly string[] scriptLines = new string[]
    {
        "A year. Long enough for the rain to peel the posters off the fence.",
        "The file says the case was closed for \"insufficient evidence.\" Nobody who knew her believed that.",
        "Then, last week, one message from Adrian Vale: \"If you want to know what happened, come to the old greenhouse on the 14th. Bring a blue flower. I'll tell you everything.\"",
        "A year of silence. Why now?"
    };

    private readonly string[] speakerNames = new string[]
    {
        "YOU",
        "YOU",
        "YOU",
        "YOU"
    };

    private int currentLineIndex = 0;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private bool isInspectingMemorial = false;
    private Coroutine inspectFadeCoroutine;
    private bool isFreeView = false;
    private Coroutine dialogueFadeCoroutine;

    public int CurrentLineIndex => currentLineIndex;
    public string CurrentSaveChapterName => "Chapter I — The Long Walk In";

    private void Awake()
    {
        CleanupUnwantedElements();
        EnsureSceneSprites();
        EnsureMemorialPosterUI();
        EnsureSchoolBGM();
    }

    private void CleanupUnwantedElements()
    {
        // 1. Remove any legacy "Button" Text objects created by DefaultControls.CreateButton
        Text[] legacyTexts = GetComponentsInChildren<Text>(true);
        foreach (Text t in legacyTexts)
        {
            if (t != null && (t.text == "Button" || t.gameObject.name.Contains("Text (Legacy)") || t.gameObject.name == "Text"))
            {
                Destroy(t.gameObject);
            }
        }

        // 2. Remove HeaderBadge ("PROLOGUE — FIRST DAY OF SCHOOL")
        if (chapterBadgeText != null)
        {
            Destroy(chapterBadgeText.gameObject);
            chapterBadgeText = null;
        }
        Transform badgeTf = transform.Find("HeaderBadge");
        if (badgeTf != null)
        {
            Destroy(badgeTf.gameObject);
        }
    }

    private void EnsureSchoolBGM()
    {
        if (schoolBgmClip == null)
        {
            schoolBgmClip = Resources.Load<AudioClip>("darkmelody");
        }

        if (schoolBgmSource == null)
        {
            GameObject bgmObj = GameObject.Find("BGM");
            if (bgmObj == null)
            {
                bgmObj = new GameObject("BGM", typeof(AudioSource));
            }
            schoolBgmSource = bgmObj.GetComponent<AudioSource>();
            if (schoolBgmSource == null)
            {
                schoolBgmSource = bgmObj.AddComponent<AudioSource>();
            }
        }

        if (schoolBgmSource != null)
        {
            schoolBgmSource.spatialBlend = 0f;
            schoolBgmSource.loop = true;
            schoolBgmSource.playOnAwake = true;
            schoolBgmSource.volume = PlayerPrefs.GetFloat("BGM_Volume", 0.8f);

            if (schoolBgmClip != null && schoolBgmSource.clip != schoolBgmClip)
            {
                schoolBgmSource.Stop();
                schoolBgmSource.clip = schoolBgmClip;
            }

            if (schoolBgmSource.clip != null && !schoolBgmSource.isPlaying)
            {
                schoolBgmSource.Play();
            }
        }
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

        // Ensure dialogue box text uses the easy-to-read default TMP font and spans the full width of the box
        if (dialogueText != null)
        {
            TMP_FontAsset defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (defaultFont != null && dialogueText.font != defaultFont)
            {
                dialogueText.font = defaultFont;
            }
            dialogueText.textWrappingMode = TextWrappingModes.Normal;
            dialogueText.margin = Vector4.zero;

            RectTransform diaRect = dialogueText.GetComponent<RectTransform>();
            if (diaRect != null)
            {
                diaRect.anchorMin = new Vector2(0.035f, 0.14f);
                diaRect.anchorMax = new Vector2(0.965f, 0.74f);
                diaRect.offsetMin = Vector2.zero;
                diaRect.offsetMax = Vector2.zero;
            }
        }
    }

    private const string MEMORIAL_INSPECT_DIALOGUE =
        "\"Elara Veyne. 18. Forever the brightest person in every room.\" Someone has drawn a small blue flower in the corner, in pen.";

    private const string NOTICE_BOARD_INSPECT_DIALOGUE =
        "A rain-stained flyer for last year's winter festival is still pinned there. Most of the program has run in the weather, and only the words \"Lantern Walk\" can still be read.";

    private const string TARP_LANTERNS_INSPECT_DIALOGUE =
        "Festival lanterns still sit under a tarp, a year later. Nobody had the heart to throw them out.";

    private string currentInspectionLine = "";
    private float inspectDialogueFinishedTime = -1f;

    private static readonly Color INSPECT_BACKDROP_COLOR = new Color(0.02f, 0.04f, 0.08f, 0.60f);

    public void PlayHoverSound()
    {
        if (hoverClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(hoverClip, SettingsMenu.GlobalSoundVolume * 0.7f);
        }
    }

    public bool IsInspectOrPopupOpen()
    {
        if (isInspectingMemorial) return true;
        if (saveArchiveMenu != null && saveArchiveMenu.IsOpen) return true;
        if (settingsMenu != null && settingsMenu.gameObject.activeInHierarchy)
        {
            CanvasGroup cg = settingsMenu.GetComponent<CanvasGroup>();
            if (cg == null || cg.alpha > 0.05f) return true;
        }
        return false;
    }

    public bool CanHighlightItems()
    {
        if (!isFreeView) return false;
        if (isInspectingMemorial) return false;
        if (isTyping) return false;
        if (dialoguePanel != null && dialoguePanel.activeSelf) return false;
        if (IsPopupOpen()) return false;
        return true;
    }

    public bool CanInteractWithWorldItems()
    {
        if (!isFreeView) return false;
        if (isInspectingMemorial) return false;
        if (isTyping) return false;
        if (dialoguePanel != null && dialoguePanel.activeSelf) return false;
        if (IsPopupOpen()) return false;
        return true;
    }

    private void EnsureMemorialPosterUI()
    {
        Sprite memorialSprite = LoadResourceSprite("memorial_poster");
        Sprite noticeBoardSprite = LoadResourceSprite("notice_board");
        Sprite noticeBoardWorldSprite = LoadResourceSprite("notice_board_world");
        if (noticeBoardWorldSprite == null) noticeBoardWorldSprite = noticeBoardSprite;
        Sprite tarpLanternsSprite = LoadResourceSprite("tarp_lanterns");
        Sprite haloSprite = LoadResourceSprite("inspect_halo_glow");

        // 1. World-space clickable Memorial Poster beside the right side of the gate
        if (memorialPosterButton == null)
        {
            Transform existingPosterTf = transform.Find("MemorialPosterObject");
            if (existingPosterTf != null)
            {
                memorialPosterButton = existingPosterTf.GetComponent<Button>();
            }
        }

        if (memorialPosterButton == null)
        {
            GameObject posterObj = new GameObject("MemorialPosterObject", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(InteractableItemHighlight));
            posterObj.transform.SetParent(transform, false);

            // Place above AdvanceTrigger but behind DialogueBox & popups
            Transform advTf = transform.Find("AdvanceTrigger");
            if (advTf != null)
            {
                posterObj.transform.SetSiblingIndex(advTf.GetSiblingIndex() + 1);
            }

            RectTransform posterRect = posterObj.GetComponent<RectTransform>();
            posterRect.anchorMin = new Vector2(0.708f, 0.242f);
            posterRect.anchorMax = new Vector2(0.837f, 0.471f);
            posterRect.offsetMin = Vector2.zero;
            posterRect.offsetMax = Vector2.zero;

            Image posterImg = posterObj.GetComponent<Image>();
            posterImg.sprite = memorialSprite;
            posterImg.type = Image.Type.Simple;
            posterImg.preserveAspect = true;
            posterImg.color = Color.white;
            posterImg.raycastTarget = true;

            memorialPosterButton = posterObj.GetComponent<Button>();
            memorialPosterButton.targetGraphic = posterImg;
            ColorBlock cb = memorialPosterButton.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = Color.white;
            cb.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            cb.selectedColor = Color.white;
            cb.disabledColor = Color.white;
            memorialPosterButton.colors = cb;

            InteractableItemHighlight hl = posterObj.GetComponent<InteractableItemHighlight>();
            if (hl != null) hl.EnsureOverlay();
        }
        else
        {
            Image posterImg = memorialPosterButton.GetComponent<Image>();
            if (posterImg != null)
            {
                if (memorialSprite != null) posterImg.sprite = memorialSprite;
                posterImg.preserveAspect = true;
                posterImg.color = Color.white;
                posterImg.raycastTarget = true;
            }

            InteractableItemHighlight hl = memorialPosterButton.GetComponent<InteractableItemHighlight>();
            if (hl == null) hl = memorialPosterButton.gameObject.AddComponent<InteractableItemHighlight>();
            hl.EnsureOverlay();
        }

        if (memorialPosterButton != null)
        {
            memorialPosterButton.onClick.RemoveAllListeners();
            memorialPosterButton.onClick.AddListener(OnMemorialPosterClicked);
        }

        // 1b. World-space clickable Notice Board on the left side of the gate
        if (noticeBoardButton == null)
        {
            Transform existingBoardTf = transform.Find("NoticeBoardObject");
            if (existingBoardTf != null)
            {
                noticeBoardButton = existingBoardTf.GetComponent<Button>();
            }
        }

        if (noticeBoardButton == null)
        {
            GameObject boardObj = new GameObject("NoticeBoardObject", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(InteractableItemHighlight));
            boardObj.transform.SetParent(transform, false);

            // Place right next to MemorialPosterObject (above AdvanceTrigger, behind MemorialInspectModal & DialogueBox)
            if (memorialPosterButton != null)
            {
                boardObj.transform.SetSiblingIndex(memorialPosterButton.transform.GetSiblingIndex() + 1);
            }
            else
            {
                Transform advTf = transform.Find("AdvanceTrigger");
                if (advTf != null)
                {
                    boardObj.transform.SetSiblingIndex(advTf.GetSiblingIndex() + 1);
                }
            }

            RectTransform boardRect = boardObj.GetComponent<RectTransform>();
            boardRect.anchorMin = new Vector2(0.5342f, 0.3503f);
            boardRect.anchorMax = new Vector2(0.5908f, 0.4635f);
            boardRect.offsetMin = Vector2.zero;
            boardRect.offsetMax = Vector2.zero;

            Image boardImg = boardObj.GetComponent<Image>();
            boardImg.sprite = noticeBoardWorldSprite;
            boardImg.type = Image.Type.Simple;
            boardImg.preserveAspect = false;
            boardImg.color = Color.white;
            boardImg.raycastTarget = true;

            noticeBoardButton = boardObj.GetComponent<Button>();
            noticeBoardButton.targetGraphic = boardImg;
            ColorBlock ncb = noticeBoardButton.colors;
            ncb.normalColor = Color.white;
            ncb.highlightedColor = Color.white;
            ncb.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            ncb.selectedColor = Color.white;
            ncb.disabledColor = Color.white;
            noticeBoardButton.colors = ncb;

            InteractableItemHighlight hl = boardObj.GetComponent<InteractableItemHighlight>();
            if (hl != null) hl.EnsureOverlay();
        }
        else
        {
            RectTransform boardRect = noticeBoardButton.GetComponent<RectTransform>();
            if (boardRect != null)
            {
                boardRect.anchorMin = new Vector2(0.5342f, 0.3503f);
                boardRect.anchorMax = new Vector2(0.5908f, 0.4635f);
                boardRect.offsetMin = Vector2.zero;
                boardRect.offsetMax = Vector2.zero;
            }

            Image boardImg = noticeBoardButton.GetComponent<Image>();
            if (boardImg != null && noticeBoardWorldSprite != null)
            {
                boardImg.sprite = noticeBoardWorldSprite;
                boardImg.preserveAspect = false;
                boardImg.color = Color.white;
            }

            InteractableItemHighlight hl = noticeBoardButton.GetComponent<InteractableItemHighlight>();
            if (hl == null) hl = noticeBoardButton.gameObject.AddComponent<InteractableItemHighlight>();
            hl.EnsureOverlay();
        }

        if (noticeBoardButton != null)
        {
            noticeBoardButton.onClick.RemoveAllListeners();
            noticeBoardButton.onClick.AddListener(OnNoticeBoardClicked);
        }

        // 1c. World-space clickable Tarp Lanterns on the roadside in front of the retaining wall on the left
        if (tarpLanternsButton == null)
        {
            Transform existingTarpTf = transform.Find("TarpLanternsObject");
            if (existingTarpTf != null)
            {
                tarpLanternsButton = existingTarpTf.GetComponent<Button>();
            }
        }

        if (tarpLanternsButton == null)
        {
            GameObject tarpObj = new GameObject("TarpLanternsObject", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(InteractableItemHighlight));
            tarpObj.transform.SetParent(transform, false);

            if (noticeBoardButton != null)
            {
                tarpObj.transform.SetSiblingIndex(noticeBoardButton.transform.GetSiblingIndex() + 1);
            }
            else if (memorialPosterButton != null)
            {
                tarpObj.transform.SetSiblingIndex(memorialPosterButton.transform.GetSiblingIndex() + 1);
            }
            else
            {
                Transform advTf = transform.Find("AdvanceTrigger");
                if (advTf != null)
                {
                    tarpObj.transform.SetSiblingIndex(advTf.GetSiblingIndex() + 1);
                }
            }

            RectTransform tarpRect = tarpObj.GetComponent<RectTransform>();
            tarpRect.anchorMin = new Vector2(0.451f, 0.237f);
            tarpRect.anchorMax = new Vector2(0.573f, 0.405f);
            tarpRect.offsetMin = new Vector2(-27f, -44.7f);
            tarpRect.offsetMax = new Vector2(-27f, -44.7f);

            Image tarpImg = tarpObj.GetComponent<Image>();
            tarpImg.sprite = tarpLanternsSprite;
            tarpImg.type = Image.Type.Simple;
            tarpImg.preserveAspect = true;
            tarpImg.color = Color.white;
            tarpImg.raycastTarget = true;

            tarpLanternsButton = tarpObj.GetComponent<Button>();
            tarpLanternsButton.targetGraphic = tarpImg;
            ColorBlock tcb = tarpLanternsButton.colors;
            tcb.normalColor = Color.white;
            tcb.highlightedColor = Color.white;
            tcb.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            tcb.selectedColor = Color.white;
            tcb.disabledColor = Color.white;
            tarpLanternsButton.colors = tcb;

            InteractableItemHighlight hl = tarpObj.GetComponent<InteractableItemHighlight>();
            if (hl != null) hl.EnsureOverlay();
        }
        else
        {
            RectTransform tarpRect = tarpLanternsButton.GetComponent<RectTransform>();
            if (tarpRect != null)
            {
                tarpRect.anchorMin = new Vector2(0.451f, 0.237f);
                tarpRect.anchorMax = new Vector2(0.573f, 0.405f);
                tarpRect.offsetMin = new Vector2(-27f, -44.7f);
            tarpRect.offsetMax = new Vector2(-27f, -44.7f);
            }

            Image tarpImg = tarpLanternsButton.GetComponent<Image>();
            if (tarpImg != null)
            {
                if (tarpLanternsSprite != null) tarpImg.sprite = tarpLanternsSprite;
                tarpImg.preserveAspect = true;
                tarpImg.color = Color.white;
                tarpImg.raycastTarget = true;
            }

            InteractableItemHighlight hl = tarpLanternsButton.GetComponent<InteractableItemHighlight>();
            if (hl == null) hl = tarpLanternsButton.gameObject.AddComponent<InteractableItemHighlight>();
            hl.EnsureOverlay();
        }

        if (tarpLanternsButton != null)
        {
            tarpLanternsButton.onClick.RemoveAllListeners();
            tarpLanternsButton.onClick.AddListener(OnTarpLanternsClicked);
        }

        // 2. Center Inspection Modal when the player interacts with an item
        if (memorialInspectCanvasGroup == null)
        {
            Transform existingModalTf = transform.Find("MemorialInspectModal");
            if (existingModalTf != null)
            {
                memorialInspectCanvasGroup = existingModalTf.GetComponent<CanvasGroup>();
                closeInspectButton = existingModalTf.GetComponent<Button>();
            }
        }

        if (memorialInspectCanvasGroup == null)
        {
            GameObject modalObj = new GameObject("MemorialInspectModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(CanvasGroup));
            modalObj.transform.SetParent(transform, false);

            // Place BEFORE DialogueBox so the background fades while DialogueBox remains unblocked on top
            if (dialoguePanel != null)
            {
                modalObj.transform.SetSiblingIndex(dialoguePanel.transform.GetSiblingIndex());
            }

            RectTransform modalRect = modalObj.GetComponent<RectTransform>();
            modalRect.anchorMin = Vector2.zero;
            modalRect.anchorMax = Vector2.one;
            modalRect.offsetMin = Vector2.zero;
            modalRect.offsetMax = Vector2.zero;

            // Faded backdrop so the background dims slightly and the player can focus on the inspected item
            Image backdropImg = modalObj.GetComponent<Image>();
            backdropImg.color = INSPECT_BACKDROP_COLOR;
            backdropImg.raycastTarget = true;

            closeInspectButton = modalObj.GetComponent<Button>();
            closeInspectButton.transition = Selectable.Transition.None;

            memorialInspectCanvasGroup = modalObj.GetComponent<CanvasGroup>();
            memorialInspectCanvasGroup.alpha = 0f;
            memorialInspectCanvasGroup.interactable = false;
            memorialInspectCanvasGroup.blocksRaycasts = false;

            // Soft radial light glow behind the centered item (matching reference image)
            if (haloSprite != null)
            {
                GameObject haloObj = new GameObject("InspectHaloGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                haloObj.transform.SetParent(modalObj.transform, false);
                RectTransform haloRect = haloObj.GetComponent<RectTransform>();
                haloRect.anchorMin = new Vector2(0.24f, 0.25f);
                haloRect.anchorMax = new Vector2(0.76f, 0.99f);
                haloRect.offsetMin = Vector2.zero;
                haloRect.offsetMax = Vector2.zero;

                Image haloImg = haloObj.GetComponent<Image>();
                haloImg.sprite = haloSprite;
                haloImg.preserveAspect = true;
                haloImg.color = new Color(1f, 0.98f, 0.96f, 0.88f);
                haloImg.raycastTarget = false;
            }

            // Centered enlarged Item Preview in the middle above DialogueBox (y: 0.305 to 0.965)
            GameObject centerImgObj = new GameObject("MemorialInspectImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            centerImgObj.transform.SetParent(modalObj.transform, false);
            RectTransform centerRect = centerImgObj.GetComponent<RectTransform>();
            centerRect.anchorMin = new Vector2(0.31f, 0.305f);
            centerRect.anchorMax = new Vector2(0.69f, 0.965f);
            centerRect.offsetMin = Vector2.zero;
            centerRect.offsetMax = Vector2.zero;

            Image centerImg = centerImgObj.GetComponent<Image>();
            centerImg.sprite = memorialSprite;
            centerImg.type = Image.Type.Simple;
            centerImg.preserveAspect = true;
            centerImg.color = Color.white;
            centerImg.raycastTarget = false;

            modalObj.SetActive(false);
        }
        else
        {
            // Ensure existing modal stays behind DialogueBox and uses the updated faded backdrop & layout
            if (dialoguePanel != null && memorialInspectCanvasGroup.transform.GetSiblingIndex() > dialoguePanel.transform.GetSiblingIndex())
            {
                memorialInspectCanvasGroup.transform.SetSiblingIndex(dialoguePanel.transform.GetSiblingIndex());
            }

            Image backdropImg = memorialInspectCanvasGroup.GetComponent<Image>();
            if (backdropImg != null)
            {
                backdropImg.color = INSPECT_BACKDROP_COLOR;
            }

            Transform oldHintTf = memorialInspectCanvasGroup.transform.Find("InspectHintText");
            if (oldHintTf != null)
            {
                Destroy(oldHintTf.gameObject);
            }

            Transform centerImgTf = memorialInspectCanvasGroup.transform.Find("MemorialInspectImage");
            if (centerImgTf != null)
            {
                RectTransform centerRect = centerImgTf.GetComponent<RectTransform>();
                if (centerRect != null)
                {
                    centerRect.anchorMin = new Vector2(0.31f, 0.305f);
                    centerRect.anchorMax = new Vector2(0.69f, 0.965f);
                    centerRect.offsetMin = Vector2.zero;
                    centerRect.offsetMax = Vector2.zero;
                }
            }
        }

        if (closeInspectButton != null)
        {
            closeInspectButton.onClick.RemoveAllListeners();
            closeInspectButton.onClick.AddListener(CloseMemorialInspect);
        }
    }

    public void OnMemorialPosterClicked()
    {
        if (!CanInteractWithWorldItems())
        {
            if (!isInspectingMemorial && !IsPopupOpen() && !isFreeView)
            {
                OnNextClicked();
            }
            return;
        }
        Sprite memorialSprite = LoadResourceSprite("memorial_poster");
        InspectItem(memorialSprite, MEMORIAL_INSPECT_DIALOGUE);
    }

    public void OnNoticeBoardClicked()
    {
        if (!CanInteractWithWorldItems())
        {
            if (!isInspectingMemorial && !IsPopupOpen() && !isFreeView)
            {
                OnNextClicked();
            }
            return;
        }
        Sprite noticeBoardSprite = LoadResourceSprite("notice_board");
        InspectItem(noticeBoardSprite, NOTICE_BOARD_INSPECT_DIALOGUE);
    }

    public void OnTarpLanternsClicked()
    {
        if (!CanInteractWithWorldItems())
        {
            if (!isInspectingMemorial && !IsPopupOpen() && !isFreeView)
            {
                OnNextClicked();
            }
            return;
        }
        Sprite tarpLanternsSprite = LoadResourceSprite("tarp_lanterns");
        InspectItem(tarpLanternsSprite, TARP_LANTERNS_INSPECT_DIALOGUE);
    }

    private void InspectItem(Sprite itemSprite, string dialogueLine)
    {
        if (IsPopupOpen() || !isFreeView) return;

        if (clickClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickClip, SettingsMenu.GlobalSoundVolume);
        }

        EnsureMemorialPosterUI();
        if (memorialInspectCanvasGroup == null) return;

        currentInspectionLine = dialogueLine;

        Transform centerImgTf = memorialInspectCanvasGroup.transform.Find("MemorialInspectImage");
        if (centerImgTf != null)
        {
            Image centerImg = centerImgTf.GetComponent<Image>();
            if (centerImg != null && itemSprite != null)
            {
                centerImg.sprite = itemSprite;
                centerImg.preserveAspect = true;
                centerImg.color = Color.white;
            }
        }

        isInspectingMemorial = true;
        memorialInspectCanvasGroup.gameObject.SetActive(true);
        memorialInspectCanvasGroup.interactable = true;
        memorialInspectCanvasGroup.blocksRaycasts = true;

        if (inspectFadeCoroutine != null) StopCoroutine(inspectFadeCoroutine);
        inspectFadeCoroutine = StartCoroutine(FadeInspectModalRoutine(memorialInspectCanvasGroup.alpha, 1f, 0.22f, false));

        // Pop up DialogueBox with the player's inspection line
        ShowInspectionDialogue(dialogueLine);
    }

    private void ShowInspectionDialogue(string dialogueLine)
    {
        inspectDialogueFinishedTime = -1f;

        if (dialogueFadeCoroutine != null)
        {
            StopCoroutine(dialogueFadeCoroutine);
            dialogueFadeCoroutine = null;
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            CanvasGroup cg = dialoguePanel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
            }
        }

        string playerName = GetPlayerName();
        if (speakerPlateObject != null)
        {
            speakerPlateObject.SetActive(true);
        }
        if (speakerNameText != null)
        {
            speakerNameText.text = $"<b>{playerName}</b>";
            speakerNameText.gameObject.SetActive(true);
        }

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeLineRoutine(dialogueLine));
    }

    public void CloseMemorialInspect()
    {
        if (!isInspectingMemorial) return;

        // Gotta finish the dialogue first before closing: reveal full text on first click
        if (isTyping)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }
            isTyping = false;
            if (dialogueText != null && !string.IsNullOrEmpty(currentInspectionLine))
            {
                dialogueText.text = currentInspectionLine;
            }
            inspectDialogueFinishedTime = Time.unscaledTime;
            return;
        }

        // Prevent accidental closing within a brief grace period (0.35s) after text completes,
        // guaranteeing the dialogue must be finished first before the player can close it
        if (inspectDialogueFinishedTime > 0f && Time.unscaledTime - inspectDialogueFinishedTime < 0.35f)
        {
            return;
        }

        if (clickClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickClip, SettingsMenu.GlobalSoundVolume);
        }

        isInspectingMemorial = false;
        isFreeView = true;

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(false);
        }

        if (memorialInspectCanvasGroup != null)
        {
            memorialInspectCanvasGroup.interactable = false;
            memorialInspectCanvasGroup.blocksRaycasts = false;

            if (inspectFadeCoroutine != null) StopCoroutine(inspectFadeCoroutine);
            inspectFadeCoroutine = StartCoroutine(FadeInspectModalRoutine(memorialInspectCanvasGroup.alpha, 0f, 0.18f, true));
        }

        // Also close the dialogue box along with the item preview
        if (dialoguePanel != null && dialoguePanel.activeSelf)
        {
            if (dialogueFadeCoroutine != null) StopCoroutine(dialogueFadeCoroutine);
            dialogueFadeCoroutine = StartCoroutine(FadeOutDialoguePanelRoutine(0.20f));
        }
    }

    private IEnumerator FadeInspectModalRoutine(float fromAlpha, float toAlpha, float duration, bool deactivateOnComplete)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (memorialInspectCanvasGroup != null)
            {
                memorialInspectCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, Mathf.SmoothStep(0f, 1f, t));
            }
            yield return null;
        }

        if (memorialInspectCanvasGroup != null)
        {
            memorialInspectCanvasGroup.alpha = toAlpha;
            if (deactivateOnComplete)
            {
                memorialInspectCanvasGroup.gameObject.SetActive(false);
            }
        }
        inspectFadeCoroutine = null;
    }

    private void Start()
    {
        CleanupUnwantedElements();
        EnsureSceneSprites();
        EnsureMemorialPosterUI();
        EnsureSchoolBGM();

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
        if (isInspectingMemorial)
        {
            return true;
        }

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

        if (isInspectingMemorial)
        {
            bool closeKey = false;
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame ||
                 UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame ||
                 UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame))
            {
                closeKey = true;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                closeKey = true;
            }
#endif
            if (closeKey)
            {
                CloseMemorialInspect();
            }
            return;
        }

        if (isFreeView || IsPopupOpen())
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
        if (isInspectingMemorial)
        {
            CloseMemorialInspect();
            return;
        }

        if (isFreeView || IsPopupOpen())
        {
            return;
        }

        if (isTyping)
        {
            // Skip typing and show full text immediately
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            dialogueText.text = FormatWithPlayerName(scriptLines[currentLineIndex]);
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
            EnterFreeViewMode();
        }
    }

    private void EnterFreeViewMode()
    {
        if (isFreeView) return;
        isFreeView = true;

        // Disable full-screen advance button so the scene is unobstructed for free view & item interaction
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(false);
        }

        if (dialoguePanel != null)
        {
            if (dialogueFadeCoroutine != null) StopCoroutine(dialogueFadeCoroutine);
            dialogueFadeCoroutine = StartCoroutine(FadeOutDialoguePanelRoutine(0.25f));
        }
    }

    private IEnumerator FadeOutDialoguePanelRoutine(float duration)
    {
        CanvasGroup cg = dialoguePanel.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = dialoguePanel.AddComponent<CanvasGroup>();
        }

        cg.interactable = false;
        cg.blocksRaycasts = false;

        float startAlpha = cg.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            cg.alpha = Mathf.Lerp(startAlpha, 0f, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        cg.alpha = 0f;
        dialoguePanel.SetActive(false);
        dialogueFadeCoroutine = null;
    }

    private string GetPlayerName()
    {
        string savedName = PlayerPrefs.GetString(OpeningSceneController.PLAYER_NAME_PREF_KEY, "Protagonist");
        return string.IsNullOrWhiteSpace(savedName) ? "Protagonist" : savedName.Trim();
    }

    private string FormatWithPlayerName(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        string playerName = GetPlayerName();
        return text.Replace("{PlayerName}", playerName).Replace("{player}", playerName);
    }

    private void ShowLine(int index)
    {
        string speaker = (index >= 0 && index < speakerNames.Length) ? speakerNames[index] : "YOU";
        if (speaker == "YOU" || speaker == "You" || speaker == "Protagonist" || speaker == "{PlayerName}")
        {
            speaker = GetPlayerName();
        }
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
        typingCoroutine = StartCoroutine(TypeLineRoutine(FormatWithPlayerName(scriptLines[index])));
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
        if (isInspectingMemorial)
        {
            inspectDialogueFinishedTime = Time.unscaledTime;
        }
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

        if (schoolBgmSource != null)
        {
            schoolBgmSource.Stop();
        }

        SceneTransitionManager.LoadScene("MenuScreen", 0.5f);
    }

    private void OnDestroy()
    {
        if (schoolBgmSource != null)
        {
            schoolBgmSource.Stop();
        }
    }
}
