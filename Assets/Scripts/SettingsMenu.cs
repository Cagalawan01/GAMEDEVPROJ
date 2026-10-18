using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsMenu : MonoBehaviour
{
    [Header("BGM Volume Settings")]
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private TextMeshProUGUI volumePercentText;

    [Header("Sound Volume Settings")]
    [SerializeField] private AudioSource soundAudioSource;
    [SerializeField] private Slider soundSlider;
    [SerializeField] private TextMeshProUGUI soundPercentText;
    [SerializeField] private Button soundMinusButton;
    [SerializeField] private Button soundPlusButton;
    [SerializeField] private Button soundMuteButton;
    [SerializeField] private Button sound50Button;
    [SerializeField] private Button sound100Button;
    [SerializeField] private AudioClip previewClip;

    [Header("UI Panels")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject mainMenuPanel;

    [Header("Transition Settings")]
    [SerializeField] private CanvasGroup settingsCanvasGroup;
    [SerializeField] private CanvasGroup mainMenuCanvasGroup;
    [SerializeField] private float transitionDuration = 0.28f;

    private Coroutine transitionCoroutine;

    private const string BGM_PREF_KEY = "BGM_Volume";
    private const string SFX_PREF_KEY = "SFX_Volume";

    public static float GlobalSoundVolume { get; private set; } = 0.8f;

    private void Awake()
    {
        InitializeBgmAudio();
        InitializeSoundAudio();
        EnsureCanvasGroups();
    }

    private void EnsureCanvasGroups()
    {
        if (settingsCanvasGroup == null)
        {
            GameObject target = settingsPanel != null ? settingsPanel : gameObject;
            settingsCanvasGroup = target.GetComponent<CanvasGroup>();
            if (settingsCanvasGroup == null) settingsCanvasGroup = target.AddComponent<CanvasGroup>();
        }

        if (mainMenuCanvasGroup == null && mainMenuPanel != null)
        {
            mainMenuCanvasGroup = mainMenuPanel.GetComponent<CanvasGroup>();
            if (mainMenuCanvasGroup == null) mainMenuCanvasGroup = mainMenuPanel.AddComponent<CanvasGroup>();
        }
    }

    private void Update()
    {
        if (settingsPanel != null && settingsPanel.activeInHierarchy && settingsCanvasGroup != null && settingsCanvasGroup.alpha > 0.5f)
        {
            bool escapePressed = false;
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                escapePressed = true;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                escapePressed = true;
            }
#endif
            if (escapePressed)
            {
                CloseSettings();
            }
        }
    }



    private void InitializeBgmAudio()
    {
        if (bgmAudioSource == null)
        {
            GameObject bgmObj = GameObject.Find("BGM");
            if (bgmObj != null)
            {
                bgmAudioSource = bgmObj.GetComponent<AudioSource>();
            }
        }

        float initialVolume = bgmAudioSource != null ? bgmAudioSource.volume : 0.8f;
        float savedVolume = PlayerPrefs.GetFloat(BGM_PREF_KEY, initialVolume);

        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = savedVolume;
        }

        if (bgmSlider != null)
        {
            bgmSlider.minValue = 0f;
            bgmSlider.maxValue = 1f;
            bgmSlider.value = savedVolume;
            bgmSlider.onValueChanged.RemoveAllListeners();
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        }

        UpdateBgmPercentText(savedVolume);
    }

    private void InitializeSoundAudio()
    {
        if (soundAudioSource == null)
        {
            soundAudioSource = GetComponent<AudioSource>();
            if (soundAudioSource == null)
            {
                soundAudioSource = gameObject.AddComponent<AudioSource>();
                soundAudioSource.playOnAwake = false;
            }
        }

        float savedSoundVol = PlayerPrefs.GetFloat(SFX_PREF_KEY, 0.8f);
        GlobalSoundVolume = savedSoundVol;

        if (soundAudioSource != null)
        {
            soundAudioSource.volume = savedSoundVol;
        }

        if (soundSlider != null)
        {
            soundSlider.minValue = 0f;
            soundSlider.maxValue = 1f;
            soundSlider.value = savedSoundVol;
            soundSlider.onValueChanged.RemoveAllListeners();
            soundSlider.onValueChanged.AddListener(SetSoundVolumeFromSlider);
        }

        if (soundMinusButton != null)
        {
            soundMinusButton.onClick.RemoveAllListeners();
            soundMinusButton.onClick.AddListener(() => ChangeSoundVolumeBy(-0.1f));
        }

        if (soundPlusButton != null)
        {
            soundPlusButton.onClick.RemoveAllListeners();
            soundPlusButton.onClick.AddListener(() => ChangeSoundVolumeBy(0.1f));
        }

        if (soundMuteButton != null)
        {
            soundMuteButton.onClick.RemoveAllListeners();
            soundMuteButton.onClick.AddListener(MuteSound);
        }

        if (sound50Button != null)
        {
            sound50Button.onClick.RemoveAllListeners();
            sound50Button.onClick.AddListener(SetSound50);
        }

        if (sound100Button != null)
        {
            sound100Button.onClick.RemoveAllListeners();
            sound100Button.onClick.AddListener(SetSound100);
        }

        UpdateSoundPercentText(savedSoundVol);
    }

    public void SetBGMVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = volume;
        }

        PlayerPrefs.SetFloat(BGM_PREF_KEY, volume);
        UpdateBgmPercentText(volume);
    }

    public void SetSoundVolume(float volume, bool playPreview = true)
    {
        volume = Mathf.Clamp01(Mathf.Round(volume * 100f) / 100f);
        GlobalSoundVolume = volume;
        PlayerPrefs.SetFloat(SFX_PREF_KEY, volume);

        if (soundSlider != null && !Mathf.Approximately(soundSlider.value, volume))
        {
            soundSlider.SetValueWithoutNotify(volume);
        }

        if (soundAudioSource != null)
        {
            soundAudioSource.volume = volume;
        }

        UpdateSoundPercentText(volume);
        ApplyVolumeToAllButtonSounds(volume);

        if (playPreview)
        {
            PlayPreviewSound();
        }
    }

    public void SetSoundVolumeFromSlider(float volume)
    {
        SetSoundVolume(volume, playPreview: false);
    }

    public void ChangeSoundVolumeBy(float delta)
    {
        float current = PlayerPrefs.GetFloat(SFX_PREF_KEY, 0.8f);
        float newVol = Mathf.Clamp01(current + delta);
        SetSoundVolume(newVol, playPreview: true);
    }

    public void MuteSound()
    {
        SetSoundVolume(0f, playPreview: false);
    }

    public void SetSound50()
    {
        SetSoundVolume(0.5f, playPreview: true);
    }

    public void SetSound100()
    {
        SetSoundVolume(1.0f, playPreview: true);
    }

    public void PlayPreviewSound()
    {
        if (previewClip != null && soundAudioSource != null && soundAudioSource.isActiveAndEnabled)
        {
            soundAudioSource.PlayOneShot(previewClip, GlobalSoundVolume);
        }
    }

    private void ApplyVolumeToAllButtonSounds(float volume)
    {
        ButtonFunction[] allButtonFuncs = Object.FindObjectsByType<ButtonFunction>(FindObjectsInactive.Include);
        foreach (var bf in allButtonFuncs)
        {
            if (bf != null && bf.mySound != null)
            {
                bf.mySound.volume = volume;
            }
        }
    }

    private void UpdateBgmPercentText(float volume)
    {
        if (volumePercentText != null)
        {
            volumePercentText.text = Mathf.RoundToInt(volume * 100f) + "%";
        }
    }

    private void UpdateSoundPercentText(float volume)
    {
        if (soundPercentText != null)
        {
            soundPercentText.text = Mathf.RoundToInt(volume * 100f) + "%";
        }
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
        else
        {
            gameObject.SetActive(true);
        }

        EnsureCanvasGroups();

        if (settingsCanvasGroup != null)
        {
            settingsCanvasGroup.alpha = 0f;
            settingsCanvasGroup.interactable = false;
            settingsCanvasGroup.blocksRaycasts = false;
        }

        // Refresh BGM UI
        float currentBgm = PlayerPrefs.GetFloat(BGM_PREF_KEY, bgmAudioSource != null ? bgmAudioSource.volume : 0.8f);
        if (bgmSlider != null)
        {
            bgmSlider.SetValueWithoutNotify(currentBgm);
        }
        UpdateBgmPercentText(currentBgm);

        // Refresh Sound UI
        float currentSound = PlayerPrefs.GetFloat(SFX_PREF_KEY, 0.8f);
        GlobalSoundVolume = currentSound;
        if (soundSlider != null)
        {
            soundSlider.SetValueWithoutNotify(currentSound);
        }
        UpdateSoundPercentText(currentSound);

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(TransitionToSettings());
    }

    public void CloseSettings()
    {
        EnsureCanvasGroups();
        PlayerPrefs.Save();

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(TransitionToMainMenu());
    }

    private IEnumerator TransitionToSettings()
    {
        if (mainMenuCanvasGroup != null)
        {
            mainMenuCanvasGroup.interactable = false;
            mainMenuCanvasGroup.blocksRaycasts = false;
        }

        if (settingsCanvasGroup != null)
        {
            settingsCanvasGroup.alpha = 0f;
            settingsCanvasGroup.interactable = false;
            settingsCanvasGroup.blocksRaycasts = false;
        }

        float elapsed = 0f;
        float startMenuAlpha = (mainMenuCanvasGroup != null) ? mainMenuCanvasGroup.alpha : 1f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float smoothT = t * t * (3f - 2f * t);

            if (mainMenuCanvasGroup != null)
            {
                mainMenuCanvasGroup.alpha = Mathf.Lerp(startMenuAlpha, 0f, smoothT);
            }

            if (settingsCanvasGroup != null)
            {
                settingsCanvasGroup.alpha = Mathf.Lerp(0f, 1f, smoothT);
            }

            yield return null;
        }

        if (mainMenuCanvasGroup != null)
        {
            mainMenuCanvasGroup.alpha = 0f;
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }

        if (settingsCanvasGroup != null)
        {
            settingsCanvasGroup.alpha = 1f;
            settingsCanvasGroup.interactable = true;
            settingsCanvasGroup.blocksRaycasts = true;
        }

        transitionCoroutine = null;
    }

    private IEnumerator TransitionToMainMenu()
    {
        if (settingsCanvasGroup != null)
        {
            settingsCanvasGroup.interactable = false;
            settingsCanvasGroup.blocksRaycasts = false;
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
            if (mainMenuCanvasGroup != null)
            {
                mainMenuCanvasGroup.alpha = 0f;
                mainMenuCanvasGroup.interactable = false;
                mainMenuCanvasGroup.blocksRaycasts = false;
            }
        }

        float elapsed = 0f;
        float startSettingsAlpha = (settingsCanvasGroup != null) ? settingsCanvasGroup.alpha : 1f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float smoothT = t * t * (3f - 2f * t);

            if (settingsCanvasGroup != null)
            {
                settingsCanvasGroup.alpha = Mathf.Lerp(startSettingsAlpha, 0f, smoothT);
            }

            if (mainMenuCanvasGroup != null)
            {
                mainMenuCanvasGroup.alpha = Mathf.Lerp(0f, 1f, smoothT);
            }

            yield return null;
        }

        if (settingsCanvasGroup != null)
        {
            settingsCanvasGroup.alpha = 0f;
            settingsCanvasGroup.interactable = false;
            settingsCanvasGroup.blocksRaycasts = false;
        }

        if (mainMenuCanvasGroup != null)
        {
            mainMenuCanvasGroup.alpha = 1f;
            mainMenuCanvasGroup.interactable = true;
            mainMenuCanvasGroup.blocksRaycasts = true;
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        transitionCoroutine = null;
    }
}

