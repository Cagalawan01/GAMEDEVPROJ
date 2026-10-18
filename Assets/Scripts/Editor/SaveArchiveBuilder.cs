#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine.UI;
using TMPro;

[InitializeOnLoad]
public static class SaveArchiveBuilder
{
    static SaveArchiveBuilder()
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
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        if (EditorSceneManager.GetActiveScene().name != "MenuScreen")
        {
            return;
        }

        Debug.Log("[SaveArchiveBuilder] Upgrading Save Archive UI with Main Menu styling...");
        BuildSaveArchive();
    }

    [MenuItem("Tools/Setup Save Archive (Continue)")]
    public static void BuildSaveArchive()
    {
        if (EditorSceneManager.GetActiveScene().name != "MenuScreen")
        {
            return;
        }

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[SaveArchiveBuilder] No Canvas found in the active scene!");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Setup Save Archive");

        Transform mainMenuTf = canvas.transform.Find("Main Menu");
        GameObject mainMenu = mainMenuTf != null ? mainMenuTf.gameObject : null;

        SaveArchiveMenu archiveMenu = BuildSaveArchiveForCanvas(canvas, mainMenu, false);

        Transform resumeBtnTf = mainMenuTf != null ? mainMenuTf.Find("resumebutton") : null;
        if (resumeBtnTf == null)
        {
            GameObject found = GameObject.Find("resumebutton");
            if (found != null) resumeBtnTf = found.transform;
        }

        if (resumeBtnTf != null && archiveMenu != null)
        {
            Button resumeBtn = resumeBtnTf.GetComponent<Button>();
            if (resumeBtn != null)
            {
                UnityEventTools.RemovePersistentListener(resumeBtn.onClick, archiveMenu.OpenArchive);
                UnityEventTools.AddPersistentListener(resumeBtn.onClick, archiveMenu.OpenArchive);
                EditorUtility.SetDirty(resumeBtn);
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("<color=cyan>[SaveArchiveBuilder] Successfully upgraded Save Archive with large cards and Main Menu button styling!</color>");
    }

    public static SaveArchiveMenu BuildSaveArchiveForCanvas(Canvas canvas, GameObject mainMenu, bool saveMode = false)
    {
        if (canvas == null) return null;

        // 2. Load Fonts - Main Menu Font: BLKCHCRY SDF
        TMP_FontAsset menuFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Buttons/Fonts/BLKCHCRY SDF.asset");
        if (menuFont == null) menuFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        TMP_FontAsset ruritaniaFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Buttons/Fonts/Ruritania SDF.asset");
        if (ruritaniaFont == null) ruritaniaFont = menuFont;

        // 3. Load Audio Clips
        AudioClip clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Sound Effect/click.mp3");
        AudioClip hoverClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Sound Effect/hover.mp3");

        // 4. Load Sprites
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/SaveThumbnails/archive_clean_bg.png");
        if (bgSprite == null) bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/SaveThumbnails/archive_background.png");

        Sprite[] p1Sprites = new Sprite[6];
        for (int i = 0; i < 6; i++)
        {
            string path = $"Assets/Art/SaveThumbnails/card_0{i + 1}.png";
            p1Sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        Sprite[] p2Sprites = new Sprite[6];
        for (int i = 0; i < 6; i++)
        {
            int num = i + 7;
            string prefix = num < 10 ? "0" : "";
            string path = $"Assets/Art/SaveThumbnails/card_{prefix}{num}.png";
            p2Sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // 5. Create or Get SaveArchivePanel
        Transform existingPanel = canvas.transform.Find("SaveArchivePanel");
        GameObject panelObj;
        if (existingPanel != null)
        {
            panelObj = existingPanel.gameObject;
        }
        else
        {
            panelObj = new GameObject("SaveArchivePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObj.transform.SetParent(canvas.transform, false);
        }

        // Anchor full screen
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Ensure Main Menu Video Background loops cleanly without freezing
        Transform menuBgTf = canvas.transform.Find("Menubackground");
        if (menuBgTf != null)
        {
            UnityEngine.Video.VideoPlayer vp = menuBgTf.GetComponent<UnityEngine.Video.VideoPlayer>();
            if (vp != null)
            {
                vp.isLooping = true;
                vp.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.None;
            }
            if (menuBgTf.GetComponent<MenuVideoLoop>() == null)
            {
                menuBgTf.gameObject.AddComponent<MenuVideoLoop>();
            }
        }

        // Background on SaveArchivePanel:
        Image panelImg = panelObj.GetComponent<Image>();
        panelImg.sprite = null;
        panelImg.color = saveMode ? new Color(0.015f, 0.04f, 0.09f, 0.85f) : new Color(0.015f, 0.04f, 0.09f, 0.62f);

        // AudioSource on Panel
        AudioSource panelAudio = panelObj.GetComponent<AudioSource>();
        if (panelAudio == null) panelAudio = panelObj.AddComponent<AudioSource>();
        panelAudio.playOnAwake = false;

        // CanvasGroup for smooth transitions
        CanvasGroup archiveCg = panelObj.GetComponent<CanvasGroup>();
        if (archiveCg == null) archiveCg = panelObj.AddComponent<CanvasGroup>();
        archiveCg.alpha = 0f;
        archiveCg.interactable = false;
        archiveCg.blocksRaycasts = false;

        CanvasGroup mainMenuCg = null;
        if (mainMenu != null)
        {
            mainMenuCg = mainMenu.GetComponent<CanvasGroup>();
            if (mainMenuCg == null) mainMenuCg = mainMenu.AddComponent<CanvasGroup>();
            if (!saveMode)
            {
                mainMenuCg.alpha = 1f;
                mainMenuCg.interactable = true;
                mainMenuCg.blocksRaycasts = true;
            }
        }

        // SaveArchiveMenu component
        SaveArchiveMenu archiveMenu = panelObj.GetComponent<SaveArchiveMenu>();
        if (archiveMenu == null) archiveMenu = panelObj.AddComponent<SaveArchiveMenu>();

        // Clear existing children to rebuild cleanly
        for (int i = panelObj.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(panelObj.transform.GetChild(i).gameObject);
        }

        DefaultControls.Resources uiRes = new DefaultControls.Resources();

        // 6. Top Left Header (Load Memory & Subtitle)
        GameObject headerObj = new GameObject("HeaderArea", typeof(RectTransform));
        headerObj.transform.SetParent(panelObj.transform, false);
        RectTransform hRect = headerObj.GetComponent<RectTransform>();
        hRect.anchorMin = new Vector2(0.05f, 0.86f);
        hRect.anchorMax = new Vector2(0.60f, 0.97f);
        hRect.offsetMin = Vector2.zero;
        hRect.offsetMax = Vector2.zero;
        hRect.pivot = new Vector2(0f, 1f);

        // Title: Load Memory
        GameObject titleObj = new GameObject("ArchiveTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(headerObj.transform, false);
        RectTransform tRect = titleObj.GetComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0f, 0.35f);
        tRect.anchorMax = new Vector2(1f, 1f);
        tRect.offsetMin = Vector2.zero;
        tRect.offsetMax = Vector2.zero;

        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        titleTmp.text = saveMode ? "\u2726  Save Memory  \u2726" : "\u2726  Load Memory  \u2726";
        titleTmp.font = menuFont;
        titleTmp.fontSize = 58;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Left;
        titleTmp.color = new Color(0.9f, 0.96f, 1f, 1f);

        // Subtitle: MEMORY ARCHIVE — PAGE 01
        GameObject subObj = new GameObject("PageSubtitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        subObj.transform.SetParent(headerObj.transform, false);
        RectTransform sRect = subObj.GetComponent<RectTransform>();
        sRect.anchorMin = new Vector2(0f, 0f);
        sRect.anchorMax = new Vector2(1f, 0.35f);
        sRect.offsetMin = Vector2.zero;
        sRect.offsetMax = Vector2.zero;

        TextMeshProUGUI subTmp = subObj.GetComponent<TextMeshProUGUI>();
        subTmp.text = "MEMORY ARCHIVE  \u2014  PAGE 01";
        subTmp.font = menuFont;
        subTmp.fontSize = 24;
        subTmp.alignment = TextAlignmentOptions.Left;
        subTmp.color = new Color(0.6f, 0.8f, 1f, 0.85f);
        subTmp.characterSpacing = 4;

        // 7. Save Cards Grid (6 cards, 2 rows x 3 columns) - BIGGER CARDS
        // Col anchors: [0.05, 0.335], [0.36, 0.645], [0.67, 0.955]
        // Row anchors: Row 0 [0.49, 0.84], Row 1 [0.13, 0.48]
        float[] colMins = new float[] { 0.05f, 0.36f, 0.67f };
        float[] colMaxs = new float[] { 0.335f, 0.645f, 0.955f };
        float[] rowMins = new float[] { 0.49f, 0.13f };
        float[] rowMaxs = new float[] { 0.84f, 0.48f };

        Button[] cards = new Button[6];
        Image[] cardImgs = new Image[6];
        TextMeshProUGUI[] cardTitles = new TextMeshProUGUI[6];
        TextMeshProUGUI[] cardTimes = new TextMeshProUGUI[6];

        string[] defaultTitles = new string[6]
        {
            "01. The First Encounter",
            "02. A Fading Promise",
            "03. The Silent Hall",
            "04. An Unspoken Truth",
            "05. Where It Began",
            "06. The Forgotten Night"
        };

        string[] defaultTimes = new string[6]
        {
            "2026.10.15  21:44",
            "2026.10.16  17:33",
            "2026.10.17  15:44",
            "2026.10.18  22:06",
            "2026.10.19  09:35",
            "2026.10.20  11:08"
        };

        Sprite frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/SaveThumbnails/ornate_card_frame.png");
        if (frameSprite == null)
        {
            AssetDatabase.ImportAsset("Assets/Art/SaveThumbnails/ornate_card_frame.png", ImportAssetOptions.ForceUpdate);
            frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/SaveThumbnails/ornate_card_frame.png");
        }

        for (int r = 0; r < 2; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                int idx = r * 3 + c;
                GameObject cardObj = DefaultControls.CreateButton(uiRes);
                cardObj.name = $"Card_{idx + 1}";
                cardObj.transform.SetParent(panelObj.transform, false);

                RectTransform cardRect = cardObj.GetComponent<RectTransform>();
                cardRect.anchorMin = new Vector2(colMins[c], rowMins[r]);
                cardRect.anchorMax = new Vector2(colMaxs[c], rowMaxs[r]);
                cardRect.offsetMin = new Vector2(8f, 6f);
                cardRect.offsetMax = new Vector2(-8f, -6f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);

                Transform defaultText = cardObj.transform.Find("Text");
                if (defaultText != null) Object.DestroyImmediate(defaultText.gameObject);

                // Ornate card frame covering the entire box
                Image cBgImg = cardObj.GetComponent<Image>();
                if (frameSprite != null)
                {
                    cBgImg.sprite = frameSprite;
                    cBgImg.type = Image.Type.Simple;
                    cBgImg.preserveAspect = false;
                    cBgImg.color = Color.white;
                }
                else
                {
                    cBgImg.color = new Color(0.04f, 0.11f, 0.22f, 0.92f);
                }

                // Thumbnail Container & Image (Large top portion, hidden if slot is empty)
                GameObject thumbObj = new GameObject("Thumbnail", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                thumbObj.transform.SetParent(cardObj.transform, false);
                RectTransform tImgRect = thumbObj.GetComponent<RectTransform>();
                tImgRect.anchorMin = new Vector2(0.045f, 0.28f);
                tImgRect.anchorMax = new Vector2(0.955f, 0.93f);
                tImgRect.offsetMin = Vector2.zero;
                tImgRect.offsetMax = Vector2.zero;

                Image thumbImg = thumbObj.GetComponent<Image>();
                thumbImg.type = Image.Type.Simple;
                thumbImg.preserveAspect = false;
                if (p1Sprites[idx] != null) thumbImg.sprite = p1Sprites[idx];
                thumbImg.color = Color.white;
                // Initially inactive if slot is empty
                thumbObj.SetActive(false);

                // Title Text (BLKCHCRY SDF) - defaults to "Empty Slot"
                GameObject cTitleObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                cTitleObj.transform.SetParent(cardObj.transform, false);
                RectTransform ctRect = cTitleObj.GetComponent<RectTransform>();
                ctRect.anchorMin = new Vector2(0.05f, 0.35f);
                ctRect.anchorMax = new Vector2(0.95f, 0.65f);
                ctRect.offsetMin = Vector2.zero;
                ctRect.offsetMax = Vector2.zero;

                TextMeshProUGUI ctTmp = cTitleObj.GetComponent<TextMeshProUGUI>();
                ctTmp.text = "Empty Slot";
                ctTmp.font = menuFont;
                ctTmp.fontSize = 36;
                ctTmp.fontStyle = FontStyles.Bold;
                ctTmp.alignment = TextAlignmentOptions.Center;
                ctTmp.color = new Color(0.55f, 0.70f, 0.88f, 0.65f);

                // Timestamp Text
                GameObject cTimeObj = new GameObject("TimeText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                cTimeObj.transform.SetParent(cardObj.transform, false);
                RectTransform ctimeRect = cTimeObj.GetComponent<RectTransform>();
                ctimeRect.anchorMin = new Vector2(0.04f, 0.01f);
                ctimeRect.anchorMax = new Vector2(0.96f, 0.12f);
                ctimeRect.offsetMin = Vector2.zero;
                ctimeRect.offsetMax = Vector2.zero;

                TextMeshProUGUI ctimeTmp = cTimeObj.GetComponent<TextMeshProUGUI>();
                ctimeTmp.text = "";
                ctimeTmp.font = menuFont;
                ctimeTmp.fontSize = 18;
                ctimeTmp.alignment = TextAlignmentOptions.Center;
                ctimeTmp.color = new Color(0.55f, 0.72f, 0.90f, 0.85f);
                cTimeObj.SetActive(false);

                // Button interaction colors
                Button btn = cardObj.GetComponent<Button>();
                ColorBlock cb = btn.colors;
                cb.normalColor = Color.white;
                cb.highlightedColor = new Color(1.25f, 1.35f, 1.55f, 1f);
                cb.pressedColor = new Color(0.7f, 0.85f, 1f, 1f);
                cb.fadeDuration = 0.1f;
                btn.colors = cb;

                // Audio
                AudioSource cardAudio = cardObj.AddComponent<AudioSource>();
                cardAudio.playOnAwake = false;
                ButtonFunction cardFunc = cardObj.AddComponent<ButtonFunction>();
                cardFunc.mySound = cardAudio;
                cardFunc.hoverSound = hoverClip;
                cardFunc.clickSound = clickClip;
                UnityEventTools.AddPersistentListener(btn.onClick, cardFunc.ClickSound);

                cards[idx] = btn;
                cardImgs[idx] = thumbImg;
                cardTitles[idx] = ctTmp;
                cardTimes[idx] = ctimeTmp;
            }
        }

        // 8. Back Button (Main Menu Screen Styling & Font!)
        GameObject backObj = DefaultControls.CreateButton(uiRes);
        backObj.name = "BackButton";
        backObj.transform.SetParent(panelObj.transform, false);
        RectTransform backRect = backObj.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.05f, 0.04f);
        backRect.anchorMax = new Vector2(0.05f, 0.04f);
        backRect.pivot = new Vector2(0f, 0.5f);
        backRect.anchoredPosition = Vector2.zero;
        backRect.sizeDelta = new Vector2(280f, 65f);

        Image backImg = backObj.GetComponent<Image>();
        backImg.color = new Color(0.12f, 0.24f, 0.42f, 0.85f);

        Transform oldBackText = backObj.transform.Find("Text");
        if (oldBackText != null) Object.DestroyImmediate(oldBackText.gameObject);

        GameObject backTextObj = new GameObject("Text (TMP)", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        backTextObj.transform.SetParent(backObj.transform, false);
        RectTransform btRect = backTextObj.GetComponent<RectTransform>();
        btRect.anchorMin = Vector2.zero;
        btRect.anchorMax = Vector2.one;
        btRect.offsetMin = Vector2.zero;
        btRect.offsetMax = Vector2.zero;

        TextMeshProUGUI backTmp = backTextObj.GetComponent<TextMeshProUGUI>();
        backTmp.text = "Back";
        backTmp.font = menuFont;
        backTmp.fontSize = 52;
        backTmp.fontStyle = FontStyles.Bold;
        backTmp.alignment = TextAlignmentOptions.Center;
        backTmp.color = new Color(0.9f, 0.96f, 1f, 1f);

        Button backBtn = backObj.GetComponent<Button>();
        ColorBlock bcb = backBtn.colors;
        bcb.normalColor = Color.white;
        bcb.highlightedColor = new Color(1.3f, 1.4f, 1.6f, 1f);
        bcb.pressedColor = new Color(0.7f, 0.85f, 1f, 1f);
        backBtn.colors = bcb;

        AudioSource backAudio = backObj.AddComponent<AudioSource>();
        backAudio.playOnAwake = false;
        ButtonFunction backFunc = backObj.AddComponent<ButtonFunction>();
        backFunc.mySound = backAudio;
        backFunc.hoverSound = hoverClip;
        backFunc.clickSound = clickClip;
        UnityEventTools.AddPersistentListener(backBtn.onClick, backFunc.ClickSound);
        UnityEventTools.AddPersistentListener(backBtn.onClick, archiveMenu.CloseArchive);

        // 9. Bottom Pagination Bar (Centered under cards, 2 Pages Only)
        GameObject pagObj = new GameObject("PaginationBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        pagObj.transform.SetParent(panelObj.transform, false);
        RectTransform pagRect = pagObj.GetComponent<RectTransform>();
        pagRect.anchorMin = new Vector2(0.5f, 0.04f);
        pagRect.anchorMax = new Vector2(0.5f, 0.04f);
        pagRect.pivot = new Vector2(0.5f, 0.5f);
        pagRect.anchoredPosition = Vector2.zero;
        pagRect.sizeDelta = new Vector2(360f, 62f);

        Image pagImg = pagObj.GetComponent<Image>();
        pagImg.color = new Color(0.05f, 0.12f, 0.22f, 0.85f);

        Button CreatePagButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, out TextMeshProUGUI textTmp, int fontSize = 42)
        {
            GameObject bObj = DefaultControls.CreateButton(uiRes);
            bObj.name = name;
            bObj.transform.SetParent(parent, false);
            RectTransform br = bObj.GetComponent<RectTransform>();
            br.anchorMin = new Vector2(0.5f, 0.5f);
            br.anchorMax = new Vector2(0.5f, 0.5f);
            br.pivot = new Vector2(0.5f, 0.5f);
            br.anchoredPosition = pos;
            br.sizeDelta = size;

            Image img = bObj.GetComponent<Image>();
            img.color = new Color(0.1f, 0.2f, 0.35f, 0.4f);

            Transform oldT = bObj.transform.Find("Text");
            if (oldT != null) Object.DestroyImmediate(oldT.gameObject);

            GameObject to = new GameObject("Text (TMP)", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            to.transform.SetParent(bObj.transform, false);
            RectTransform tr = to.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;

            textTmp = to.GetComponent<TextMeshProUGUI>();
            textTmp.text = label;
            textTmp.font = menuFont;
            textTmp.fontSize = fontSize;
            textTmp.fontStyle = FontStyles.Bold;
            textTmp.alignment = TextAlignmentOptions.Center;
            textTmp.color = new Color(0.65f, 0.8f, 0.95f, 0.85f);

            Button b = bObj.GetComponent<Button>();
            ColorBlock pcb = b.colors;
            pcb.normalColor = Color.white;
            pcb.highlightedColor = new Color(1.3f, 1.4f, 1.6f, 1f);
            pcb.pressedColor = new Color(0.7f, 0.85f, 1f, 1f);
            b.colors = pcb;

            AudioSource a = bObj.AddComponent<AudioSource>();
            a.playOnAwake = false;
            ButtonFunction f = bObj.AddComponent<ButtonFunction>();
            f.mySound = a;
            f.hoverSound = hoverClip;
            f.clickSound = clickClip;
            UnityEventTools.AddPersistentListener(b.onClick, f.ClickSound);

            return b;
        }

        TextMeshProUGUI prevTmp, p1Tmp, p2Tmp, nextTmp;
        Button prevBtn = CreatePagButton(pagObj.transform, "PrevPageBtn", "<", new Vector2(-110f, 0), new Vector2(52f, 50f), out prevTmp, 38);
        Button p1Btn = CreatePagButton(pagObj.transform, "Page1Btn", "1", new Vector2(-38f, 0), new Vector2(65f, 50f), out p1Tmp, 42);
        Button p2Btn = CreatePagButton(pagObj.transform, "Page2Btn", "2", new Vector2(38f, 0), new Vector2(65f, 50f), out p2Tmp, 42);
        Button nextBtn = CreatePagButton(pagObj.transform, "NextPageBtn", ">", new Vector2(110f, 0), new Vector2(52f, 50f), out nextTmp, 38);

        // Highlight Page 1 by default
        p1Tmp.color = new Color(0.25f, 0.88f, 1f, 1f);

        // 10. Toast Notification Banner
        GameObject notifObj = new GameObject("NotificationText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        notifObj.transform.SetParent(panelObj.transform, false);
        RectTransform notifRect = notifObj.GetComponent<RectTransform>();
        notifRect.anchorMin = new Vector2(0.5f, 0.09f);
        notifRect.anchorMax = new Vector2(0.5f, 0.09f);
        notifRect.pivot = new Vector2(0.5f, 0.5f);
        notifRect.anchoredPosition = Vector2.zero;
        notifRect.sizeDelta = new Vector2(800f, 50f);

        TextMeshProUGUI notifTmp = notifObj.GetComponent<TextMeshProUGUI>();
        notifTmp.text = "";
        notifTmp.font = menuFont;
        notifTmp.fontSize = 32;
        notifTmp.fontStyle = FontStyles.Bold;
        notifTmp.alignment = TextAlignmentOptions.Center;
        notifTmp.color = new Color(0.25f, 0.88f, 1f, 1f);
        notifObj.SetActive(false);

        // 11. Wire SaveArchiveMenu SerializedProperties
        SerializedObject so = new SerializedObject(archiveMenu);
        so.Update();

        so.FindProperty("archivePanel").objectReferenceValue = panelObj;
        so.FindProperty("mainMenuPanel").objectReferenceValue = mainMenu;
        so.FindProperty("archiveCanvasGroup").objectReferenceValue = archiveCg;
        so.FindProperty("mainMenuCanvasGroup").objectReferenceValue = mainMenuCg;
        so.FindProperty("archiveTitleText").objectReferenceValue = titleTmp;
        so.FindProperty("pageSubtitleText").objectReferenceValue = subTmp;
        so.FindProperty("page1Button").objectReferenceValue = p1Btn;
        so.FindProperty("page2Button").objectReferenceValue = p2Btn;
        so.FindProperty("page1Text").objectReferenceValue = p1Tmp;
        so.FindProperty("page2Text").objectReferenceValue = p2Tmp;
        so.FindProperty("prevPageButton").objectReferenceValue = prevBtn;
        so.FindProperty("nextPageButton").objectReferenceValue = nextBtn;
        so.FindProperty("backButton").objectReferenceValue = backBtn;
        so.FindProperty("isSaveMode").boolValue = saveMode;
        so.FindProperty("audioSource").objectReferenceValue = panelAudio;
        so.FindProperty("clickSound").objectReferenceValue = clickClip;
        so.FindProperty("hoverSound").objectReferenceValue = hoverClip;
        so.FindProperty("notificationText").objectReferenceValue = notifTmp;

        SerializedProperty cardBtnsProp = so.FindProperty("cardButtons");
        SerializedProperty cardImgsProp = so.FindProperty("cardImages");
        SerializedProperty cardTitlesProp = so.FindProperty("cardTitleTexts");
        SerializedProperty cardTimesProp = so.FindProperty("cardTimeTexts");

        for (int i = 0; i < 6; i++)
        {
            cardBtnsProp.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            cardImgsProp.GetArrayElementAtIndex(i).objectReferenceValue = cardImgs[i];
            cardTitlesProp.GetArrayElementAtIndex(i).objectReferenceValue = cardTitles[i];
            cardTimesProp.GetArrayElementAtIndex(i).objectReferenceValue = cardTimes[i];
        }

        SerializedProperty p1SpritesProp = so.FindProperty("page1Sprites");
        for (int i = 0; i < 6; i++)
        {
            p1SpritesProp.GetArrayElementAtIndex(i).objectReferenceValue = p1Sprites[i];
        }

        SerializedProperty p2SpritesProp = so.FindProperty("page2Sprites");
        for (int i = 0; i < 6; i++)
        {
            p2SpritesProp.GetArrayElementAtIndex(i).objectReferenceValue = p2Sprites[i];
        }

        so.ApplyModifiedProperties();

        // Hide Archive Panel by default so Main Menu / Scene is displayed first
        panelObj.SetActive(false);

        EditorUtility.SetDirty(panelObj);
        return archiveMenu;
    }
}
#endif
