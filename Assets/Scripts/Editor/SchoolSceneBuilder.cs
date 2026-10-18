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

    static SchoolSceneBuilder()
    {
        EditorApplication.update += RunOnce;
    }

    private static void RunOnce()
    {
        EditorApplication.update -= RunOnce;
        CheckAndBuild();
    }

    [InitializeOnLoadMethod]
    public static void CheckAndBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        if (!File.Exists(SCENE_PATH))
        {
            BuildSchoolScene();
        }

        SetupStartButtonInMenu();
        RegisterSceneInBuildSettings();
    }

    [MenuItem("Tools/Build School Scene & Setup Start Button")]
    public static void ManualBuild()
    {
        BuildSchoolScene();
        SetupStartButtonInMenu();
        RegisterSceneInBuildSettings();
        Debug.Log("<color=green>[SchoolSceneBuilder] Build completed successfully!</color>");
    }

    public static void BuildSchoolScene()
    {
        // Import the newly added texture first
        AssetDatabase.ImportAsset("Assets/Art/cherry_blossom_school.png", ImportAssetOptions.ForceUpdate);
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/cherry_blossom_school.png");
        if (bgSprite == null)
        {
            bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/cherry_blossom_school.png");
        }

        TMP_FontAsset menuFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Buttons/Fonts/BLKCHCRY SDF.asset");
        if (menuFont == null) menuFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        // Remember active scene to return to it
        string activeScenePath = EditorSceneManager.GetActiveScene().path;

        // Create new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Main Camera
        GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        Camera cam = camObj.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.05f, 0.12f, 1f);
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

        // 6. Header Badge (Prologue Title)
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

        // 7. Back to Menu Button (Top Left)
        DefaultControls.Resources uiRes = new DefaultControls.Resources();
        GameObject backBtnObj = DefaultControls.CreateButton(uiRes);
        backBtnObj.name = "BackToMenuButton";
        backBtnObj.transform.SetParent(canvasObj.transform, false);
        RectTransform backRect = backBtnObj.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.03f, 0.90f);
        backRect.anchorMax = new Vector2(0.16f, 0.97f);
        backRect.offsetMin = Vector2.zero;
        backRect.offsetMax = Vector2.zero;

        Image backImg = backBtnObj.GetComponent<Image>();
        backImg.color = new Color(0.04f, 0.10f, 0.22f, 0.85f);

        Transform defaultBackText = backBtnObj.transform.Find("Text");
        if (defaultBackText != null) Object.DestroyImmediate(defaultBackText.gameObject);

        GameObject backTextObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        backTextObj.transform.SetParent(backBtnObj.transform, false);
        RectTransform btRect = backTextObj.GetComponent<RectTransform>();
        btRect.anchorMin = Vector2.zero;
        btRect.anchorMax = Vector2.one;
        btRect.offsetMin = Vector2.zero;
        btRect.offsetMax = Vector2.zero;

        TextMeshProUGUI btTmp = backTextObj.GetComponent<TextMeshProUGUI>();
        btTmp.text = "Return to Menu";
        btTmp.font = menuFont;
        btTmp.fontSize = 20;
        btTmp.fontStyle = FontStyles.Bold;
        btTmp.alignment = TextAlignmentOptions.Center;
        btTmp.color = new Color(0.75f, 0.92f, 1f, 1f);

        // 8. Full-screen Advance Click Overlay (invisible)
        GameObject advanceObj = DefaultControls.CreateButton(uiRes);
        advanceObj.name = "AdvanceTrigger";
        advanceObj.transform.SetParent(canvasObj.transform, false);
        RectTransform advRect = advanceObj.GetComponent<RectTransform>();
        advRect.anchorMin = Vector2.zero;
        advRect.anchorMax = Vector2.one;
        advRect.offsetMin = Vector2.zero;
        advRect.offsetMax = Vector2.zero;

        Image advImg = advanceObj.GetComponent<Image>();
        advImg.color = new Color(0, 0, 0, 0); // completely invisible
        Transform advText = advanceObj.transform.Find("Text");
        if (advText != null) Object.DestroyImmediate(advText.gameObject);

        // 9. Visual Novel Dialogue Box at Bottom
        GameObject boxObj = new GameObject("DialogueBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        boxObj.transform.SetParent(canvasObj.transform, false);
        RectTransform boxRect = boxObj.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.12f, 0.05f);
        boxRect.anchorMax = new Vector2(0.88f, 0.28f);
        boxRect.offsetMin = Vector2.zero;
        boxRect.offsetMax = Vector2.zero;

        Image boxImg = boxObj.GetComponent<Image>();
        boxImg.color = new Color(0.03f, 0.08f, 0.18f, 0.88f);

        // Speaker Name Badge
        GameObject speakerObj = new GameObject("SpeakerName", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        speakerObj.transform.SetParent(boxObj.transform, false);
        RectTransform spRect = speakerObj.GetComponent<RectTransform>();
        spRect.anchorMin = new Vector2(0.04f, 0.72f);
        spRect.anchorMax = new Vector2(0.35f, 0.96f);
        spRect.offsetMin = Vector2.zero;
        spRect.offsetMax = Vector2.zero;

        TextMeshProUGUI spTmp = speakerObj.GetComponent<TextMeshProUGUI>();
        spTmp.text = "<b>Narrator</b>";
        spTmp.font = menuFont;
        spTmp.fontSize = 28;
        spTmp.alignment = TextAlignmentOptions.Left;
        spTmp.color = new Color(0.35f, 0.85f, 1f, 1f);

        // Dialogue Text
        GameObject diaTextObj = new GameObject("DialogueText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        diaTextObj.transform.SetParent(boxObj.transform, false);
        RectTransform diaRect = diaTextObj.GetComponent<RectTransform>();
        diaRect.anchorMin = new Vector2(0.04f, 0.12f);
        diaRect.anchorMax = new Vector2(0.96f, 0.70f);
        diaRect.offsetMin = Vector2.zero;
        diaRect.offsetMax = Vector2.zero;

        TextMeshProUGUI diaTmp = diaTextObj.GetComponent<TextMeshProUGUI>();
        diaTmp.text = "The gentle morning breeze carries the sweet scent of spring cherry blossoms through the air...";
        diaTmp.font = menuFont;
        diaTmp.fontSize = 26;
        diaTmp.lineSpacing = 16f;
        diaTmp.color = new Color(0.96f, 0.98f, 1f, 1f);

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

        // 10. SchoolPrologueController Component
        SchoolPrologueController controller = canvasObj.AddComponent<SchoolPrologueController>();
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("speakerNameText").objectReferenceValue = spTmp;
        so.FindProperty("dialogueText").objectReferenceValue = diaTmp;
        so.FindProperty("chapterBadgeText").objectReferenceValue = badgeTmp;
        so.FindProperty("nextButton").objectReferenceValue = advanceObj.GetComponent<Button>();
        so.FindProperty("backToMenuButton").objectReferenceValue = backBtnObj.GetComponent<Button>();
        so.FindProperty("dialoguePanel").objectReferenceValue = boxObj;
        so.ApplyModifiedProperties();

        // 11. Save SchoolScene
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        Debug.Log($"<color=pink>[SchoolSceneBuilder] Created and saved {SCENE_PATH}!</color>");

        // Return to previously active scene if different
        if (!string.IsNullOrEmpty(activeScenePath) && File.Exists(activeScenePath))
        {
            EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);
        }
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
        Debug.Log("<color=cyan>[SchoolSceneBuilder] Successfully wired startbutton to transition to SchoolScene!</color>");
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
