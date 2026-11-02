#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[InitializeOnLoad]
public static class SchoolSceneBuilder
{
    private const string SCENE_PATH = "Assets/Scenes/SchoolScene.unity";
    private const string MENU_SCENE_PATH = "Assets/Scenes/MenuScreen.unity";
    private const string SESSION_BUILD_KEY = "SchoolScene_Built_V23";

    static SchoolSceneBuilder()
    {
        EditorApplication.update += RunOnce;
    }

    private static void RunOnce()
    {
        EditorApplication.update -= RunOnce;
        CheckAndBuild();
    }

    public static void CheckAndBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        bool needsRebuild = !File.Exists(SCENE_PATH) || !SessionState.GetBool(SESSION_BUILD_KEY, false);
        if (needsRebuild)
        {
            SessionState.SetBool(SESSION_BUILD_KEY, true);
            BuildSchoolScene();
            SetupStartButtonInMenu();
        }
        else if (EditorSceneManager.GetActiveScene().path == MENU_SCENE_PATH)
        {
            SetupStartButtonInMenu();
        }
        RegisterSceneInBuildSettings();
    }

    [MenuItem("Tools/Build School Scene & Setup Start Button")]
    public static void ManualBuild()
    {
        SessionState.SetBool(SESSION_BUILD_KEY, true);
        BuildSchoolScene();
        SetupStartButtonInMenu();
        RegisterSceneInBuildSettings();
        Debug.Log("<color=green>[SchoolSceneBuilder] Build completed successfully!</color>");
    }

    private static Sprite TryLoadSpriteAtPath(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        if (s == null)
        {
            Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object o in all)
            {
                if (o is Sprite sp) return sp;
            }
        }
        return s;
    }

    private static Sprite LoadOrImportSprite(string primaryPath, string fallbackPath = null)
    {
        Sprite s = TryLoadSpriteAtPath(primaryPath);
        if (s != null) return s;
        return TryLoadSpriteAtPath(fallbackPath);
    }

    public static void BuildSchoolScene()
    {
        Sprite bgSprite = LoadOrImportSprite("Assets/Resources/cherry_blossom_school.png", "Assets/Art/cherry_blossom_school.png");
        Sprite dialogueBoxSprite = LoadOrImportSprite("Assets/Resources/vn_dialogue_box.png", "Assets/Art/vn_dialogue_box.png");
        Sprite speakerPlateSprite = LoadOrImportSprite("Assets/Resources/vn_speaker_plate.png", "Assets/Art/vn_speaker_plate.png");
        Sprite gearIconSprite = LoadOrImportSprite("Assets/Resources/settings_gear_icon.png", "Assets/Art/settings_gear_icon.png");
        Sprite memorialSprite = LoadOrImportSprite("Assets/Resources/memorial_poster.png", "Assets/Art/memorial_poster.png");
        Sprite noticeBoardSprite = LoadOrImportSprite("Assets/Resources/notice_board.png", "Assets/Art/notice_board.png");
        Sprite noticeBoardWorldSprite = LoadOrImportSprite("Assets/Resources/notice_board_world.png", "Assets/Art/notice_board_world.png");
        if (noticeBoardWorldSprite == null) noticeBoardWorldSprite = noticeBoardSprite;
        Sprite tarpLanternsSprite = LoadOrImportSprite("Assets/Resources/tarp_lanterns.png", "Assets/Art/tarp_lanterns.png");
        Sprite haloSprite = LoadOrImportSprite("Assets/Resources/inspect_halo_glow.png");

        TMP_FontAsset defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        TMP_FontAsset menuFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Buttons/Fonts/BLKCHCRY SDF.asset");
        if (menuFont == null) menuFont = defaultFont;
        if (defaultFont == null) defaultFont = menuFont;

        AudioClip clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Sound Effect/click.mp3");
        AudioClip hoverClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Sound Effect/hover.mp3");
        AudioClip darkMelodyClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM/darkmelody.mp3");
        if (darkMelodyClip == null) darkMelodyClip = Resources.Load<AudioClip>("darkmelody");

        // Remember active scene to return to it
        string activeScenePath = EditorSceneManager.GetActiveScene().path;

        // Create new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Main Camera
        GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        Camera cam = camObj.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.08f, 0.12f, 1f);
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        camObj.tag = "MainCamera";

        // 2. Event System
        GameObject esObj = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        esObj.AddComponent<StandaloneInputModule>();
#endif

        // 2b. SchoolScene BGM (darkmelody.mp3 looped on SchoolScene only)
        GameObject bgmObj = new GameObject("BGM", typeof(AudioSource));
        AudioSource bgmSource = bgmObj.GetComponent<AudioSource>();
        bgmSource.clip = darkMelodyClip;
        bgmSource.loop = true;
        bgmSource.playOnAwake = true;
        bgmSource.spatialBlend = 0f;
        bgmSource.volume = PlayerPrefs.GetFloat("BGM_Volume", 0.8f);

        // 3. Canvas
        GameObject canvasObj = new GameObject("SchoolCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // 4. Background Image (Cherry Blossom School)
        GameObject bgObj = new GameObject("CherryBlossomBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgObj.transform.SetParent(canvasObj.transform, false);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        Image bgImg = bgObj.GetComponent<Image>();
        bgImg.sprite = bgSprite;
        bgImg.type = Image.Type.Simple;
        bgImg.preserveAspect = false;
        bgImg.color = Color.white;

        // 5. Floating Sakura Petals Overlay
        GameObject petalsObj = new GameObject("SakuraPetalsOverlay", typeof(RectTransform), typeof(SakuraPetalsOverlay));
        petalsObj.transform.SetParent(canvasObj.transform, false);
        RectTransform petalsRect = petalsObj.GetComponent<RectTransform>();
        petalsRect.anchorMin = Vector2.zero;
        petalsRect.anchorMax = Vector2.one;
        petalsRect.offsetMin = Vector2.zero;
        petalsRect.offsetMax = Vector2.zero;

        DefaultControls.Resources uiRes = new DefaultControls.Resources();

        // 6. Full-screen Advance Click Overlay (placed BEFORE interactive items & UI buttons so they sit on top)
        GameObject advanceObj = DefaultControls.CreateButton(uiRes);
        advanceObj.name = "AdvanceTrigger";
        advanceObj.transform.SetParent(canvasObj.transform, false);
        RectTransform advRect = advanceObj.GetComponent<RectTransform>();
        advRect.anchorMin = Vector2.zero;
        advRect.anchorMax = Vector2.one;
        advRect.offsetMin = Vector2.zero;
        advRect.offsetMax = Vector2.zero;

        Image advImg = advanceObj.GetComponent<Image>();
        advImg.color = new Color(0, 0, 0, 0);
        foreach (Text legacyText in advanceObj.GetComponentsInChildren<Text>(true))
        {
            if (legacyText != null) Object.DestroyImmediate(legacyText.gameObject);
        }

        // 7. Clickable Memorial Poster beside the right side of the School Gate
        GameObject memorialPosterObj = new GameObject("MemorialPosterObject", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(InteractableItemHighlight));
        memorialPosterObj.transform.SetParent(canvasObj.transform, false);
        RectTransform memorialRect = memorialPosterObj.GetComponent<RectTransform>();
        memorialRect.anchorMin = new Vector2(0.708f, 0.242f);
        memorialRect.anchorMax = new Vector2(0.837f, 0.471f);
        memorialRect.offsetMin = Vector2.zero;
        memorialRect.offsetMax = Vector2.zero;

        Image memorialImg = memorialPosterObj.GetComponent<Image>();
        memorialSprite = LoadOrImportSprite("Assets/Resources/memorial_poster.png", "Assets/Art/memorial_poster.png");
        memorialImg.sprite = memorialSprite;
        memorialImg.type = Image.Type.Simple;
        memorialImg.preserveAspect = true;
        memorialImg.color = Color.white;
        memorialImg.raycastTarget = true;

        Button memorialBtn = memorialPosterObj.GetComponent<Button>();
        memorialBtn.targetGraphic = memorialImg;
        ColorBlock mcb = memorialBtn.colors;
        mcb.normalColor = Color.white;
        mcb.highlightedColor = Color.white;
        mcb.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
        mcb.selectedColor = Color.white;
        mcb.disabledColor = Color.white;
        memorialBtn.colors = mcb;

        InteractableItemHighlight mhl = memorialPosterObj.GetComponent<InteractableItemHighlight>();
        if (mhl != null) mhl.EnsureOverlay();

        // 7b. Clickable Notice Board beside the left side of the School Gate
        GameObject noticeBoardObj = new GameObject("NoticeBoardObject", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(InteractableItemHighlight));
        noticeBoardObj.transform.SetParent(canvasObj.transform, false);
        RectTransform noticeBoardRect = noticeBoardObj.GetComponent<RectTransform>();
        noticeBoardRect.anchorMin = new Vector2(0.5342f, 0.3503f);
        noticeBoardRect.anchorMax = new Vector2(0.5908f, 0.4635f);
        noticeBoardRect.offsetMin = Vector2.zero;
        noticeBoardRect.offsetMax = Vector2.zero;

        Image noticeBoardImg = noticeBoardObj.GetComponent<Image>();
        noticeBoardImg.sprite = noticeBoardWorldSprite;
        noticeBoardImg.type = Image.Type.Simple;
        noticeBoardImg.preserveAspect = false;
        noticeBoardImg.color = Color.white;
        noticeBoardImg.raycastTarget = true;

        Button noticeBoardBtn = noticeBoardObj.GetComponent<Button>();
        noticeBoardBtn.targetGraphic = noticeBoardImg;
        ColorBlock ncb = noticeBoardBtn.colors;
        ncb.normalColor = Color.white;
        ncb.highlightedColor = Color.white;
        ncb.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
        ncb.selectedColor = Color.white;
        ncb.disabledColor = Color.white;
        noticeBoardBtn.colors = ncb;

        InteractableItemHighlight nhl = noticeBoardObj.GetComponent<InteractableItemHighlight>();
        if (nhl != null) nhl.EnsureOverlay();

        // 7c. Clickable Tarp Lanterns in front of retaining wall on the left
        GameObject tarpLanternsObj = new GameObject("TarpLanternsObject", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(InteractableItemHighlight));
        tarpLanternsObj.transform.SetParent(canvasObj.transform, false);
        RectTransform tarpLanternsRect = tarpLanternsObj.GetComponent<RectTransform>();
        tarpLanternsRect.anchorMin = new Vector2(0.451f, 0.237f);
        tarpLanternsRect.anchorMax = new Vector2(0.573f, 0.405f);
        tarpLanternsRect.offsetMin = new Vector2(-27f, -44.7f);
        tarpLanternsRect.offsetMax = new Vector2(-27f, -44.7f);

        Image tarpLanternsImg = tarpLanternsObj.GetComponent<Image>();
        tarpLanternsImg.sprite = tarpLanternsSprite;
        tarpLanternsImg.type = Image.Type.Simple;
        tarpLanternsImg.preserveAspect = true;
        tarpLanternsImg.color = Color.white;
        tarpLanternsImg.raycastTarget = true;

        Button tarpLanternsBtn = tarpLanternsObj.GetComponent<Button>();
        tarpLanternsBtn.targetGraphic = tarpLanternsImg;
        ColorBlock tcb = tarpLanternsBtn.colors;
        tcb.normalColor = Color.white;
        tcb.highlightedColor = Color.white;
        tcb.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
        tcb.selectedColor = Color.white;
        tcb.disabledColor = Color.white;
        tarpLanternsBtn.colors = tcb;

        InteractableItemHighlight thl = tarpLanternsObj.GetComponent<InteractableItemHighlight>();
        if (thl != null) thl.EnsureOverlay();

        // 8. Center Inspection Modal when the player interacts with an item (placed BEFORE DialogueBox so it fades the background without blocking DialogueBox)
        GameObject inspectModalObj = new GameObject("MemorialInspectModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(CanvasGroup));
        inspectModalObj.transform.SetParent(canvasObj.transform, false);
        RectTransform inspectModalRect = inspectModalObj.GetComponent<RectTransform>();
        inspectModalRect.anchorMin = Vector2.zero;
        inspectModalRect.anchorMax = Vector2.one;
        inspectModalRect.offsetMin = Vector2.zero;
        inspectModalRect.offsetMax = Vector2.zero;

        Image inspectBackdropImg = inspectModalObj.GetComponent<Image>();
        inspectBackdropImg.color = new Color(0.02f, 0.04f, 0.08f, 0.60f);
        inspectBackdropImg.raycastTarget = true;

        Button closeInspectBtn = inspectModalObj.GetComponent<Button>();
        closeInspectBtn.transition = Selectable.Transition.None;

        CanvasGroup inspectModalCg = inspectModalObj.GetComponent<CanvasGroup>();
        inspectModalCg.alpha = 0f;
        inspectModalCg.interactable = false;
        inspectModalCg.blocksRaycasts = false;

        GameObject haloObj = new GameObject("InspectHaloGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        haloObj.transform.SetParent(inspectModalObj.transform, false);
        RectTransform haloRect = haloObj.GetComponent<RectTransform>();
        haloRect.anchorMin = new Vector2(0.24f, 0.25f);
        haloRect.anchorMax = new Vector2(0.76f, 0.99f);
        haloRect.offsetMin = Vector2.zero;
        haloRect.offsetMax = Vector2.zero;

        Image haloImg = haloObj.GetComponent<Image>();
        haloImg.sprite = haloSprite;
        haloImg.type = Image.Type.Simple;
        haloImg.preserveAspect = true;
        haloImg.color = new Color(1f, 0.99f, 0.95f, 0.95f);
        haloImg.raycastTarget = false;

        GameObject inspectCenterImgObj = new GameObject("MemorialInspectImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        inspectCenterImgObj.transform.SetParent(inspectModalObj.transform, false);
        RectTransform inspectCenterRect = inspectCenterImgObj.GetComponent<RectTransform>();
        inspectCenterRect.anchorMin = new Vector2(0.31f, 0.305f);
        inspectCenterRect.anchorMax = new Vector2(0.69f, 0.965f);
        inspectCenterRect.offsetMin = Vector2.zero;
        inspectCenterRect.offsetMax = Vector2.zero;

        Image inspectCenterImg = inspectCenterImgObj.GetComponent<Image>();
        inspectCenterImg.sprite = memorialSprite;
        inspectCenterImg.type = Image.Type.Simple;
        inspectCenterImg.preserveAspect = true;
        inspectCenterImg.color = Color.white;
        inspectCenterImg.raycastTarget = false;

        inspectModalObj.SetActive(false);

        // 9. Restyled Visual Novel Dialogue Box (matching reference image)
        GameObject boxObj = new GameObject("DialogueBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        boxObj.transform.SetParent(canvasObj.transform, false);
        RectTransform boxRect = boxObj.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.07f, 0.04f);
        boxRect.anchorMax = new Vector2(0.93f, 0.29f);
        boxRect.offsetMin = Vector2.zero;
        boxRect.offsetMax = Vector2.zero;

        Image boxImg = boxObj.GetComponent<Image>();
        if (dialogueBoxSprite != null)
        {
            boxImg.sprite = dialogueBoxSprite;
            boxImg.type = Image.Type.Simple;
            boxImg.preserveAspect = false;
            boxImg.color = Color.white;
        }
        else
        {
            boxImg.color = new Color(0.02f, 0.14f, 0.19f, 0.94f);
        }
        boxImg.raycastTarget = false;

        // Overlapping Top-Left Speaker Nameplate ("R E S M E" with diamond ornaments)
        GameObject speakerPlateObj = new GameObject("SpeakerPlate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        speakerPlateObj.transform.SetParent(boxObj.transform, false);
        RectTransform plateRect = speakerPlateObj.GetComponent<RectTransform>();
        plateRect.anchorMin = new Vector2(0.015f, 0.80f);
        plateRect.anchorMax = new Vector2(0.275f, 1.15f);
        plateRect.offsetMin = Vector2.zero;
        plateRect.offsetMax = Vector2.zero;

        Image plateImg = speakerPlateObj.GetComponent<Image>();
        if (speakerPlateSprite != null)
        {
            plateImg.sprite = speakerPlateSprite;
            plateImg.type = Image.Type.Simple;
            plateImg.preserveAspect = false;
            plateImg.color = Color.white;
        }
        else
        {
            plateImg.color = new Color(0.06f, 0.28f, 0.36f, 0.96f);
        }
        plateImg.raycastTarget = false;

        // Speaker Name Text inside SpeakerPlate
        GameObject speakerObj = new GameObject("SpeakerName", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        speakerObj.transform.SetParent(speakerPlateObj.transform, false);
        RectTransform spRect = speakerObj.GetComponent<RectTransform>();
        spRect.anchorMin = new Vector2(0.12f, 0.08f);
        spRect.anchorMax = new Vector2(0.88f, 0.92f);
        spRect.offsetMin = Vector2.zero;
        spRect.offsetMax = Vector2.zero;

        TextMeshProUGUI spTmp = speakerObj.GetComponent<TextMeshProUGUI>();
        spTmp.text = "<b>You</b>";
        spTmp.font = menuFont;
        spTmp.fontSize = 28;
        spTmp.alignment = TextAlignmentOptions.Center;
        spTmp.color = new Color(0.93f, 0.98f, 1f, 1f);
        spTmp.raycastTarget = false;

        // Dialogue Text inside DialogueBox
        GameObject diaTextObj = new GameObject("DialogueText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        diaTextObj.transform.SetParent(boxObj.transform, false);
        RectTransform diaRect = diaTextObj.GetComponent<RectTransform>();
        diaRect.anchorMin = new Vector2(0.035f, 0.14f);
        diaRect.anchorMax = new Vector2(0.965f, 0.74f);
        diaRect.offsetMin = Vector2.zero;
        diaRect.offsetMax = Vector2.zero;

        TextMeshProUGUI diaTmp = diaTextObj.GetComponent<TextMeshProUGUI>();
        diaTmp.text = "A year. Long enough for the rain to peel the posters off the fence.";
        diaTmp.font = defaultFont;
        diaTmp.fontSize = 26;
        diaTmp.lineSpacing = 16f;
        diaTmp.textWrappingMode = TextWrappingModes.Normal;
        diaTmp.margin = Vector4.zero;
        diaTmp.alignment = TextAlignmentOptions.TopLeft;
        diaTmp.color = new Color(0.96f, 0.98f, 1f, 1f);
        diaTmp.raycastTarget = false;

        // Continue Indicator ("▼")
        GameObject indObj = new GameObject("Indicator", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        indObj.transform.SetParent(boxObj.transform, false);
        RectTransform indRect = indObj.GetComponent<RectTransform>();
        indRect.anchorMin = new Vector2(0.92f, 0.06f);
        indRect.anchorMax = new Vector2(0.98f, 0.22f);
        indRect.offsetMin = Vector2.zero;
        indRect.offsetMax = Vector2.zero;

        TextMeshProUGUI indTmp = indObj.GetComponent<TextMeshProUGUI>();
        indTmp.text = "\u25BC";
        indTmp.font = menuFont;
        indTmp.fontSize = 22;
        indTmp.alignment = TextAlignmentOptions.Center;
        indTmp.color = new Color(0.35f, 0.85f, 1f, 0.8f);
        indTmp.raycastTarget = false;

        // 10. Top-Left Settings Gear Icon Button (replaces the old text Return to Menu button)
        GameObject settingsBtnObj = DefaultControls.CreateButton(uiRes);
        settingsBtnObj.name = "SettingsIconButton";
        settingsBtnObj.transform.SetParent(canvasObj.transform, false);
        RectTransform settingsBtnRect = settingsBtnObj.GetComponent<RectTransform>();
        settingsBtnRect.anchorMin = new Vector2(0.03f, 0.92f);
        settingsBtnRect.anchorMax = new Vector2(0.03f, 0.92f);
        settingsBtnRect.pivot = new Vector2(0f, 0.5f);
        settingsBtnRect.anchoredPosition = Vector2.zero;
        settingsBtnRect.sizeDelta = new Vector2(74f, 74f);

        foreach (Text legacyText in settingsBtnObj.GetComponentsInChildren<Text>(true))
        {
            if (legacyText != null) Object.DestroyImmediate(legacyText.gameObject);
        }

        Image settingsBtnImg = settingsBtnObj.GetComponent<Image>();
        if (gearIconSprite != null)
        {
            settingsBtnImg.sprite = gearIconSprite;
            settingsBtnImg.type = Image.Type.Simple;
            settingsBtnImg.preserveAspect = true;
            settingsBtnImg.color = Color.white;
        }
        else
        {
            settingsBtnImg.color = new Color(0.10f, 0.35f, 0.45f, 0.95f);
        }

        Button settingsIconBtn = settingsBtnObj.GetComponent<Button>();
        ColorBlock scb = settingsIconBtn.colors;
        scb.normalColor = Color.white;
        scb.highlightedColor = new Color(1.2f, 1.35f, 1.45f, 1f);
        scb.pressedColor = new Color(0.75f, 0.9f, 1f, 1f);
        settingsIconBtn.colors = scb;

        AudioSource settingsBtnAudio = settingsBtnObj.AddComponent<AudioSource>();
        settingsBtnAudio.playOnAwake = false;
        ButtonFunction settingsBtnFunc = settingsBtnObj.AddComponent<ButtonFunction>();
        settingsBtnFunc.mySound = settingsBtnAudio;
        settingsBtnFunc.hoverSound = hoverClip;
        settingsBtnFunc.clickSound = clickClip;
        UnityEventTools.AddPersistentListener(settingsIconBtn.onClick, settingsBtnFunc.ClickSound);

        // 11. Build In-Game Settings Popup Panel (BGM Volume, Sound Volume, Save Progress, Return to Menu, Resume)
        SettingsMenu settingsMenu = BuildInGameSettingsPopup(canvas, bgmSource, menuFont, clickClip, hoverClip, out GameObject settingsPanelObj);

        // 12. Build SaveArchivePanel (in Save Mode so choosing a slot saves progress to that slot)
        SaveArchiveMenu saveArchiveMenu = SaveArchiveBuilder.BuildSaveArchiveForCanvas(canvas, settingsPanelObj, saveMode: true);

        // Wire SaveArchiveMenu into SettingsMenu
        if (settingsMenu != null && saveArchiveMenu != null)
        {
            SerializedObject settingsSo = new SerializedObject(settingsMenu);
            settingsSo.Update();
            settingsSo.FindProperty("saveArchiveMenu").objectReferenceValue = saveArchiveMenu;
            settingsSo.ApplyModifiedProperties();
        }

        // Wire SettingsIconButton -> settingsMenu.ToggleSettings
        if (settingsMenu != null)
        {
            UnityEventTools.AddPersistentListener(settingsIconBtn.onClick, settingsMenu.ToggleSettings);
        }

        // 13. Wire SchoolPrologueController Component
        SchoolPrologueController controller = canvasObj.AddComponent<SchoolPrologueController>();
        AudioSource ctrlAudio = canvasObj.AddComponent<AudioSource>();
        ctrlAudio.playOnAwake = false;

        UnityEventTools.AddPersistentListener(memorialBtn.onClick, controller.OnMemorialPosterClicked);
        UnityEventTools.AddPersistentListener(noticeBoardBtn.onClick, controller.OnNoticeBoardClicked);
        UnityEventTools.AddPersistentListener(tarpLanternsBtn.onClick, controller.OnTarpLanternsClicked);
        UnityEventTools.AddPersistentListener(closeInspectBtn.onClick, controller.CloseMemorialInspect);

        SerializedObject so = new SerializedObject(controller);
        so.Update();
        so.FindProperty("speakerPlateObject").objectReferenceValue = speakerPlateObj;
        so.FindProperty("speakerNameText").objectReferenceValue = spTmp;
        so.FindProperty("dialogueText").objectReferenceValue = diaTmp;
        so.FindProperty("nextButton").objectReferenceValue = advanceObj.GetComponent<Button>();
        so.FindProperty("settingsIconButton").objectReferenceValue = settingsIconBtn;
        so.FindProperty("backToMenuButton").objectReferenceValue = settingsIconBtn;
        so.FindProperty("dialoguePanel").objectReferenceValue = boxObj;
        so.FindProperty("settingsMenu").objectReferenceValue = settingsMenu;
        so.FindProperty("saveArchiveMenu").objectReferenceValue = saveArchiveMenu;
        so.FindProperty("memorialPosterButton").objectReferenceValue = memorialBtn;
        so.FindProperty("noticeBoardButton").objectReferenceValue = noticeBoardBtn;
        so.FindProperty("tarpLanternsButton").objectReferenceValue = tarpLanternsBtn;
        so.FindProperty("memorialInspectCanvasGroup").objectReferenceValue = inspectModalCg;
        so.FindProperty("closeInspectButton").objectReferenceValue = closeInspectBtn;
        so.FindProperty("schoolBgmSource").objectReferenceValue = bgmSource;
        so.FindProperty("schoolBgmClip").objectReferenceValue = darkMelodyClip;
        so.FindProperty("audioSource").objectReferenceValue = ctrlAudio;
        so.FindProperty("clickClip").objectReferenceValue = clickClip;
        so.FindProperty("hoverClip").objectReferenceValue = hoverClip;
        so.ApplyModifiedProperties();

        // 14. Save SchoolScene
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        Debug.Log($"<color=pink>[SchoolSceneBuilder] Rebuilt and saved {SCENE_PATH} with restyled DialogueBox, Settings Icon, Settings Popup, and SaveArchivePanel!</color>");

        // Return to previously active scene if different
        if (!string.IsNullOrEmpty(activeScenePath) && activeScenePath != SCENE_PATH && File.Exists(activeScenePath))
        {
            EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);
        }
    }

    private static SettingsMenu BuildInGameSettingsPopup(
        Canvas canvas,
        AudioSource bgmSource,
        TMP_FontAsset fontAsset,
        AudioClip clickClip,
        AudioClip hoverClip,
        out GameObject panelObj)
    {
        panelObj = new GameObject("SettingsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(Outline));
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(740f, 600f);

        Image panelImage = panelObj.GetComponent<Image>();
        panelImage.color = new Color(0.04f, 0.08f, 0.16f, 0.96f);

        Outline panelOutline = panelObj.GetComponent<Outline>();
        panelOutline.effectColor = new Color(0.30f, 0.72f, 0.88f, 0.75f);
        panelOutline.effectDistance = new Vector2(2f, -2f);

        CanvasGroup panelCg = panelObj.GetComponent<CanvasGroup>();
        panelCg.alpha = 0f;
        panelCg.interactable = false;
        panelCg.blocksRaycasts = false;

        AudioSource panelAudio = panelObj.AddComponent<AudioSource>();
        panelAudio.playOnAwake = false;

        SettingsMenu settingsMenu = panelObj.AddComponent<SettingsMenu>();
        DefaultControls.Resources uiResources = new DefaultControls.Resources();

        TMP_FontAsset bodyFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (bodyFont == null) bodyFont = fontAsset;

        // --- Helper: Button Creator ---
        Button CreatePillButton(Transform parent, string btnName, string label, Vector2 pos, Vector2 size, Color bgColor, Color outlineColor, int fontSize = 16, FontStyles fontStyle = FontStyles.Bold)
        {
            GameObject btnObj = DefaultControls.CreateButton(uiResources);
            btnObj.name = btnName;
            btnObj.transform.SetParent(parent, false);

            RectTransform br = btnObj.GetComponent<RectTransform>();
            br.anchorMin = new Vector2(0.5f, 0.5f);
            br.anchorMax = new Vector2(0.5f, 0.5f);
            br.pivot = new Vector2(0.5f, 0.5f);
            br.anchoredPosition = pos;
            br.sizeDelta = size;

            Image img = btnObj.GetComponent<Image>();
            img.color = bgColor;

            Outline ol = btnObj.AddComponent<Outline>();
            ol.effectColor = outlineColor;
            ol.effectDistance = new Vector2(1.2f, -1.2f);

            foreach (Text legacyText in btnObj.GetComponentsInChildren<Text>(true))
            {
                if (legacyText != null) Object.DestroyImmediate(legacyText.gameObject);
            }

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform tr = textObj.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            if (bodyFont != null) tmp.font = bodyFont;
            tmp.fontSize = fontSize;
            tmp.fontStyle = fontStyle;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            Button btn = btnObj.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.25f, 1.35f, 1.55f, 1f);
            cb.pressedColor = new Color(0.7f, 0.85f, 1f, 1f);
            btn.colors = cb;

            AudioSource btnAudio = btnObj.AddComponent<AudioSource>();
            btnAudio.playOnAwake = false;
            ButtonFunction bFunc = btnObj.AddComponent<ButtonFunction>();
            bFunc.mySound = btnAudio;
            bFunc.hoverSound = hoverClip;
            bFunc.clickSound = clickClip;
            UnityEventTools.AddPersistentListener(btn.onClick, bFunc.ClickSound);

            return btn;
        }

        // --- Helper: Horizontal Slider Creator ---
        void CreateStyledSlider(Transform parent, string sliderName, Vector2 pos, float width, float initVal, Color fillColor, out Slider sliderComponent)
        {
            GameObject sliderObj = DefaultControls.CreateSlider(uiResources);
            sliderObj.name = sliderName;
            sliderObj.transform.SetParent(parent, false);

            RectTransform sr = sliderObj.GetComponent<RectTransform>();
            sr.anchorMin = new Vector2(0.5f, 1f);
            sr.anchorMax = new Vector2(0.5f, 1f);
            sr.pivot = new Vector2(0.5f, 1f);
            sr.anchoredPosition = pos;
            sr.sizeDelta = new Vector2(width, 24f);

            Slider slider = sliderObj.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = initVal;

            Transform bgTf = sliderObj.transform.Find("Background");
            if (bgTf != null)
            {
                Image bgImg = bgTf.GetComponent<Image>();
                if (bgImg != null) bgImg.color = new Color(0.10f, 0.18f, 0.28f, 0.95f);
                RectTransform bgRect = bgTf.GetComponent<RectTransform>();
                bgRect.anchorMin = new Vector2(0f, 0.35f);
                bgRect.anchorMax = new Vector2(1f, 0.65f);
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;
            }

            if (slider.fillRect != null)
            {
                Image fillImg = slider.fillRect.GetComponent<Image>();
                if (fillImg != null) fillImg.color = fillColor;
                RectTransform fillRect = slider.fillRect.GetComponent<RectTransform>();
                fillRect.anchorMin = new Vector2(0f, 0.35f);
                fillRect.anchorMax = new Vector2(1f, 0.65f);
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;
            }

            if (slider.handleRect != null)
            {
                Image handleImg = slider.handleRect.GetComponent<Image>();
                if (handleImg != null) handleImg.color = new Color(0.95f, 0.98f, 1f, 1f);
                slider.handleRect.sizeDelta = new Vector2(24f, 24f);
            }

            sliderComponent = slider;
        }

        // --- 1. Top Icon Badge ---
        GameObject badgeObj = new GameObject("TopIconBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        badgeObj.transform.SetParent(panelObj.transform, false);
        RectTransform badgeRect = badgeObj.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0.5f, 1f);
        badgeRect.anchorMax = new Vector2(0.5f, 1f);
        badgeRect.pivot = new Vector2(0.5f, 1f);
        badgeRect.anchoredPosition = new Vector2(0, -20f);
        badgeRect.sizeDelta = new Vector2(40f, 40f);

        Image badgeImg = badgeObj.GetComponent<Image>();
        badgeImg.color = new Color(0.09f, 0.18f, 0.28f, 0.90f);

        Outline badgeOutline = badgeObj.GetComponent<Outline>();
        badgeOutline.effectColor = new Color(0.35f, 0.75f, 0.92f, 0.65f);
        badgeOutline.effectDistance = new Vector2(1.2f, -1.2f);

        GameObject badgeTextObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        badgeTextObj.transform.SetParent(badgeObj.transform, false);
        RectTransform btRect = badgeTextObj.GetComponent<RectTransform>();
        btRect.anchorMin = Vector2.zero;
        btRect.anchorMax = Vector2.one;
        btRect.offsetMin = Vector2.zero;
        btRect.offsetMax = Vector2.zero;

        TextMeshProUGUI btTmp = badgeTextObj.GetComponent<TextMeshProUGUI>();
        btTmp.text = "\u2699";
        btTmp.fontSize = 20;
        btTmp.alignment = TextAlignmentOptions.Center;
        btTmp.color = new Color(0.40f, 0.88f, 1f, 1f);

        // --- 2. Title Text ("Settings") ---
        GameObject titleObj = new GameObject("SettingsTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(panelObj.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -68f);
        titleRect.sizeDelta = new Vector2(550f, 44f);

        TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
        titleText.text = "Settings";
        if (fontAsset != null) titleText.font = fontAsset;
        titleText.fontSize = 44;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0.95f, 0.98f, 1f, 1f);

        // --- 3. Subtitle Text ---
        GameObject subObj = new GameObject("SettingsSubtitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        subObj.transform.SetParent(panelObj.transform, false);
        RectTransform subRect = subObj.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.5f, 1f);
        subRect.anchorMax = new Vector2(0.5f, 1f);
        subRect.pivot = new Vector2(0.5f, 1f);
        subRect.anchoredPosition = new Vector2(0, -112f);
        subRect.sizeDelta = new Vector2(620f, 24f);

        TextMeshProUGUI subText = subObj.GetComponent<TextMeshProUGUI>();
        subText.text = "A moment of quiet. Make yourself comfortable.";
        if (bodyFont != null) subText.font = bodyFont;
        subText.fontSize = 16;
        subText.alignment = TextAlignmentOptions.Center;
        subText.color = new Color(0.62f, 0.76f, 0.90f, 0.85f);

        // --- 4. Section Divider ("— AUDIO —") ---
        GameObject divObj = new GameObject("AudioDivider", typeof(RectTransform));
        divObj.transform.SetParent(panelObj.transform, false);
        RectTransform divRect = divObj.GetComponent<RectTransform>();
        divRect.anchorMin = new Vector2(0.5f, 1f);
        divRect.anchorMax = new Vector2(0.5f, 1f);
        divRect.pivot = new Vector2(0.5f, 1f);
        divRect.anchoredPosition = new Vector2(0, -140f);
        divRect.sizeDelta = new Vector2(620f, 20f);

        Color divLineColor = new Color(0.25f, 0.42f, 0.60f, 0.5f);

        GameObject lineL = new GameObject("LineLeft", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lineL.transform.SetParent(divObj.transform, false);
        RectTransform llRect = lineL.GetComponent<RectTransform>();
        llRect.anchorMin = new Vector2(0.5f, 0.5f);
        llRect.anchorMax = new Vector2(0.5f, 0.5f);
        llRect.pivot = new Vector2(0.5f, 0.5f);
        llRect.anchoredPosition = new Vector2(-175f, 0);
        llRect.sizeDelta = new Vector2(230f, 1.5f);
        lineL.GetComponent<Image>().color = divLineColor;

        GameObject lineR = new GameObject("LineRight", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lineR.transform.SetParent(divObj.transform, false);
        RectTransform lrRect = lineR.GetComponent<RectTransform>();
        lrRect.anchorMin = new Vector2(0.5f, 0.5f);
        lrRect.anchorMax = new Vector2(0.5f, 0.5f);
        lrRect.pivot = new Vector2(0.5f, 0.5f);
        lrRect.anchoredPosition = new Vector2(175f, 0);
        lrRect.sizeDelta = new Vector2(230f, 1.5f);
        lineR.GetComponent<Image>().color = divLineColor;

        GameObject divTextObj = new GameObject("AudioText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        divTextObj.transform.SetParent(divObj.transform, false);
        RectTransform dtRect = divTextObj.GetComponent<RectTransform>();
        dtRect.anchorMin = new Vector2(0.5f, 0.5f);
        dtRect.anchorMax = new Vector2(0.5f, 0.5f);
        dtRect.pivot = new Vector2(0.5f, 0.5f);
        dtRect.anchoredPosition = Vector2.zero;
        dtRect.sizeDelta = new Vector2(80f, 20f);

        TextMeshProUGUI dtTmp = divTextObj.GetComponent<TextMeshProUGUI>();
        dtTmp.text = "AUDIO";
        if (bodyFont != null) dtTmp.font = bodyFont;
        dtTmp.fontSize = 14;
        dtTmp.fontStyle = FontStyles.Bold;
        dtTmp.alignment = TextAlignmentOptions.Center;
        dtTmp.color = new Color(0.50f, 0.72f, 0.88f, 0.8f);

        // --- 5. BGM Volume Section ---
        GameObject bgmSection = new GameObject("BgmSection", typeof(RectTransform));
        bgmSection.transform.SetParent(panelObj.transform, false);
        RectTransform bgmSecRect = bgmSection.GetComponent<RectTransform>();
        bgmSecRect.anchorMin = new Vector2(0.5f, 1f);
        bgmSecRect.anchorMax = new Vector2(0.5f, 1f);
        bgmSecRect.pivot = new Vector2(0.5f, 1f);
        bgmSecRect.anchoredPosition = new Vector2(0, -170f);
        bgmSecRect.sizeDelta = new Vector2(620f, 108f);

        // Header: Label & Percent Badge
        GameObject bgmHeader = new GameObject("HeaderRow", typeof(RectTransform));
        bgmHeader.transform.SetParent(bgmSection.transform, false);
        RectTransform bhRect = bgmHeader.GetComponent<RectTransform>();
        bhRect.anchorMin = new Vector2(0.5f, 1f);
        bhRect.anchorMax = new Vector2(0.5f, 1f);
        bhRect.pivot = new Vector2(0.5f, 1f);
        bhRect.anchoredPosition = Vector2.zero;
        bhRect.sizeDelta = new Vector2(620f, 30f);

        GameObject bgmLabelObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        bgmLabelObj.transform.SetParent(bgmHeader.transform, false);
        RectTransform blRect = bgmLabelObj.GetComponent<RectTransform>();
        blRect.anchorMin = new Vector2(0f, 0.5f);
        blRect.anchorMax = new Vector2(0f, 0.5f);
        blRect.pivot = new Vector2(0f, 0.5f);
        blRect.anchoredPosition = Vector2.zero;
        blRect.sizeDelta = new Vector2(400f, 30f);

        TextMeshProUGUI bgmLabelTmp = bgmLabelObj.GetComponent<TextMeshProUGUI>();
        bgmLabelTmp.text = "\u266A  BGM Volume";
        if (bodyFont != null) bgmLabelTmp.font = bodyFont;
        bgmLabelTmp.fontSize = 20;
        bgmLabelTmp.fontStyle = FontStyles.Bold;
        bgmLabelTmp.alignment = TextAlignmentOptions.MidlineLeft;
        bgmLabelTmp.color = new Color(0.85f, 0.93f, 1f, 1f);

        // Right Badge Box for BGM
        GameObject bgmBadge = new GameObject("PercentBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        bgmBadge.transform.SetParent(bgmHeader.transform, false);
        RectTransform bbRect = bgmBadge.GetComponent<RectTransform>();
        bbRect.anchorMin = new Vector2(1f, 0.5f);
        bbRect.anchorMax = new Vector2(1f, 0.5f);
        bbRect.pivot = new Vector2(1f, 0.5f);
        bbRect.anchoredPosition = Vector2.zero;
        bbRect.sizeDelta = new Vector2(74f, 28f);

        Image bbImg = bgmBadge.GetComponent<Image>();
        bbImg.color = new Color(0.08f, 0.16f, 0.28f, 0.9f);
        Outline bbOutline = bgmBadge.GetComponent<Outline>();
        bbOutline.effectColor = new Color(0.30f, 0.65f, 0.85f, 0.55f);
        bbOutline.effectDistance = new Vector2(1.2f, -1.2f);

        GameObject bgmPctTextObj = new GameObject("VolumePercentText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        bgmPctTextObj.transform.SetParent(bgmBadge.transform, false);
        RectTransform bptRect = bgmPctTextObj.GetComponent<RectTransform>();
        bptRect.anchorMin = Vector2.zero;
        bptRect.anchorMax = Vector2.one;
        bptRect.offsetMin = Vector2.zero;
        bptRect.offsetMax = Vector2.zero;

        TextMeshProUGUI bgmPercentText = bgmPctTextObj.GetComponent<TextMeshProUGUI>();
        bgmPercentText.text = Mathf.RoundToInt((bgmSource != null ? bgmSource.volume : 0.8f) * 100f) + "%";
        if (bodyFont != null) bgmPercentText.font = bodyFont;
        bgmPercentText.fontSize = 17;
        bgmPercentText.fontStyle = FontStyles.Bold;
        bgmPercentText.alignment = TextAlignmentOptions.Center;
        bgmPercentText.color = new Color(0.92f, 0.97f, 1f, 1f);

        // BGM Slider
        Slider bgmSlider;
        CreateStyledSlider(bgmSection.transform, "BgmSlider", new Vector2(0, -36f), 620f, bgmSource != null ? bgmSource.volume : 0.8f, new Color(0.22f, 0.75f, 0.92f, 1f), out bgmSlider);

        // BGM Preset Buttons
        GameObject bgmPresets = new GameObject("PresetRow", typeof(RectTransform));
        bgmPresets.transform.SetParent(bgmSection.transform, false);
        RectTransform bpRect = bgmPresets.GetComponent<RectTransform>();
        bpRect.anchorMin = new Vector2(0.5f, 0f);
        bpRect.anchorMax = new Vector2(0.5f, 0f);
        bpRect.pivot = new Vector2(0.5f, 0f);
        bpRect.anchoredPosition = Vector2.zero;
        bpRect.sizeDelta = new Vector2(620f, 32f);

        Color presetBg = new Color(0.08f, 0.18f, 0.30f, 0.85f);
        Color presetOutline = new Color(0.28f, 0.60f, 0.80f, 0.5f);
        Button bgmMuteBtn = CreatePillButton(bgmPresets.transform, "BgmMuteButton", "Mute", new Vector2(-210f, 0), new Vector2(130f, 32f), presetBg, presetOutline);
        Button bgm50Btn = CreatePillButton(bgmPresets.transform, "Bgm50Button", "50%", new Vector2(0f, 0), new Vector2(130f, 32f), presetBg, presetOutline);
        Button bgm100Btn = CreatePillButton(bgmPresets.transform, "Bgm100Button", "100%", new Vector2(210f, 0), new Vector2(130f, 32f), presetBg, presetOutline);

        // --- 6. Sound Volume Section ---
        GameObject soundSection = new GameObject("SoundSection", typeof(RectTransform));
        soundSection.transform.SetParent(panelObj.transform, false);
        RectTransform soundSecRect = soundSection.GetComponent<RectTransform>();
        soundSecRect.anchorMin = new Vector2(0.5f, 1f);
        soundSecRect.anchorMax = new Vector2(0.5f, 1f);
        soundSecRect.pivot = new Vector2(0.5f, 1f);
        soundSecRect.anchoredPosition = new Vector2(0, -290f);
        soundSecRect.sizeDelta = new Vector2(620f, 108f);

        GameObject soundHeader = new GameObject("HeaderRow", typeof(RectTransform));
        soundHeader.transform.SetParent(soundSection.transform, false);
        RectTransform shRect = soundHeader.GetComponent<RectTransform>();
        shRect.anchorMin = new Vector2(0.5f, 1f);
        shRect.anchorMax = new Vector2(0.5f, 1f);
        shRect.pivot = new Vector2(0.5f, 1f);
        shRect.anchoredPosition = Vector2.zero;
        shRect.sizeDelta = new Vector2(620f, 30f);

        GameObject soundLabelObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        soundLabelObj.transform.SetParent(soundHeader.transform, false);
        RectTransform slRect = soundLabelObj.GetComponent<RectTransform>();
        slRect.anchorMin = new Vector2(0f, 0.5f);
        slRect.anchorMax = new Vector2(0f, 0.5f);
        slRect.pivot = new Vector2(0f, 0.5f);
        slRect.anchoredPosition = Vector2.zero;
        slRect.sizeDelta = new Vector2(400f, 30f);

        TextMeshProUGUI soundLabelTmp = soundLabelObj.GetComponent<TextMeshProUGUI>();
        soundLabelTmp.text = "\uD83D\uDD0A  Sound Volume";
        if (bodyFont != null) soundLabelTmp.font = bodyFont;
        soundLabelTmp.fontSize = 20;
        soundLabelTmp.fontStyle = FontStyles.Bold;
        soundLabelTmp.alignment = TextAlignmentOptions.MidlineLeft;
        soundLabelTmp.color = new Color(0.85f, 0.93f, 1f, 1f);

        // Right Badge Box for Sound
        GameObject soundBadge = new GameObject("PercentBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        soundBadge.transform.SetParent(soundHeader.transform, false);
        RectTransform sbRect = soundBadge.GetComponent<RectTransform>();
        sbRect.anchorMin = new Vector2(1f, 0.5f);
        sbRect.anchorMax = new Vector2(1f, 0.5f);
        sbRect.pivot = new Vector2(1f, 0.5f);
        sbRect.anchoredPosition = Vector2.zero;
        sbRect.sizeDelta = new Vector2(74f, 28f);

        Image sbImg = soundBadge.GetComponent<Image>();
        sbImg.color = new Color(0.08f, 0.16f, 0.28f, 0.9f);
        Outline sbOutline = soundBadge.GetComponent<Outline>();
        sbOutline.effectColor = new Color(0.30f, 0.65f, 0.85f, 0.55f);
        sbOutline.effectDistance = new Vector2(1.2f, -1.2f);

        GameObject soundPctTextObj = new GameObject("SoundPercentText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        soundPctTextObj.transform.SetParent(soundBadge.transform, false);
        RectTransform sptRect = soundPctTextObj.GetComponent<RectTransform>();
        sptRect.anchorMin = Vector2.zero;
        sptRect.anchorMax = Vector2.one;
        sptRect.offsetMin = Vector2.zero;
        sptRect.offsetMax = Vector2.zero;

        float savedSoundVol = PlayerPrefs.GetFloat("SFX_Volume", 0.8f);
        TextMeshProUGUI soundPercentText = soundPctTextObj.GetComponent<TextMeshProUGUI>();
        soundPercentText.text = Mathf.RoundToInt(savedSoundVol * 100f) + "%";
        if (bodyFont != null) soundPercentText.font = bodyFont;
        soundPercentText.fontSize = 17;
        soundPercentText.fontStyle = FontStyles.Bold;
        soundPercentText.alignment = TextAlignmentOptions.Center;
        soundPercentText.color = new Color(0.92f, 0.97f, 1f, 1f);

        // Sound Slider
        Slider soundSlider;
        CreateStyledSlider(soundSection.transform, "SoundSlider", new Vector2(0, -36f), 620f, savedSoundVol, new Color(0.25f, 0.80f, 0.95f, 1f), out soundSlider);

        // Sound Preset Buttons
        GameObject soundPresets = new GameObject("PresetRow", typeof(RectTransform));
        soundPresets.transform.SetParent(soundSection.transform, false);
        RectTransform spRect = soundPresets.GetComponent<RectTransform>();
        spRect.anchorMin = new Vector2(0.5f, 0f);
        spRect.anchorMax = new Vector2(0.5f, 0f);
        spRect.pivot = new Vector2(0.5f, 0f);
        spRect.anchoredPosition = Vector2.zero;
        spRect.sizeDelta = new Vector2(620f, 32f);

        Button soundMuteBtn = CreatePillButton(soundPresets.transform, "SoundMuteButton", "Mute", new Vector2(-210f, 0), new Vector2(130f, 32f), presetBg, presetOutline);
        Button sound50Btn = CreatePillButton(soundPresets.transform, "Sound50Button", "50%", new Vector2(0f, 0), new Vector2(130f, 32f), presetBg, presetOutline);
        Button sound100Btn = CreatePillButton(soundPresets.transform, "Sound100Button", "100%", new Vector2(210f, 0), new Vector2(130f, 32f), presetBg, presetOutline);

        // --- 7. Divider Line ---
        GameObject bottomDiv = new GameObject("BottomDivider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bottomDiv.transform.SetParent(panelObj.transform, false);
        RectTransform bdRect = bottomDiv.GetComponent<RectTransform>();
        bdRect.anchorMin = new Vector2(0.5f, 1f);
        bdRect.anchorMax = new Vector2(0.5f, 1f);
        bdRect.pivot = new Vector2(0.5f, 1f);
        bdRect.anchoredPosition = new Vector2(0, -410f);
        bdRect.sizeDelta = new Vector2(620f, 1.5f);
        bottomDiv.GetComponent<Image>().color = divLineColor;

        // --- 8. Action Buttons (Save Progress & Return to Menu) ---
        GameObject actionsRow = new GameObject("ActionsRow", typeof(RectTransform));
        actionsRow.transform.SetParent(panelObj.transform, false);
        RectTransform arRect = actionsRow.GetComponent<RectTransform>();
        arRect.anchorMin = new Vector2(0.5f, 1f);
        arRect.anchorMax = new Vector2(0.5f, 1f);
        arRect.pivot = new Vector2(0.5f, 1f);
        arRect.anchoredPosition = new Vector2(0, -426f);
        arRect.sizeDelta = new Vector2(620f, 44f);

        Color saveBtnBg = new Color(0.08f, 0.22f, 0.34f, 0.85f);
        Color saveBtnOutline = new Color(0.30f, 0.70f, 0.88f, 0.65f);
        Button saveProgressBtn = CreatePillButton(actionsRow.transform, "SaveProgressButton", "\uD83D\uDCC4  Save Progress", new Vector2(-160f, 0), new Vector2(295f, 42f), saveBtnBg, saveBtnOutline, 18);

        Color menuBtnBg = new Color(0.12f, 0.18f, 0.30f, 0.85f);
        Color menuBtnOutline = new Color(0.30f, 0.70f, 0.88f, 0.65f);
        Button returnToMenuBtn = CreatePillButton(actionsRow.transform, "ReturnToMenuButton", "\u2302  Return to Menu", new Vector2(160f, 0), new Vector2(295f, 42f), menuBtnBg, menuBtnOutline, 18);

        // --- 9. Primary Hero Button ("Resume") ---
        Color heroBg = new Color(0.14f, 0.42f, 0.56f, 0.95f);
        Color heroOutline = new Color(0.38f, 0.85f, 1f, 0.85f);
        Button resumeBtn = CreatePillButton(panelObj.transform, "BackButton", "\u25B7  Resume", new Vector2(0, -480f), new Vector2(620f, 46f), heroBg, heroOutline, 22);
        RectTransform resumeRect = resumeBtn.GetComponent<RectTransform>();
        resumeRect.anchorMin = new Vector2(0.5f, 1f);
        resumeRect.anchorMax = new Vector2(0.5f, 1f);
        resumeRect.pivot = new Vector2(0.5f, 1f);
        resumeRect.anchoredPosition = new Vector2(0, -480f);

        // --- 10. Footer Text ---
        GameObject footerObj = new GameObject("SettingsFooter", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        footerObj.transform.SetParent(panelObj.transform, false);
        RectTransform footRect = footerObj.GetComponent<RectTransform>();
        footRect.anchorMin = new Vector2(0.5f, 1f);
        footRect.anchorMax = new Vector2(0.5f, 1f);
        footRect.pivot = new Vector2(0.5f, 1f);
        footRect.anchoredPosition = new Vector2(0, -538f);
        footRect.sizeDelta = new Vector2(620f, 22f);

        TextMeshProUGUI footTmp = footerObj.GetComponent<TextMeshProUGUI>();
        footTmp.text = "Your story is paused. Stay as long as you need.";
        if (bodyFont != null) footTmp.font = bodyFont;
        footTmp.fontSize = 15;
        footTmp.fontStyle = FontStyles.Italic;
        footTmp.alignment = TextAlignmentOptions.Center;
        footTmp.color = new Color(0.55f, 0.70f, 0.85f, 0.75f);

        // --- 11. Wire Persistent Listeners ---
        UnityEventTools.AddPersistentListener(bgmMuteBtn.onClick, settingsMenu.MuteBGM);
        UnityEventTools.AddPersistentListener(bgm50Btn.onClick, settingsMenu.SetBGM50);
        UnityEventTools.AddPersistentListener(bgm100Btn.onClick, settingsMenu.SetBGM100);

        UnityEventTools.AddPersistentListener(soundMuteBtn.onClick, settingsMenu.MuteSound);
        UnityEventTools.AddPersistentListener(sound50Btn.onClick, settingsMenu.SetSound50);
        UnityEventTools.AddPersistentListener(sound100Btn.onClick, settingsMenu.SetSound100);

        UnityEventTools.AddPersistentListener(saveProgressBtn.onClick, settingsMenu.OnSaveProgressClicked);
        UnityEventTools.AddPersistentListener(returnToMenuBtn.onClick, settingsMenu.OnReturnToMenuClicked);
        UnityEventTools.AddPersistentListener(resumeBtn.onClick, settingsMenu.CloseSettings);

        // --- 12. Wire SerializedProperties on SettingsMenu ---
        SerializedObject so = new SerializedObject(settingsMenu);
        so.Update();
        so.FindProperty("bgmAudioSource").objectReferenceValue = bgmSource;
        so.FindProperty("bgmSlider").objectReferenceValue = bgmSlider;
        so.FindProperty("volumePercentText").objectReferenceValue = bgmPercentText;
        so.FindProperty("bgmMuteButton").objectReferenceValue = bgmMuteBtn;
        so.FindProperty("bgm50Button").objectReferenceValue = bgm50Btn;
        so.FindProperty("bgm100Button").objectReferenceValue = bgm100Btn;

        so.FindProperty("soundAudioSource").objectReferenceValue = panelAudio;
        so.FindProperty("soundSlider").objectReferenceValue = soundSlider;
        so.FindProperty("soundPercentText").objectReferenceValue = soundPercentText;
        so.FindProperty("soundMuteButton").objectReferenceValue = soundMuteBtn;
        so.FindProperty("sound50Button").objectReferenceValue = sound50Btn;
        so.FindProperty("sound100Button").objectReferenceValue = sound100Btn;
        so.FindProperty("previewClip").objectReferenceValue = clickClip;

        so.FindProperty("saveProgressButton").objectReferenceValue = saveProgressBtn;
        so.FindProperty("returnToMenuButton").objectReferenceValue = returnToMenuBtn;
        so.FindProperty("resumeButton").objectReferenceValue = resumeBtn;
        so.FindProperty("settingsPanel").objectReferenceValue = panelObj;
        so.FindProperty("settingsCanvasGroup").objectReferenceValue = panelCg;
        so.ApplyModifiedProperties();

        panelObj.SetActive(false);
        EditorUtility.SetDirty(panelObj);

        return settingsMenu;
    }

    public static void SetupStartButtonInMenu()
    {
        string currentScene = EditorSceneManager.GetActiveScene().path;
        if (currentScene != MENU_SCENE_PATH)
        {
            if (File.Exists(MENU_SCENE_PATH))
            {
                EditorSceneManager.OpenScene(MENU_SCENE_PATH, OpenSceneMode.Single);
            }
            else
            {
                return;
            }
        }

        // Remove any leftover legacy "Button" Text objects in MenuScreen
        Text[] legacyTexts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include);
        foreach (Text t in legacyTexts)
        {
            if (t != null && (t.text == "Button" || t.gameObject.name.Contains("Text (Legacy)")))
            {
                Object.DestroyImmediate(t.gameObject);
            }
        }

        GameObject startBtnObj = GameObject.Find("startbutton");
        if (startBtnObj == null)
        {
            Debug.LogWarning("[SchoolSceneBuilder] 'startbutton' not found in MenuScreen!");
            return;
        }

        StartGameButton startComp = startBtnObj.GetComponent<StartGameButton>();
        if (startComp == null)
        {
            startComp = startBtnObj.AddComponent<StartGameButton>();
        }

        Button btn = startBtnObj.GetComponent<Button>();
        if (btn != null)
        {
            UnityEventTools.RemovePersistentListener(btn.onClick, startComp.OnStartClicked);
            UnityEventTools.AddPersistentListener(btn.onClick, startComp.OnStartClicked);
            EditorUtility.SetDirty(btn);
        }

        EditorUtility.SetDirty(startBtnObj);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    public static void RegisterSceneInBuildSettings()
    {
        var existingScenes = EditorBuildSettings.scenes;
        bool menuFound = false;
        bool schoolFound = false;

        foreach (var s in existingScenes)
        {
            if (s.path == MENU_SCENE_PATH) menuFound = true;
            if (s.path == SCENE_PATH) schoolFound = true;
        }

        if (!schoolFound || !menuFound)
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(existingScenes);
            if (!menuFound && File.Exists(MENU_SCENE_PATH))
            {
                list.Insert(0, new EditorBuildSettingsScene(MENU_SCENE_PATH, true));
            }
            if (!schoolFound && File.Exists(SCENE_PATH))
            {
                list.Add(new EditorBuildSettingsScene(SCENE_PATH, true));
            }
            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log("<color=green>[SchoolSceneBuilder] Registered scenes in EditorBuildSettings!</color>");
        }
    }
}
#endif