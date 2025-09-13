using System.Collections;
using System;
using UnityEngine;
using BepInEx;
using UnityEngine.Networking;
using BepInEx.Configuration;
using System.IO;
using GlobalEnums;

namespace MyMod;

[BepInPlugin("com.animaleco.silksong.mymod", "MyMod", "1.0.0")]
public class MyMod : BaseUnityPlugin, IPlayOnDie
{
    private AudioSource soundEffect;
    private HeroController player;
    private bool isPlayerConfigured = false;

    public void Update()
    {
        // Check if is playing
        if (IsPlaying())
            if (!isPlayerConfigured)
                ConfigurePlayer();
    }

    public void ConfigurePlayer()
    {
        player = HeroController.instance;

        soundEffect = player.gameObject.AddComponent<AudioSource>();
        soundEffect.loop = false;
        soundEffect.volume = 0.5f;
        soundEffect.mute = false;
        soundEffect.clip = LoadSong();
        player.OnDeath += PlaySong;
        isPlayerConfigured = true;
    }

    public bool IsPlaying()
    {
        if (GameManager._instance.GameState == GlobalEnums.GameState.PLAYING)
            return true;

        return false;
    }

    public AudioClip LoadSong()
    {
        throw new NotImplementedException();
    }

    public void PlaySong()
    {
        soundEffect.Play();
    }
}
