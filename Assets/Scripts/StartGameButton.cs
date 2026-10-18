using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class StartGameButton : MonoBehaviour
{
    [SerializeField] private string targetScene = "OpeningScene";
    [SerializeField] private float fadeDuration = 0.8f;
    [SerializeField] private AudioClip startSound;

    private Button button;
    private AudioSource audioSource;

    private void Awake()
    {
        button = GetComponent<Button>();
        audioSource = GetComponent<AudioSource>();

        if (string.IsNullOrEmpty(targetScene) || targetScene == "SchoolScene")
        {
            targetScene = "OpeningScene";
        }

        if (button != null)
        {
            button.onClick.RemoveListener(OnStartClicked);
            button.onClick.AddListener(OnStartClicked);
        }
    }

    public void OnStartClicked()
    {
        if (string.IsNullOrEmpty(targetScene) || targetScene == "SchoolScene")
        {
            targetScene = "OpeningScene";
        }

        if (startSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(startSound, SettingsMenu.GlobalSoundVolume);
        }
        else
        {
            ButtonFunction bf = GetComponent<ButtonFunction>();
            if (bf != null) bf.ClickSound();
        }

        Debug.Log($"<color=cyan>[StartGameButton] Redirecting from MenuScreen to {targetScene}...</color>");
        SceneTransitionManager.LoadScene(targetScene, fadeDuration);
    }
}
