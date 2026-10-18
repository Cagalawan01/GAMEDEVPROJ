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
    private const string SESSION_BUILD_KEY = "SchoolScene_Built_V5";

    static SchoolSceneBuilder()
    {
        EditorApplication.delayCall += CheckAndBuild;
    }

    public static void CheckAndBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        bool needsRebuild = !File.Exists(SCENE_PATH) || !SessionState.GetBool(SESSION_BUILD_KEY, false);
        if (needsRebuild)
        {
            SessionState.SetBool(SESSION_BUILD_KEY, true);
            BuildSchoolScene();
        }

        if (EditorSceneManager.GetActiveScene().path == MENU_SCENE_PATH)
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

        TMP_FontAsset menuFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Buttons/Fonts/BLKCHCRY SDF.asset");
        if (menuFont == null) menuFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        AudioClip clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Sound Effect/click.mp3");
        AudioClip hoverClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Sound Effect/hover.mp3");
        AudioClip bgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM/Menu.mp3");

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

        // 3. BGM AudioSource
        GameObject bgmObj = new GameObject("BGM", typeof(AudioSource));
        AudioSource bgmSource = bgmObj.GetComponent<AudioSource>();
        bgmSource.clip = bgmClip;
        bgmSource.loop = true;
        bgmSource.playOnAwake = true;
        bgmSource.volume = PlayerPrefs.GetFloat("BGM_Volume", 0.8f);

        // 4. Canvas
        GameObject canvasObj = new GameObject("SchoolCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // 5. Background Image (Cherry Blossom School)
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

        // 6. Floating Sakura Petals Overlay
        GameObject petalsObj = new GameObject("SakuraPetalsOverlay", typeof(RectTransform), typeof(SakuraPetalsOverlay));
        petalsObj.transform.SetParent(canvasObj.transform, false);
        RectTransform petalsRect = petalsObj.GetComponent<RectTransform>();
        petalsRect.anchorMin = Vector2.zero;
        petalsRect.anchorMax = Vector2.one;
        petalsRect.offsetMin = Vector2.zero;
        petalsRect.offsetMax = Vector2.zero;

        // 7. Header Badge (Prologue Title)
        GameObject badgeObj = new GameObject("HeaderBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        badgeObj.transform.SetParent(canvasObj.transform, false);
        RectTransform badgeRect = badgeObj.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0.2f, 0.90f);
        badgeRect.anchorMax = new Vector2(0.8f, 0.98f);
        badgeRect.offsetMin = Vector2.zero;
        badgeRect.offsetMax = Vector2.zero;

        TextMeshProUGUI badgeTmp = badgeObj.GetComponent<TextMeshProUGUI>();
        badgeTmp.text = "PROLOGUE  \u2014  FIRST DAY OF SCHOOL";
        badgeTmp.font = menuFont;
        badgeTmp.fontSize = 32;
        badgeTmp.fontStyle = FontStyles.Bold;
        badgeTmp.alignment = TextAlignmentOptions.Center;
        badgeTmp.color = new Color(1f, 0.95f, 0.98f, 0.95f);

        DefaultControls.Resources uiRes = new DefaultControls.Resources();

        // 8. Full-screen Advance Click Overlay (placed BEFORE DialogueBox & Settings Icon so UI buttons sit on top)
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
        Transform advText = advanceObj.transform.Find("Text");
        if (advText != null) Object.DestroyImmediate(advText.gameObject);

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
        spTmp.text = "<b>Narrator</b>";
        spTmp.font = menuFont;
        spTmp.fontSize = 28;
        spTmp.alignment = TextAlignmentOptions.Center;
        spTmp.color = new Color(0.93f, 0.98f, 1f, 1f);
        spTmp.raycastTarget = false;

        // Dialogue Text inside DialogueBox
        GameObject diaTextObj = new GameObject("DialogueText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        diaTextObj.transform.SetParent(boxObj.transform, false);
        RectTransform diaRect = diaTextObj.GetComponent<RectTransform>();
        diaRect.anchorMin = new Vector2(0.045f, 0.14f);
        diaRect.anchorMax = new Vector2(0.955f, 0.74f);
        diaRect.offsetMin = Vector2.zero;
        diaRect.offsetMax = Vector2.zero;

        TextMeshProUGUI diaTmp = diaTextObj.GetComponent<TextMeshProUGUI>();
        diaTmp.text = "The gentle morning breeze carries the sweet scent of spring cherry blossoms through the air...";
        diaTmp.font = menuFont;
        diaTmp.fontSize = 26;
        diaTmp.lineSpacing = 16f;
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

        Transform defaultSettingsText = settingsBtnObj.transform.Find("Text");
        if (defaultSettingsText != null) Object.DestroyImmediate(defaultSettingsText.gameObject);

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

        SerializedObject so = new SerializedObject(controller);
        so.Update();
        so.FindProperty("speakerPlateObject").objectReferenceValue = speakerPlateObj;
        so.FindProperty("speakerNameText").objectReferenceValue = spTmp;
        so.FindProperty("dialogueText").objectReferenceValue = diaTmp;
        so.FindProperty("chapterBadgeText").objectReferenceValue = badgeTmp;
        so.FindProperty("nextButton").objectReferenceValue = advanceObj.GetComponent<Button>();
        so.FindProperty("settingsIconButton").objectReferenceValue = settingsIconBtn;
        so.FindProperty("backToMenuButton").objectReferenceValue = settingsIconBtn;
        so.FindProperty("dialoguePanel").objectReferenceValue = boxObj;
        so.FindProperty("settingsMenu").objectReferenceValue = settingsMenu;
        so.FindProperty("saveArchiveMenu").objectReferenceValue = saveArchiveMenu;
        so.FindProperty("audioSource").objectReferenceValue = ctrlAudio;
        so.FindProperty("clickClip").objectReferenceValue = clickClip;
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
        panelObj = new GameObject("SettingsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(920f, 760f);

        Image panelImage = panelObj.GetComponent<Image>();
        panelImage.color = new Color(0.03f, 0.13f, 0.19f, 0.96f);

        Outline panelOutline = panelObj.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.32f, 0.78f, 0.88f, 0.75f);
        panelOutline.effectDistance = new Vector2(2f, -2f);

        CanvasGroup panelCg = panelObj.GetComponent<CanvasGroup>();
        panelCg.alpha = 0f;
        panelCg.interactable = false;
        panelCg.blocksRaycasts = false;

        AudioSource panelAudio = panelObj.AddComponent<AudioSource>();
        panelAudio.playOnAwake = false;

        SettingsMenu settingsMenu = panelObj.AddComponent<SettingsMenu>();
        DefaultControls.Resources uiResources = new DefaultControls.Resources();

        // 1. Title Text ("SETTINGS")
        GameObject titleObj = new GameObject("SettingsTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(panelObj.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -28f);
        titleRect.sizeDelta = new Vector2(600f, 75f);

        TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
        titleText.text = "SETTINGS";
        if (fontAsset != null) titleText.font = fontAsset;
        titleText.fontSize = 64;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0.92f, 0.98f, 1f, 1f);

        // 2. BGM Volume Section
        GameObject bgmRow = new GameObject("VolumeRow", typeof(RectTransform));
        bgmRow.transform.SetParent(panelObj.transform, false);
        RectTransform bgmRowRect = bgmRow.GetComponent<RectTransform>();
        bgmRowRect.anchorMin = new Vector2(0.5f, 1f);
        bgmRowRect.anchorMax = new Vector2(0.5f, 1f);
        bgmRowRect.pivot = new Vector2(0.5f, 1f);
        bgmRowRect.anchoredPosition = new Vector2(0, -115f);
        bgmRowRect.sizeDelta = new Vector2(800f, 110f);

        GameObject bgmLabelObj = new GameObject("VolumeLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        bgmLabelObj.transform.SetParent(bgmRow.transform, false);
        RectTransform bgmLabelRect = bgmLabelObj.GetComponent<RectTransform>();
        bgmLabelRect.anchorMin = new Vector2(0.5f, 1f);
        bgmLabelRect.anchorMax = new Vector2(0.5f, 1f);
        bgmLabelRect.pivot = new Vector2(0.5f, 1f);
        bgmLabelRect.anchoredPosition = Vector2.zero;
        bgmLabelRect.sizeDelta = new Vector2(500f, 42f);

        TextMeshProUGUI bgmLabelText = bgmLabelObj.GetComponent<TextMeshProUGUI>();
        bgmLabelText.text = "BGM Volume";
        if (fontAsset != null) bgmLabelText.font = fontAsset;
        bgmLabelText.fontSize = 38;
        bgmLabelText.alignment = TextAlignmentOptions.Center;
        bgmLabelText.color = new Color(0.85f, 0.94f, 1f, 1f);

        GameObject bgmSliderObj = DefaultControls.CreateSlider(uiResources);
        bgmSliderObj.name = "BgmSlider";
        bgmSliderObj.transform.SetParent(bgmRow.transform, false);
        RectTransform bgmSliderRect = bgmSliderObj.GetComponent<RectTransform>();
        bgmSliderRect.anchorMin = new Vector2(0.5f, 0f);
        bgmSliderRect.anchorMax = new Vector2(0.5f, 0f);
        bgmSliderRect.pivot = new Vector2(0.5f, 0f);
        bgmSliderRect.anchoredPosition = new Vector2(-50f, 14f);
        bgmSliderRect.sizeDelta = new Vector2(480f, 30f);

        Slider bgmSlider = bgmSliderObj.GetComponent<Slider>();
        bgmSlider.minValue = 0f;
        bgmSlider.maxValue = 1f;
        bgmSlider.value = bgmSource != null ? bgmSource.volume : 0.8f;

        Image bgmFillImage = bgmSlider.fillRect != null ? bgmSlider.fillRect.GetComponent<Image>() : null;
        if (bgmFillImage != null) bgmFillImage.color = new Color(0.22f, 0.72f, 0.88f, 1f);

        Image bgmHandleImage = bgmSlider.handleRect != null ? bgmSlider.handleRect.GetComponent<Image>() : null;
        if (bgmHandleImage != null) bgmHandleImage.color = new Color(0.95f, 0.98f, 1f, 1f);

        GameObject bgmPercentObj = new GameObject("VolumePercentText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        bgmPercentObj.transform.SetParent(bgmRow.transform, false);
        RectTransform bgmPercentRect = bgmPercentObj.GetComponent<RectTransform>();
        bgmPercentRect.anchorMin = new Vector2(0.5f, 0f);
        bgmPercentRect.anchorMax = new Vector2(0.5f, 0f);
        bgmPercentRect.pivot = new Vector2(0f, 0.5f);
        bgmPercentRect.anchoredPosition = new Vector2(215f, 29f);
        bgmPercentRect.sizeDelta = new Vector2(120f, 40f);

        TextMeshProUGUI bgmPercentText = bgmPercentObj.GetComponent<TextMeshProUGUI>();
        bgmPercentText.text = Mathf.RoundToInt(bgmSlider.value * 100f) + "%";
        if (fontAsset != null) bgmPercentText.font = fontAsset;
        bgmPercentText.fontSize = 36;
        bgmPercentText.alignment = TextAlignmentOptions.MidlineLeft;
        bgmPercentText.color = new Color(0.95f, 0.98f, 1f, 1f);

        // 3. Sound Volume Section
        GameObject soundRow = new GameObject("SoundRow", typeof(RectTransform));
        soundRow.transform.SetParent(panelObj.transform, false);
        RectTransform soundRowRect = soundRow.GetComponent<RectTransform>();
        soundRowRect.anchorMin = new Vector2(0.5f, 1f);
        soundRowRect.anchorMax = new Vector2(0.5f, 1f);
        soundRowRect.pivot = new Vector2(0.5f, 1f);
        soundRowRect.anchoredPosition = new Vector2(0, -240f);
        soundRowRect.sizeDelta = new Vector2(800f, 195f);

        GameObject soundLabelObj = new GameObject("SoundLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        soundLabelObj.transform.SetParent(soundRow.transform, false);
        RectTransform soundLabelRect = soundLabelObj.GetComponent<RectTransform>();
        soundLabelRect.anchorMin = new Vector2(0.5f, 1f);
        soundLabelRect.anchorMax = new Vector2(0.5f, 1f);
        soundLabelRect.pivot = new Vector2(0.5f, 1f);
        soundLabelRect.anchoredPosition = Vector2.zero;
        soundLabelRect.sizeDelta = new Vector2(500f, 42f);

        TextMeshProUGUI soundLabelText = soundLabelObj.GetComponent<TextMeshProUGUI>();
        soundLabelText.text = "Sound Volume";
        if (fontAsset != null) soundLabelText.font = fontAsset;
        soundLabelText.fontSize = 38;
        soundLabelText.alignment = TextAlignmentOptions.Center;
        soundLabelText.color = new Color(0.85f, 0.94f, 1f, 1f);

        Button CreateStyledButton(Transform parent, string btnName, string label, Vector2 anchoredPos, Vector2 size, Color bgColor, int fontSize = 34)
        {
            GameObject btnObj = DefaultControls.CreateButton(uiResources);
            btnObj.name = btnName;
            btnObj.transform.SetParent(parent, false);
            RectTransform bRect = btnObj.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.5f, 0f);
            bRect.anchorMax = new Vector2(0.5f, 0f);
            bRect.pivot = new Vector2(0.5f, 0.5f);
            bRect.anchoredPosition = anchoredPos;
            bRect.sizeDelta = size;

            Image bImg = btnObj.GetComponent<Image>();
            bImg.color = bgColor;

            Transform oldText = btnObj.transform.Find("Text");
            if (oldText != null) Object.DestroyImmediate(oldText.gameObject);

            GameObject textObj = new GameObject("Text (TMP)", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform tRect = textObj.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;
            tRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            if (fontAsset != null) tmp.font = fontAsset;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            Button btn = btnObj.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.22f, 1.32f, 1.45f, 1f);
            cb.pressedColor = new Color(0.75f, 0.88f, 1f, 1f);
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

        Color subBtnColor = new Color(0.09f, 0.25f, 0.36f, 0.92f);
        Button soundMinusBtn = CreateStyledButton(soundRow.transform, "SoundMinusButton", "\u2014", new Vector2(-235f, 88f), new Vector2(48f, 40f), subBtnColor, 28);

        GameObject soundSliderObj = DefaultControls.CreateSlider(uiResources);
        soundSliderObj.name = "SoundSlider";
        soundSliderObj.transform.SetParent(soundRow.transform, false);
        RectTransform soundSliderRect = soundSliderObj.GetComponent<RectTransform>();
        soundSliderRect.anchorMin = new Vector2(0.5f, 0f);
        soundSliderRect.anchorMax = new Vector2(0.5f, 0f);
        soundSliderRect.pivot = new Vector2(0.5f, 0.5f);
        soundSliderRect.anchoredPosition = new Vector2(-20f, 88f);
        soundSliderRect.sizeDelta = new Vector2(350f, 30f);

        Slider soundSlider = soundSliderObj.GetComponent<Slider>();
        soundSlider.minValue = 0f;
        soundSlider.maxValue = 1f;
        float savedSoundVol = PlayerPrefs.GetFloat("SFX_Volume", 0.8f);
        soundSlider.value = savedSoundVol;

        Image soundFillImage = soundSlider.fillRect != null ? soundSlider.fillRect.GetComponent<Image>() : null;
        if (soundFillImage != null) soundFillImage.color = new Color(0.25f, 0.78f, 0.92f, 1f);

        Image soundHandleImage = soundSlider.handleRect != null ? soundSlider.handleRect.GetComponent<Image>() : null;
        if (soundHandleImage != null) soundHandleImage.color = new Color(0.95f, 0.98f, 1f, 1f);

        Button soundPlusBtn = CreateStyledButton(soundRow.transform, "SoundPlusButton", "+", new Vector2(195f, 88f), new Vector2(48f, 40f), subBtnColor, 32);

        GameObject soundPercentObj = new GameObject("SoundPercentText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        soundPercentObj.transform.SetParent(soundRow.transform, false);
        RectTransform soundPercentRect = soundPercentObj.GetComponent<RectTransform>();
        soundPercentRect.anchorMin = new Vector2(0.5f, 0f);
        soundPercentRect.anchorMax = new Vector2(0.5f, 0f);
        soundPercentRect.pivot = new Vector2(0f, 0.5f);
        soundPercentRect.anchoredPosition = new Vector2(235f, 88f);
        soundPercentRect.sizeDelta = new Vector2(110f, 40f);

        TextMeshProUGUI soundPercentText = soundPercentObj.GetComponent<TextMeshProUGUI>();
        soundPercentText.text = Mathf.RoundToInt(savedSoundVol * 100f) + "%";
        if (fontAsset != null) soundPercentText.font = fontAsset;
        soundPercentText.fontSize = 36;
        soundPercentText.alignment = TextAlignmentOptions.MidlineLeft;
        soundPercentText.color = new Color(0.95f, 0.98f, 1f, 1f);

        Button soundMuteBtn = CreateStyledButton(soundRow.transform, "SoundMuteButton", "Mute", new Vector2(-160f, 26f), new Vector2(130f, 46f), subBtnColor, 32);
        Button sound50Btn = CreateStyledButton(soundRow.transform, "Sound50Button", "50%", new Vector2(0f, 26f), new Vector2(130f, 46f), subBtnColor, 32);
        Button sound100Btn = CreateStyledButton(soundRow.transform, "Sound100Button", "100%", new Vector2(160f, 26f), new Vector2(130f, 46f), subBtnColor, 32);

        // 4. Save Progress & Return to Menu Buttons
        Color actionBtnColor = new Color(0.08f, 0.34f, 0.44f, 0.95f);
        Color menuBtnColor = new Color(0.14f, 0.24f, 0.40f, 0.95f);

        Button saveProgressBtn = CreateStyledButton(
            panelObj.transform,
            "SaveProgressButton",
            "Save Progress",
            new Vector2(-185f, 175f),
            new Vector2(330f, 68f),
            actionBtnColor,
            38);

        Button returnToMenuBtn = CreateStyledButton(
            panelObj.transform,
            "ReturnToMenuButton",
            "Return to Menu",
            new Vector2(185f, 175f),
            new Vector2(330f, 68f),
            menuBtnColor,
            38);

        // 5. Resume / Back Button
        Button backBtn = CreateStyledButton(
            panelObj.transform,
            "BackButton",
            "Resume",
            new Vector2(0f, 68f),
            new Vector2(260f, 62f),
            new Color(0.16f, 0.30f, 0.48f, 0.92f),
            38);

        // Wire persistent listeners
        UnityEventTools.AddPersistentListener(soundMuteBtn.onClick, settingsMenu.MuteSound);
        UnityEventTools.AddPersistentListener(sound50Btn.onClick, settingsMenu.SetSound50);
        UnityEventTools.AddPersistentListener(sound100Btn.onClick, settingsMenu.SetSound100);
        UnityEventTools.AddFloatPersistentListener(soundMinusBtn.onClick, settingsMenu.ChangeSoundVolumeBy, -0.1f);
        UnityEventTools.AddFloatPersistentListener(soundPlusBtn.onClick, settingsMenu.ChangeSoundVolumeBy, 0.1f);

        UnityEventTools.AddPersistentListener(saveProgressBtn.onClick, settingsMenu.OnSaveProgressClicked);
        UnityEventTools.AddPersistentListener(returnToMenuBtn.onClick, settingsMenu.OnReturnToMenuClicked);
        UnityEventTools.AddPersistentListener(backBtn.onClick, settingsMenu.CloseSettings);

        // Wire SerializedProperties on SettingsMenu
        SerializedObject so = new SerializedObject(settingsMenu);
        so.Update();
        so.FindProperty("bgmAudioSource").objectReferenceValue = bgmSource;
        so.FindProperty("bgmSlider").objectReferenceValue = bgmSlider;
        so.FindProperty("volumePercentText").objectReferenceValue = bgmPercentText;

        so.FindProperty("soundAudioSource").objectReferenceValue = panelAudio;
        so.FindProperty("soundSlider").objectReferenceValue = soundSlider;
        so.FindProperty("soundPercentText").objectReferenceValue = soundPercentText;
        so.FindProperty("soundMinusButton").objectReferenceValue = soundMinusBtn;
        so.FindProperty("soundPlusButton").objectReferenceValue = soundPlusBtn;
        so.FindProperty("soundMuteButton").objectReferenceValue = soundMuteBtn;
        so.FindProperty("sound50Button").objectReferenceValue = sound50Btn;
        so.FindProperty("sound100Button").objectReferenceValue = sound100Btn;
        so.FindProperty("previewClip").objectReferenceValue = clickClip;

        so.FindProperty("saveProgressButton").objectReferenceValue = saveProgressBtn;
        so.FindProperty("returnToMenuButton").objectReferenceValue = returnToMenuBtn;
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
