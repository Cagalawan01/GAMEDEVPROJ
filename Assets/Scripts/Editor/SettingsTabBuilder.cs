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
                              panel.Find("TopIconBadge") == null ||
                              panel.Find("BgmSection") == null;

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
        panelRect.sizeDelta = new Vector2(740f, 540f);

        // Style Panel Image (Deep gothic navy window)
        Image panelImage = panelObj.GetComponent<Image>();
        panelImage.color = new Color(0.04f, 0.08f, 0.16f, 0.96f);

        Outline panelOutline = panelObj.GetComponent<Outline>();
        if (panelOutline == null) panelOutline = panelObj.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.30f, 0.72f, 0.88f, 0.75f);
        panelOutline.effectDistance = new Vector2(2f, -2f);

        CanvasGroup panelCg = panelObj.GetComponent<CanvasGroup>();
        if (panelCg == null) panelCg = panelObj.AddComponent<CanvasGroup>();
        panelCg.alpha = 0f;
        panelCg.interactable = false;
        panelCg.blocksRaycasts = false;

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

        // --- 8. Primary Hero Back Button ---
        Color heroBg = new Color(0.14f, 0.42f, 0.56f, 0.95f);
        Color heroOutline = new Color(0.38f, 0.85f, 1f, 0.85f);
        Button backBtn = CreatePillButton(panelObj.transform, "BackButton", "\u25C1  Back", new Vector2(0, -446f), new Vector2(620f, 46f), heroBg, heroOutline, 22);
        RectTransform backRect = backBtn.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.5f, 1f);
        backRect.anchorMax = new Vector2(0.5f, 1f);
        backRect.pivot = new Vector2(0.5f, 1f);
        backRect.anchoredPosition = new Vector2(0, -446f);

        // --- 9. Wire Listeners ---
        UnityEventTools.AddPersistentListener(bgmMuteBtn.onClick, settingsMenu.MuteBGM);
        UnityEventTools.AddPersistentListener(bgm50Btn.onClick, settingsMenu.SetBGM50);
        UnityEventTools.AddPersistentListener(bgm100Btn.onClick, settingsMenu.SetBGM100);

        UnityEventTools.AddPersistentListener(soundMuteBtn.onClick, settingsMenu.MuteSound);
        UnityEventTools.AddPersistentListener(sound50Btn.onClick, settingsMenu.SetSound50);
        UnityEventTools.AddPersistentListener(sound100Btn.onClick, settingsMenu.SetSound100);

        UnityEventTools.AddPersistentListener(backBtn.onClick, settingsMenu.CloseSettings);

        // --- 10. Wire SerializedProperties on SettingsMenu ---
        SerializedObject so = new SerializedObject(settingsMenu);
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

        so.FindProperty("settingsPanel").objectReferenceValue = panelObj;
        so.FindProperty("mainMenuPanel").objectReferenceValue = mainMenu;
        so.FindProperty("settingsCanvasGroup").objectReferenceValue = panelCg;
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

        Debug.Log("<color=green>[SettingsTabBuilder] Successfully created and wired Settings Tab with BGM & Sound Volume controls!</color>");
    }
}
#endif