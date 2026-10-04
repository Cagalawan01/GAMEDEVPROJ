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
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas != null && canvas.transform.Find("SettingsPanel") == null)
        {
            BuildSettingsTab();
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

        // 3. Locate BGM
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
        panelRect.sizeDelta = new Vector2(900f, 600f);

        // Style Panel Image (Deep translucent modal window)
        Image panelImage = panelObj.GetComponent<Image>();
        panelImage.color = new Color(0.06f, 0.12f, 0.20f, 0.94f);

        // Add or get SettingsMenu component
        SettingsMenu settingsMenu = panelObj.GetComponent<SettingsMenu>();
        if (settingsMenu == null)
        {
            settingsMenu = panelObj.AddComponent<SettingsMenu>();
        }

        // Clean out existing children to ensure clean rebuild
        for (int i = panelObj.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(panelObj.transform.GetChild(i).gameObject);
        }

        // 6. Title Text
        GameObject titleObj = new GameObject("SettingsTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(panelObj.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -45f);
        titleRect.sizeDelta = new Vector2(600f, 100f);

        TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
        titleText.text = "SETTINGS";
        if (fontAsset != null) titleText.font = fontAsset;
        titleText.fontSize = 76;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0.95f, 0.95f, 1f, 1f);

        // 7. Volume Section Container
        GameObject volumeRow = new GameObject("VolumeRow", typeof(RectTransform));
        volumeRow.transform.SetParent(panelObj.transform, false);
        RectTransform volRowRect = volumeRow.GetComponent<RectTransform>();
        volRowRect.anchorMin = new Vector2(0.5f, 0.5f);
        volRowRect.anchorMax = new Vector2(0.5f, 0.5f);
        volRowRect.pivot = new Vector2(0.5f, 0.5f);
        volRowRect.anchoredPosition = new Vector2(0, 30f);
        volRowRect.sizeDelta = new Vector2(800f, 140f);

        // Label: "BGM Volume"
        GameObject labelObj = new GameObject("VolumeLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(volumeRow.transform, false);
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 1f);
        labelRect.anchorMax = new Vector2(0.5f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(0, 0);
        labelRect.sizeDelta = new Vector2(500f, 55f);

        TextMeshProUGUI labelText = labelObj.GetComponent<TextMeshProUGUI>();
        labelText.text = "BGM Volume";
        if (fontAsset != null) labelText.font = fontAsset;
        labelText.fontSize = 46;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = new Color(0.85f, 0.92f, 1f, 1f);

        // Slider Container
        DefaultControls.Resources uiResources = new DefaultControls.Resources();
        GameObject sliderObj = DefaultControls.CreateSlider(uiResources);
        sliderObj.name = "BgmSlider";
        sliderObj.transform.SetParent(volumeRow.transform, false);
        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0f);
        sliderRect.anchorMax = new Vector2(0.5f, 0f);
        sliderRect.pivot = new Vector2(0.5f, 0f);
        sliderRect.anchoredPosition = new Vector2(-50f, 20f);
        sliderRect.sizeDelta = new Vector2(480f, 36f);

        Slider slider = sliderObj.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = bgmSource != null ? bgmSource.volume : 0.8f;

        // Custom Slider styling
        Image fillImage = slider.fillRect != null ? slider.fillRect.GetComponent<Image>() : null;
        if (fillImage != null)
        {
            fillImage.color = new Color(0.2f, 0.65f, 0.95f, 1f); // Vibrant light blue
        }

        Image handleImage = slider.handleRect != null ? slider.handleRect.GetComponent<Image>() : null;
        if (handleImage != null)
        {
            handleImage.color = new Color(0.95f, 0.95f, 1f, 1f);
        }

        // Percentage Text next to Slider
        GameObject percentObj = new GameObject("VolumePercentText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        percentObj.transform.SetParent(volumeRow.transform, false);
        RectTransform percentRect = percentObj.GetComponent<RectTransform>();
        percentRect.anchorMin = new Vector2(0.5f, 0f);
        percentRect.anchorMax = new Vector2(0.5f, 0f);
        percentRect.pivot = new Vector2(0f, 0.5f);
        percentRect.anchoredPosition = new Vector2(215f, 38f);
        percentRect.sizeDelta = new Vector2(130f, 40f);

        TextMeshProUGUI percentText = percentObj.GetComponent<TextMeshProUGUI>();
        percentText.text = Mathf.RoundToInt(slider.value * 100f) + "%";
        if (fontAsset != null) percentText.font = fontAsset;
        percentText.fontSize = 40;
        percentText.alignment = TextAlignmentOptions.MidlineLeft;
        percentText.color = new Color(0.95f, 0.95f, 1f, 1f);

        // 8. Back / Close Button
        GameObject backBtnObj = DefaultControls.CreateButton(uiResources);
        backBtnObj.name = "BackButton";
        backBtnObj.transform.SetParent(panelObj.transform, false);
        RectTransform backBtnRect = backBtnObj.GetComponent<RectTransform>();
        backBtnRect.anchorMin = new Vector2(0.5f, 0f);
        backBtnRect.anchorMax = new Vector2(0.5f, 0f);
        backBtnRect.pivot = new Vector2(0.5f, 0f);
        backBtnRect.anchoredPosition = new Vector2(0, 50f);
        backBtnRect.sizeDelta = new Vector2(260f, 75f);

        Image backBtnImg = backBtnObj.GetComponent<Image>();
        backBtnImg.color = new Color(0.18f, 0.32f, 0.52f, 0.9f);

        // Replace default Button Text with TextMeshPro
        Transform defaultText = backBtnObj.transform.Find("Text");
        if (defaultText != null) Object.DestroyImmediate(defaultText.gameObject);

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
        backText.fontSize = 44;
        backText.alignment = TextAlignmentOptions.Center;
        backText.color = Color.white;

        // Button component for Back button
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

        // 9. Wire SettingsMenu references via SerializedObject
        SerializedObject so = new SerializedObject(settingsMenu);
        so.FindProperty("bgmAudioSource").objectReferenceValue = bgmSource;
        so.FindProperty("bgmSlider").objectReferenceValue = slider;
        so.FindProperty("volumePercentText").objectReferenceValue = percentText;
        so.FindProperty("settingsPanel").objectReferenceValue = panelObj;
        so.FindProperty("mainMenuPanel").objectReferenceValue = mainMenu;
        so.ApplyModifiedProperties();

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

        Debug.Log("<color=green>[SettingsTabBuilder] Successfully created and wired the Settings Tab with BGM Volume slider!</color>");
    }
}
#endif
