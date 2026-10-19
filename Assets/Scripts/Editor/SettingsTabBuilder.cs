#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine.UI;
using TMPro;

[InitializeOnLoad]
public static class SettingsTabBuilder
{
    static SettingsTabBuilder()
    {
        EditorApplication.delayCall += CheckAndBuild;
    }

    [InitializeOnLoadMethod]
    private static void CheckAndBuild()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.Log("[SettingsTabBuilder] Exiting play mode to build save archive...");
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += CheckAndBuild;
            return;
        }

        if (EditorSceneManager.GetActiveScene().name != "MenuScreen")
        {
            return;
        }

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            Transform panel = canvas.transform.Find("SettingsPanel");
            bool needsBuild = panel == null ||
                              panel.Find("SoundRow") == null ||
                              panel.Find("ResolutionRow") != null;

            if (needsBuild)
            {
                BuildSettingsTab();
            }

            if (canvas.transform.Find("SaveArchivePanel") == null)
            {
                SaveArchiveBuilder.BuildSaveArchive();
            }
        }
    }

    [MenuItem("Tools/Setup Settings Tab & Volume Control")]
    public static void BuildSettingsTab()
    {
        // 1. Locate Canvas
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[SettingsTabBuilder] No Canvas found in the active scene!");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Setup Settings Tab");

        // 2. Locate Main Menu & settingsbutton
        Transform mainMenuTf = canvas.transform.Find("Main Menu");
        GameObject mainMenu = mainMenuTf != null ? mainMenuTf.gameObject : null;

        Transform settingsBtnTf = mainMenuTf != null ? mainMenuTf.Find("settingsbutton") : null;
        if (settingsBtnTf == null)
        {
            GameObject found = GameObject.Find("settingsbutton");
            if (found != null) settingsBtnTf = found.transform;
        }

        // 3. Locate BGM AudioSource
        GameObject bgmObj = GameObject.Find("BGM");
        AudioSource bgmSource = bgmObj != null ? bgmObj.GetComponent<AudioSource>() : null;

        // 4. Load Font Asset
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Buttons/Fonts/BLKCHCRY SDF.asset");
        if (fontAsset == null)
        {
            fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }

        // Load Audio Clips
        AudioClip clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Sound Effect/click.mp3");
        AudioClip hoverClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Sound Effect/hover.mp3");

        // 5. Create or Get SettingsPanel
        Transform existingPanel = canvas.transform.Find("SettingsPanel");
        GameObject panelObj;
        if (existingPanel != null)
        {
            panelObj = existingPanel.gameObject;
        }
        else
        {
            panelObj = new GameObject("SettingsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObj.transform.SetParent(canvas.transform, false);
        }

        // Style Panel RectTransform
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(900f, 680f);

        // Style Panel Image (Deep translucent modal window)
        Image panelImage = panelObj.GetComponent<Image>();
        panelImage.color = new Color(0.06f, 0.12f, 0.20f, 0.95f);

        // Add AudioSource for preview sound
        AudioSource panelAudio = panelObj.GetComponent<AudioSource>();
        if (panelAudio == null)
        {
            panelAudio = panelObj.AddComponent<AudioSource>();
        }
        panelAudio.playOnAwake = false;

        // Add or get SettingsMenu component
        SettingsMenu settingsMenu = panelObj.GetComponent<SettingsMenu>();
        if (settingsMenu == null)
        {
            settingsMenu = panelObj.AddComponent<SettingsMenu>();
        }

        // Clean out existing children to rebuild cleanly
        for (int i = panelObj.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(panelObj.transform.GetChild(i).gameObject);
        }

        DefaultControls.Resources uiResources = new DefaultControls.Resources();

        // 6. Title Text
        GameObject titleObj = new GameObject("SettingsTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(panelObj.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -35f);
        titleRect.sizeDelta = new Vector2(600f, 85f);

        TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
        titleText.text = "SETTINGS";
        if (fontAsset != null) titleText.font = fontAsset;
        titleText.fontSize = 72;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0.95f, 0.95f, 1f, 1f);

        // 7. BGM Volume Section Container
        GameObject bgmRow = new GameObject("VolumeRow", typeof(RectTransform));
        bgmRow.transform.SetParent(panelObj.transform, false);
        RectTransform bgmRowRect = bgmRow.GetComponent<RectTransform>();
        bgmRowRect.anchorMin = new Vector2(0.5f, 1f);
        bgmRowRect.anchorMax = new Vector2(0.5f, 1f);
        bgmRowRect.pivot = new Vector2(0.5f, 1f);
        bgmRowRect.anchoredPosition = new Vector2(0, -135f);
        bgmRowRect.sizeDelta = new Vector2(800f, 115f);

        // Label: "BGM Volume"
        GameObject bgmLabelObj = new GameObject("VolumeLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        bgmLabelObj.transform.SetParent(bgmRow.transform, false);
        RectTransform bgmLabelRect = bgmLabelObj.GetComponent<RectTransform>();
        bgmLabelRect.anchorMin = new Vector2(0.5f, 1f);
        bgmLabelRect.anchorMax = new Vector2(0.5f, 1f);
        bgmLabelRect.pivot = new Vector2(0.5f, 1f);
        bgmLabelRect.anchoredPosition = new Vector2(0, 0);
        bgmLabelRect.sizeDelta = new Vector2(500f, 45f);

        TextMeshProUGUI bgmLabelText = bgmLabelObj.GetComponent<TextMeshProUGUI>();
        bgmLabelText.text = "BGM Volume";
        if (fontAsset != null) bgmLabelText.font = fontAsset;
        bgmLabelText.fontSize = 42;
        bgmLabelText.alignment = TextAlignmentOptions.Center;
        bgmLabelText.color = new Color(0.85f, 0.92f, 1f, 1f);

        // BGM Slider Container
        GameObject bgmSliderObj = DefaultControls.CreateSlider(uiResources);
        bgmSliderObj.name = "BgmSlider";
        bgmSliderObj.transform.SetParent(bgmRow.transform, false);
        RectTransform bgmSliderRect = bgmSliderObj.GetComponent<RectTransform>();
        bgmSliderRect.anchorMin = new Vector2(0.5f, 0f);
        bgmSliderRect.anchorMax = new Vector2(0.5f, 0f);
        bgmSliderRect.pivot = new Vector2(0.5f, 0f);
        bgmSliderRect.anchoredPosition = new Vector2(-50f, 15f);
        bgmSliderRect.sizeDelta = new Vector2(480f, 32f);

        Slider bgmSlider = bgmSliderObj.GetComponent<Slider>();
        bgmSlider.minValue = 0f;
        bgmSlider.maxValue = 1f;
        bgmSlider.value = bgmSource != null ? bgmSource.volume : 0.8f;

        // Custom BGM Slider styling
        Image bgmFillImage = bgmSlider.fillRect != null ? bgmSlider.fillRect.GetComponent<Image>() : null;
        if (bgmFillImage != null)
        {
            bgmFillImage.color = new Color(0.2f, 0.6f, 0.95f, 1f);
        }

        Image bgmHandleImage = bgmSlider.handleRect != null ? bgmSlider.handleRect.GetComponent<Image>() : null;
        if (bgmHandleImage != null)
        {
            bgmHandleImage.color = new Color(0.95f, 0.95f, 1f, 1f);
        }

        // Percentage Text next to BGM Slider
        GameObject bgmPercentObj = new GameObject("VolumePercentText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        bgmPercentObj.transform.SetParent(bgmRow.transform, false);
        RectTransform bgmPercentRect = bgmPercentObj.GetComponent<RectTransform>();
        bgmPercentRect.anchorMin = new Vector2(0.5f, 0f);
        bgmPercentRect.anchorMax = new Vector2(0.5f, 0f);
        bgmPercentRect.pivot = new Vector2(0f, 0.5f);
        bgmPercentRect.anchoredPosition = new Vector2(215f, 31f);
        bgmPercentRect.sizeDelta = new Vector2(120f, 40f);

        TextMeshProUGUI bgmPercentText = bgmPercentObj.GetComponent<TextMeshProUGUI>();
        bgmPercentText.text = Mathf.RoundToInt(bgmSlider.value * 100f) + "%";
        if (fontAsset != null) bgmPercentText.font = fontAsset;
        bgmPercentText.fontSize = 38;
        bgmPercentText.alignment = TextAlignmentOptions.MidlineLeft;
        bgmPercentText.color = new Color(0.95f, 0.95f, 1f, 1f);

        // 8. Sound Volume Section Container (Replaces ResolutionRow)
        GameObject soundRow = new GameObject("SoundRow", typeof(RectTransform));
        soundRow.transform.SetParent(panelObj.transform, false);
        RectTransform soundRowRect = soundRow.GetComponent<RectTransform>();
        soundRowRect.anchorMin = new Vector2(0.5f, 1f);
        soundRowRect.anchorMax = new Vector2(0.5f, 1f);
        soundRowRect.pivot = new Vector2(0.5f, 1f);
        soundRowRect.anchoredPosition = new Vector2(0, -270f);
        soundRowRect.sizeDelta = new Vector2(800f, 210f);

        // Label: "Sound Volume"
        GameObject soundLabelObj = new GameObject("SoundLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        soundLabelObj.transform.SetParent(soundRow.transform, false);
        RectTransform soundLabelRect = soundLabelObj.GetComponent<RectTransform>();
        soundLabelRect.anchorMin = new Vector2(0.5f, 1f);
        soundLabelRect.anchorMax = new Vector2(0.5f, 1f);
        soundLabelRect.pivot = new Vector2(0.5f, 1f);
        soundLabelRect.anchoredPosition = new Vector2(0, 0);
        soundLabelRect.sizeDelta = new Vector2(500f, 45f);

        TextMeshProUGUI soundLabelText = soundLabelObj.GetComponent<TextMeshProUGUI>();
        soundLabelText.text = "Sound Volume";
        if (fontAsset != null) soundLabelText.font = fontAsset;
        soundLabelText.fontSize = 42;
        soundLabelText.alignment = TextAlignmentOptions.Center;
        soundLabelText.color = new Color(0.85f, 0.92f, 1f, 1f);

        // Helper to create styled buttons
        Button CreateStyledButton(Transform parent, string btnName, string label, Vector2 anchoredPos, Vector2 size, int fontSize = 34)
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
            bImg.color = new Color(0.14f, 0.22f, 0.35f, 0.9f);

            foreach (Text legacyText in btnObj.GetComponentsInChildren<Text>(true))
            {
                if (legacyText != null) Object.DestroyImmediate(legacyText.gameObject);
            }

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
            AudioSource btnAudio = btnObj.AddComponent<AudioSource>();
            btnAudio.playOnAwake = false;
            ButtonFunction bFunc = btnObj.AddComponent<ButtonFunction>();
            bFunc.mySound = btnAudio;
            bFunc.hoverSound = hoverClip;
            bFunc.clickSound = clickClip;

            UnityEventTools.AddPersistentListener(btn.onClick, bFunc.ClickSound);

            return btn;
        }

        // Minus Button for Sound Volume
        Button soundMinusBtn = CreateStyledButton(soundRow.transform, "SoundMinusButton", "—", new Vector2(-235f, 92f), new Vector2(48f, 40f), 30);

        // Sound Slider Container
        GameObject soundSliderObj = DefaultControls.CreateSlider(uiResources);
        soundSliderObj.name = "SoundSlider";
        soundSliderObj.transform.SetParent(soundRow.transform, false);
        RectTransform soundSliderRect = soundSliderObj.GetComponent<RectTransform>();
        soundSliderRect.anchorMin = new Vector2(0.5f, 0f);
        soundSliderRect.anchorMax = new Vector2(0.5f, 0f);
        soundSliderRect.pivot = new Vector2(0.5f, 0.5f);
        soundSliderRect.anchoredPosition = new Vector2(-20f, 92f);
        soundSliderRect.sizeDelta = new Vector2(350f, 32f);

        Slider soundSlider = soundSliderObj.GetComponent<Slider>();
        soundSlider.minValue = 0f;
        soundSlider.maxValue = 1f;
        float savedSoundVol = PlayerPrefs.GetFloat("SFX_Volume", 0.8f);
        soundSlider.value = savedSoundVol;

        // Custom Sound Slider styling
        Image soundFillImage = soundSlider.fillRect != null ? soundSlider.fillRect.GetComponent<Image>() : null;
        if (soundFillImage != null)
        {
            soundFillImage.color = new Color(0.25f, 0.72f, 0.95f, 1f);
        }

        Image soundHandleImage = soundSlider.handleRect != null ? soundSlider.handleRect.GetComponent<Image>() : null;
        if (soundHandleImage != null)
        {
            soundHandleImage.color = new Color(0.95f, 0.95f, 1f, 1f);
        }

        // Plus Button for Sound Volume
        Button soundPlusBtn = CreateStyledButton(soundRow.transform, "SoundPlusButton", "+", new Vector2(195f, 92f), new Vector2(48f, 40f), 34);

        // Percentage Text next to Sound Slider
        GameObject soundPercentObj = new GameObject("SoundPercentText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        soundPercentObj.transform.SetParent(soundRow.transform, false);
        RectTransform soundPercentRect = soundPercentObj.GetComponent<RectTransform>();
        soundPercentRect.anchorMin = new Vector2(0.5f, 0f);
        soundPercentRect.anchorMax = new Vector2(0.5f, 0f);
        soundPercentRect.pivot = new Vector2(0f, 0.5f);
        soundPercentRect.anchoredPosition = new Vector2(235f, 92f);
        soundPercentRect.sizeDelta = new Vector2(110f, 40f);

        TextMeshProUGUI soundPercentText = soundPercentObj.GetComponent<TextMeshProUGUI>();
        soundPercentText.text = Mathf.RoundToInt(savedSoundVol * 100f) + "%";
        if (fontAsset != null) soundPercentText.font = fontAsset;
        soundPercentText.fontSize = 38;
        soundPercentText.alignment = TextAlignmentOptions.MidlineLeft;
        soundPercentText.color = new Color(0.95f, 0.95f, 1f, 1f);

        // Preset Buttons Row
        Button soundMuteBtn = CreateStyledButton(soundRow.transform, "SoundMuteButton", "Mute", new Vector2(-160f, 30f), new Vector2(130f, 50f), 34);
        Button sound50Btn = CreateStyledButton(soundRow.transform, "Sound50Button", "50%", new Vector2(0f, 30f), new Vector2(130f, 50f), 34);
        Button sound100Btn = CreateStyledButton(soundRow.transform, "Sound100Button", "100%", new Vector2(160f, 30f), new Vector2(130f, 50f), 34);

        // 9. Back / Close Button
        GameObject backBtnObj = DefaultControls.CreateButton(uiResources);
        backBtnObj.name = "BackButton";
        backBtnObj.transform.SetParent(panelObj.transform, false);
        RectTransform backBtnRect = backBtnObj.GetComponent<RectTransform>();
        backBtnRect.anchorMin = new Vector2(0.5f, 0f);
        backBtnRect.anchorMax = new Vector2(0.5f, 0f);
        backBtnRect.pivot = new Vector2(0.5f, 0f);
        backBtnRect.anchoredPosition = new Vector2(0, 35f);
        backBtnRect.sizeDelta = new Vector2(250f, 65f);

        Image backBtnImg = backBtnObj.GetComponent<Image>();
        backBtnImg.color = new Color(0.18f, 0.32f, 0.52f, 0.9f);

        foreach (Text legacyText in backBtnObj.GetComponentsInChildren<Text>(true))
        {
            if (legacyText != null) Object.DestroyImmediate(legacyText.gameObject);
        }

        GameObject backTextObj = new GameObject("Text (TMP)", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        backTextObj.transform.SetParent(backBtnObj.transform, false);
        RectTransform backTextRect = backTextObj.GetComponent<RectTransform>();
        backTextRect.anchorMin = Vector2.zero;
        backTextRect.anchorMax = Vector2.one;
        backTextRect.sizeDelta = Vector2.zero;
        backTextRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI backText = backTextObj.GetComponent<TextMeshProUGUI>();
        backText.text = "Back";
        if (fontAsset != null) backText.font = fontAsset;
        backText.fontSize = 42;
        backText.alignment = TextAlignmentOptions.Center;
        backText.color = Color.white;

        // Sound audio source and ButtonFunction for Back Button
        Button backBtn = backBtnObj.GetComponent<Button>();
        AudioSource backBtnAudio = backBtnObj.AddComponent<AudioSource>();
        backBtnAudio.playOnAwake = false;
        ButtonFunction backBtnFunc = backBtnObj.AddComponent<ButtonFunction>();
        backBtnFunc.mySound = backBtnAudio;
        backBtnFunc.hoverSound = hoverClip;
        backBtnFunc.clickSound = clickClip;

        // Wire Back Button onClick
        UnityEventTools.RemovePersistentListener(backBtn.onClick, backBtnFunc.ClickSound);
        UnityEventTools.AddPersistentListener(backBtn.onClick, backBtnFunc.ClickSound);
        UnityEventTools.RemovePersistentListener(backBtn.onClick, settingsMenu.CloseSettings);
        UnityEventTools.AddPersistentListener(backBtn.onClick, settingsMenu.CloseSettings);

        // 10. Wire SettingsMenu references via SerializedObject
        SerializedObject so = new SerializedObject(settingsMenu);
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

        so.FindProperty("settingsPanel").objectReferenceValue = panelObj;
        so.FindProperty("mainMenuPanel").objectReferenceValue = mainMenu;
        so.ApplyModifiedProperties();

        // Wire buttons onClick to SettingsMenu methods
        UnityEventTools.RemovePersistentListener(soundMuteBtn.onClick, settingsMenu.MuteSound);
        UnityEventTools.AddPersistentListener(soundMuteBtn.onClick, settingsMenu.MuteSound);

        UnityEventTools.RemovePersistentListener(sound50Btn.onClick, settingsMenu.SetSound50);
        UnityEventTools.AddPersistentListener(sound50Btn.onClick, settingsMenu.SetSound50);

        UnityEventTools.RemovePersistentListener(sound100Btn.onClick, settingsMenu.SetSound100);
        UnityEventTools.AddPersistentListener(sound100Btn.onClick, settingsMenu.SetSound100);

        UnityEventTools.AddFloatPersistentListener(soundMinusBtn.onClick, settingsMenu.ChangeSoundVolumeBy, -0.1f);
        UnityEventTools.AddFloatPersistentListener(soundPlusBtn.onClick, settingsMenu.ChangeSoundVolumeBy, 0.1f);

        // Wire settingsbutton onClick -> SettingsMenu.OpenSettings
        if (settingsBtnTf != null)
        {
            Button settingsBtn = settingsBtnTf.GetComponent<Button>();
            if (settingsBtn != null)
            {
                UnityEventTools.RemovePersistentListener(settingsBtn.onClick, settingsMenu.OpenSettings);
                UnityEventTools.AddPersistentListener(settingsBtn.onClick, settingsMenu.OpenSettings);
                EditorUtility.SetDirty(settingsBtn);
            }
        }

        // Hide panel initially so it only shows when Settings button is clicked
        panelObj.SetActive(false);

        // Mark Dirty & Save Scene
        EditorUtility.SetDirty(panelObj);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("<color=green>[SettingsTabBuilder] Successfully created and wired Settings Tab with BGM & Sound Volume controls!</color>");
    }
}
#endif
