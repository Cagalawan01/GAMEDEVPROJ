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

    [Header("UI Panels")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject mainMenuPanel;

    private const string BGM_PREF_KEY = "BGM_Volume";

    private void Awake()
    {
        // Try to automatically find BGM AudioSource if not assigned
        if (bgmAudioSource == null)
        {
            GameObject bgmObj = GameObject.Find("BGM");
            if (bgmObj != null)
            {
                bgmAudioSource = bgmObj.GetComponent<AudioSource>();
            }
        }

        // Load saved volume (default to current volume or 0.8)
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
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        }

        UpdatePercentText(savedVolume);
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
