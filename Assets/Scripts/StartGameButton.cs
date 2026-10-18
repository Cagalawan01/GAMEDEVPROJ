using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class StartGameButton : MonoBehaviour
{
    [SerializeField] private string targetScene = "SchoolScene";
    [SerializeField] private float fadeDuration = 0.6f;
    [SerializeField] private AudioClip startSound;

    private Button button;
    private AudioSource audioSource;

    private void Awake()
    {
        button = GetComponent<Button>();
        audioSource = GetComponent<AudioSource>();

        if (button != null)
        {
            button.onClick.RemoveListener(OnStartClicked);
            button.onClick.AddListener(OnStartClicked);
        }
    }

    public void OnStartClicked()
    {
        if (startSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(startSound, SettingsMenu.GlobalSoundVolume);
        }
        else
        {
            ButtonFunction bf = GetComponent<ButtonFunction>();
            if (bf != null) bf.ClickSound();
        }

        Debug.Log($"<color=pink>[StartGameButton] Starting transition to {targetScene}...</color>");
        SceneTransitionManager.LoadScene(targetScene, fadeDuration);
    }
}
