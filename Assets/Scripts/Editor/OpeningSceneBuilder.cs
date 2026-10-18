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
public static class OpeningSceneBuilder
{
    public const string SCENE_PATH = "Assets/Scenes/OpeningScene.unity";
    private const string MENU_SCENE_PATH = "Assets/Scenes/MenuScreen.unity";
    private const string SCHOOL_SCENE_PATH = "Assets/Scenes/SchoolScene.unity";

    static OpeningSceneBuilder()
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

        if (!SessionState.GetBool("OpeningScene_Built_V2", false) || !File.Exists(SCENE_PATH))
        {
            SessionState.SetBool("OpeningScene_Built_V2", true);
            BuildOpeningSceneAndWireMenu();
        }
        else
        {
            RegisterScenesInBuildSettings();
        }
    }

    [MenuItem("Tools/Build Opening Scene (Beat 1 Title Card)")]
    public static void BuildOpeningSceneAndWireMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        // 1. Build and save OpeningScene.unity
        BuildOpeningScene(leaveOpen: false);

        // 2. Open MenuScreen.unity and wire Start button to OpeningScene
        WireStartButtonToOpeningScene();

        // 3. Ensure all scenes (MenuScreen -> OpeningScene -> SchoolScene) are in Build Settings
        RegisterScenesInBuildSettings();

        Debug.Log("<color=cyan>[OpeningSceneBuilder] Flow ready: MenuScreen (Start) -> OpeningScene -> SchoolScene!</color>");
    }

    public static void BuildOpeningScene(bool leaveOpen = true)
    {
        TMP_FontAsset bodyFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        TMP_FontAsset titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Buttons/Fonts/BLKCHCRY SDF.asset");
        if (titleFont == null) titleFont = bodyFont;
        if (bodyFont == null) bodyFont = titleFont;

        Sprite dividerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/titledesign1.png");

        string previousScenePath = EditorSceneManager.GetActiveScene().path;

        // Create new empty scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Main Camera (Pure Black Background)
        GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        Camera cam = camObj.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        camObj.tag = "MainCamera";

        // 2. EventSystem
        GameObject esObj = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        esObj.AddComponent<StandaloneInputModule>();
#endif

        // 3. Canvas (1920x1080)
        GameObject canvasObj = new GameObject("OpeningCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // 4. Pure Black Background Image
        GameObject bgObj = new GameObject("BlackBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgObj.transform.SetParent(canvasObj.transform, false);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        Image bgImg = bgObj.GetComponent<Image>();
        bgImg.color = Color.black;
        bgImg.raycastTarget = false;

        // 5. Narrative Text Container (Beat 1 Lines)
        // Visible in Edit Mode with all 3 lines so inspecting the scene shows the exact script text;
        // at runtime OpeningSceneController fades each line in alone and holds before the next.
        GameObject narrativeGroupObj = new GameObject("NarrativeContainer", typeof(RectTransform), typeof(CanvasGroup));
        narrativeGroupObj.transform.SetParent(canvasObj.transform, false);
        RectTransform narGroupRect = narrativeGroupObj.GetComponent<RectTransform>();
        narGroupRect.anchorMin = new Vector2(0.12f, 0.25f);
        narGroupRect.anchorMax = new Vector2(0.88f, 0.75f);
        narGroupRect.offsetMin = Vector2.zero;
        narGroupRect.offsetMax = Vector2.zero;

        CanvasGroup narrativeCg = narrativeGroupObj.GetComponent<CanvasGroup>();
        narrativeCg.alpha = 1f;
        narrativeCg.interactable = false;
        narrativeCg.blocksRaycasts = false;

        GameObject narrativeTextObj = new GameObject("NarrativeText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        narrativeTextObj.transform.SetParent(narrativeGroupObj.transform, false);
        RectTransform narTextRect = narrativeTextObj.GetComponent<RectTransform>();
        narTextRect.anchorMin = Vector2.zero;
        narTextRect.anchorMax = Vector2.one;
        narTextRect.offsetMin = Vector2.zero;
        narTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI narTmp = narrativeTextObj.GetComponent<TextMeshProUGUI>();
        narTmp.text = "October 14th.\n\nOne year ago today, a girl named Elara Veyne was last seen alive.\n\nNobody agrees on what happened after that.";
        narTmp.font = bodyFont;
        narTmp.fontSize = 34;
        narTmp.lineSpacing = 14f;
        narTmp.alignment = TextAlignmentOptions.Center;
        narTmp.color = new Color(0.92f, 0.94f, 0.98f, 1f);
        narTmp.raycastTarget = false;

        // 6. Title Card Container (Fades in after 3-second hold following the 3rd line)
        GameObject titleGroupObj = new GameObject("TitleCardContainer", typeof(RectTransform), typeof(CanvasGroup));
        titleGroupObj.transform.SetParent(canvasObj.transform, false);
        RectTransform titleGroupRect = titleGroupObj.GetComponent<RectTransform>();
        titleGroupRect.anchorMin = new Vector2(0.10f, 0.25f);
        titleGroupRect.anchorMax = new Vector2(0.90f, 0.75f);
        titleGroupRect.offsetMin = Vector2.zero;
        titleGroupRect.offsetMax = Vector2.zero;

        CanvasGroup titleCg = titleGroupObj.GetComponent<CanvasGroup>();
        titleCg.alpha = 0f; // Hidden in Edit Mode so NarrativeText is clearly readable; fades in during Play Mode
        titleCg.interactable = false;
        titleCg.blocksRaycasts = false;

        GameObject titleTextObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleTextObj.transform.SetParent(titleGroupObj.transform, false);
        RectTransform titleTextRect = titleTextObj.GetComponent<RectTransform>();
        titleTextRect.anchorMin = new Vector2(0.05f, 0.48f);
        titleTextRect.anchorMax = new Vector2(0.95f, 0.82f);
        titleTextRect.offsetMin = Vector2.zero;
        titleTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI titleTmp = titleTextObj.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "The Day the Blue Flowers Died";
        titleTmp.font = titleFont;
        titleTmp.fontSize = 68;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.78f, 0.90f, 1f, 1f);
        titleTmp.raycastTarget = false;

        if (dividerSprite != null)
        {
            GameObject divObj = new GameObject("OrnateDivider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            divObj.transform.SetParent(titleGroupObj.transform, false);
            RectTransform divRect = divObj.GetComponent<RectTransform>();
            divRect.anchorMin = new Vector2(0.25f, 0.40f);
            divRect.anchorMax = new Vector2(0.75f, 0.48f);
            divRect.offsetMin = Vector2.zero;
            divRect.offsetMax = Vector2.zero;

            Image divImg = divObj.GetComponent<Image>();
            divImg.sprite = dividerSprite;
            divImg.preserveAspect = true;
            divImg.color = new Color(0.65f, 0.85f, 1f, 0.75f);
            divImg.raycastTarget = false;
        }

        GameObject subTextObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        subTextObj.transform.SetParent(titleGroupObj.transform, false);
        RectTransform subTextRect = subTextObj.GetComponent<RectTransform>();
        subTextRect.anchorMin = new Vector2(0.10f, 0.22f);
        subTextRect.anchorMax = new Vector2(0.90f, 0.38f);
        subTextRect.offsetMin = Vector2.zero;
        subTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI subTmp = subTextObj.GetComponent<TextMeshProUGUI>();
        subTmp.text = "Chapter I  \u2014  The Long Walk In";
        subTmp.font = bodyFont;
        subTmp.fontSize = 26;
        subTmp.characterSpacing = 6f;
        subTmp.alignment = TextAlignmentOptions.Center;
        subTmp.color = new Color(0.68f, 0.78f, 0.90f, 0.85f);
        subTmp.raycastTarget = false;

        // 7. Subtle Tap-to-Advance Prompt at Bottom
        GameObject promptObj = new GameObject("TapPrompt", typeof(RectTransform), typeof(CanvasRenderer), typeof(CanvasGroup), typeof(TextMeshProUGUI));
        promptObj.transform.SetParent(canvasObj.transform, false);
        RectTransform promptRect = promptObj.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0.25f, 0.06f);
        promptRect.anchorMax = new Vector2(0.75f, 0.12f);
        promptRect.offsetMin = Vector2.zero;
        promptRect.offsetMax = Vector2.zero;

        CanvasGroup promptCg = promptObj.GetComponent<CanvasGroup>();
        promptCg.alpha = 0f;
        promptCg.interactable = false;
        promptCg.blocksRaycasts = false;

        TextMeshProUGUI promptTmp = promptObj.GetComponent<TextMeshProUGUI>();
        promptTmp.text = "Click or press Space to continue";
        promptTmp.font = bodyFont;
        promptTmp.fontSize = 18;
        promptTmp.alignment = TextAlignmentOptions.Center;
        promptTmp.color = new Color(0.65f, 0.72f, 0.82f, 0.75f);
        promptTmp.raycastTarget = false;

        // 8. Full-Screen Invisible Tap-to-Advance Button
        DefaultControls.Resources uiRes = new DefaultControls.Resources();
        GameObject advanceObj = DefaultControls.CreateButton(uiRes);
        advanceObj.name = "AdvanceTrigger";
        advanceObj.transform.SetParent(canvasObj.transform, false);
        RectTransform advRect = advanceObj.GetComponent<RectTransform>();
        advRect.anchorMin = Vector2.zero;
        advRect.anchorMax = Vector2.one;
        advRect.offsetMin = Vector2.zero;
        advRect.offsetMax = Vector2.zero;

        Image advImg = advanceObj.GetComponent<Image>();
        advImg.color = new Color(0f, 0f, 0f, 0f);
        Transform advText = advanceObj.transform.Find("Text");
        if (advText != null) Object.DestroyImmediate(advText.gameObject);

        // 9. Attach OpeningSceneController and bind references
        OpeningSceneController controller = canvasObj.AddComponent<OpeningSceneController>();
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("narrativeCanvasGroup").objectReferenceValue = narrativeCg;
        so.FindProperty("narrativeText").objectReferenceValue = narTmp;
        so.FindProperty("titleCardCanvasGroup").objectReferenceValue = titleCg;
        so.FindProperty("titleText").objectReferenceValue = titleTmp;
        so.FindProperty("subtitleText").objectReferenceValue = subTmp;
        so.FindProperty("tapPromptCanvasGroup").objectReferenceValue = promptCg;
        so.FindProperty("advanceButton").objectReferenceValue = advanceObj.GetComponent<Button>();
        so.ApplyModifiedProperties();

        // 10. Save Scene
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, SCENE_PATH);

        if (!leaveOpen && !string.IsNullOrEmpty(previousScenePath) && File.Exists(previousScenePath))
        {
            EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
        }
    }

    public static void WireStartButtonToOpeningScene()
    {
        if (!File.Exists(MENU_SCENE_PATH)) return;

        string currentScene = EditorSceneManager.GetActiveScene().path;
        if (currentScene != MENU_SCENE_PATH)
        {
            EditorSceneManager.OpenScene(MENU_SCENE_PATH, OpenSceneMode.Single);
        }

        GameObject startBtnObj = GameObject.Find("startbutton");
        if (startBtnObj != null)
        {
            StartGameButton startComp = startBtnObj.GetComponent<StartGameButton>();
            if (startComp == null)
            {
                startComp = startBtnObj.AddComponent<StartGameButton>();
            }

            SerializedObject so = new SerializedObject(startComp);
            SerializedProperty targetSceneProp = so.FindProperty("targetScene");
            if (targetSceneProp != null)
            {
                targetSceneProp.stringValue = "OpeningScene";
            }
            so.ApplyModifiedProperties();

            Button btn = startBtnObj.GetComponent<Button>();
            if (btn != null)
            {
                UnityEventTools.RemovePersistentListener(btn.onClick, startComp.OnStartClicked);
                UnityEventTools.AddPersistentListener(btn.onClick, startComp.OnStartClicked);
                EditorUtility.SetDirty(btn);
            }

            EditorUtility.SetDirty(startComp);
            EditorUtility.SetDirty(startBtnObj);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
    }

    public static void RegisterScenesInBuildSettings()
    {
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();

        if (File.Exists(MENU_SCENE_PATH))
            list.Add(new EditorBuildSettingsScene(MENU_SCENE_PATH, true));

        if (File.Exists(SCENE_PATH))
            list.Add(new EditorBuildSettingsScene(SCENE_PATH, true));

        if (File.Exists(SCHOOL_SCENE_PATH))
            list.Add(new EditorBuildSettingsScene(SCHOOL_SCENE_PATH, true));

        foreach (var existing in EditorBuildSettings.scenes)
        {
            if (existing.path != MENU_SCENE_PATH &&
                existing.path != SCENE_PATH &&
                existing.path != SCHOOL_SCENE_PATH)
            {
                list.Add(existing);
            }
        }

        EditorBuildSettings.scenes = list.ToArray();
    }
}
#endif
