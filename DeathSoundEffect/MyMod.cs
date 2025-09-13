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
    private AudioClip song;
    private AudioSource aSource;
    private HeroController player;
    private bool isPlayerConfigured = false;
    string pathSong = @"C:\Program Files (x86)\Steam\steamapps\common\Hollow Knight Silksong\Sounds\Cry.ogg";

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

        aSource = player.gameObject.AddComponent<AudioSource>();
        aSource.loop = false;
        aSource.volume = 0.5f;
        aSource.mute = false;
        StartCoroutine(LoadSong(pathSong));
        aSource.clip = song;
        player.OnDeath += PlaySong;
        isPlayerConfigured = true;
    }

    public bool IsPlaying()
    {
        if (GameManager._instance.GameState == GlobalEnums.GameState.PLAYING)
            return true;

        return false;
    }

    public IEnumerator LoadSong(string path)
    {
        using (var request = UnityWebRequestMultimedia.GetAudioClip("file:///" + path.Replace("\\", "/ "), AudioType.UNKNOWN))
        {
            yield return request.SendWebRequest();
            song = DownloadHandlerAudioClip.GetContent(request);
        }
    }

    public void PlaySong()
    {
        aSource.Play();
    }

}
