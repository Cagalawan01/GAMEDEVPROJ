using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsMenu : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private TextMeshProUGUI volumePercentText;

    [Header("Resolution Settings")]
    [SerializeField] private Button normalResButton;
    [SerializeField] private Button lowResButton;
    [SerializeField] private TextMeshProUGUI normalResText;
    [SerializeField] private TextMeshProUGUI lowResText;

    [Header("UI Panels")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject mainMenuPanel;

    private const string BGM_PREF_KEY = "BGM_Volume";
    private const string RES_PREF_KEY = "Game_Resolution_Mode"; // 0 = Normal, 1 = Low

    // Button colors for selected vs unselected resolution
    private readonly Color activeBtnColor = new Color(0.2f, 0.6f, 0.95f, 1f);
    private readonly Color inactiveBtnColor = new Color(0.12f, 0.18f, 0.28f, 0.85f);
    private readonly Color activeTextColor = Color.white;
    private readonly Color inactiveTextColor = new Color(0.7f, 0.75f, 0.85f, 0.8f);

    private void Awake()
    {
        InitializeAudio();
        InitializeResolution();
    }

    private void InitializeAudio()
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

        UpdatePercentText(savedVolume);
    }

    private void InitializeResolution()
    {
        // 0 = Normal, 1 = Low
        int savedMode = PlayerPrefs.GetInt(RES_PREF_KEY, 0);

        if (normalResButton != null)
        {
            normalResButton.onClick.RemoveAllListeners();
            normalResButton.onClick.AddListener(() => SetResolutionMode(0));
        }

        if (lowResButton != null)
        {
            lowResButton.onClick.RemoveAllListeners();
            lowResButton.onClick.AddListener(() => SetResolutionMode(1));
        }

        ApplyResolution(savedMode, false);
    }

    public void SetResolutionMode(int mode)
    {
        ApplyResolution(mode, true);
    }

    public void SetNormalResolution()
    {
        SetResolutionMode(0);
    }

    public void SetLowResolution()
    {
        SetResolutionMode(1);
    }

    private void ApplyResolution(int mode, bool save)
    {
        bool isLow = (mode == 1);

        int nativeWidth = Screen.currentResolution.width > 0 ? Screen.currentResolution.width : 1366;
        int nativeHeight = Screen.currentResolution.height > 0 ? Screen.currentResolution.height : 768;

        if (isLow)
        {
            // Low Resolution: scaled down to ~67% (e.g. 720p or lower for performance)
            int lowWidth = Mathf.Max(640, Mathf.RoundToInt(nativeWidth * 0.67f));
            int lowHeight = Mathf.Max(480, Mathf.RoundToInt(nativeHeight * 0.67f));
            Screen.SetResolution(lowWidth, lowHeight, Screen.fullScreenMode);
            QualitySettings.resolutionScalingFixedDPIFactor = 0.67f;
        }
        else
        {
            // Normal Resolution: native display resolution
            Screen.SetResolution(nativeWidth, nativeHeight, Screen.fullScreenMode);
            QualitySettings.resolutionScalingFixedDPIFactor = 1.0f;
        }

        // Update button visual states
        UpdateResolutionButtonsUI(isLow);

        if (save)
        {
            PlayerPrefs.SetInt(RES_PREF_KEY, mode);
            PlayerPrefs.Save();
        }
    }

    private void UpdateResolutionButtonsUI(bool isLow)
    {
        if (normalResButton != null)
        {
            Image img = normalResButton.GetComponent<Image>();
            if (img != null) img.color = !isLow ? activeBtnColor : inactiveBtnColor;
        }

        if (lowResButton != null)
        {
            Image img = lowResButton.GetComponent<Image>();
            if (img != null) img.color = isLow ? activeBtnColor : inactiveBtnColor;
        }

        if (normalResText != null)
        {
            normalResText.color = !isLow ? activeTextColor : inactiveTextColor;
        }

        if (lowResText != null)
        {
            lowResText.color = isLow ? activeTextColor : inactiveTextColor;
        }
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }

        // Refresh slider to current volume
        if (bgmSlider != null && bgmAudioSource != null)
        {
            bgmSlider.value = bgmAudioSource.volume;
            UpdatePercentText(bgmSlider.value);
        }

        // Refresh resolution buttons state
        int savedMode = PlayerPrefs.GetInt(RES_PREF_KEY, 0);
        UpdateResolutionButtonsUI(savedMode == 1);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }

        PlayerPrefs.Save();
    }

    public void SetBGMVolume(float volume)
    {
        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = volume;
        }

        PlayerPrefs.SetFloat(BGM_PREF_KEY, volume);
        UpdatePercentText(volume);
    }

    private void UpdatePercentText(float volume)
    {
        if (volumePercentText != null)
        {
            volumePercentText.text = Mathf.RoundToInt(volume * 100f) + "%";
        }
    }
}
