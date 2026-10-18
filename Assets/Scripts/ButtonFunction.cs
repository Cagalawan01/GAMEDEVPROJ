using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonFunction : MonoBehaviour
{
    public AudioSource mySound;
    public AudioClip hoverSound;
    public AudioClip clickSound;

    private void Awake()
    {
        if (mySound == null)
        {
            mySound = GetComponent<AudioSource>();
        }
    }

    public void HoverSound()
    {
        if (mySound != null && mySound.isActiveAndEnabled && hoverSound != null)
        {
            float sfxVol = PlayerPrefs.GetFloat("SFX_Volume", 0.8f);
            mySound.PlayOneShot(hoverSound, sfxVol);
        }
    }

    public void ClickSound()
    {
        if (mySound != null && mySound.isActiveAndEnabled && clickSound != null)
        {
            float sfxVol = PlayerPrefs.GetFloat("SFX_Volume", 0.8f);
            mySound.PlayOneShot(clickSound, sfxVol);
        }
    }
}
