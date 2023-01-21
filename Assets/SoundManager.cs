using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SoundManager : NetworkBehaviour
{
    public static SoundManager Instance;

    public AudioSource playSound;

    public AudioSource checkSound;

    public AudioSource canelSound;

    private void Awake()
    {
        if(Instance == null) Instance = this;
    }

    public void PlaySound()
    {
        playSound.Play();
    }

    public void CheckSound()
    {
        checkSound.Play();
    }
    public void CancelSound()
    {
        canelSound.Play();
    }
}
