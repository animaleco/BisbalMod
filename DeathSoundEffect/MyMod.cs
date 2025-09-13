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
    string pathSong = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Hollow Knight Silksong\\Sounds\\Cry.ogg";

    public void Update()
    {
        // Check if is playing
        if (IsPlaying())
            if (!isPlayerConfigured)
                ConfigurePlayer();
    }

    public void ConfigurePlayer()
    {
        Logger.LogInfo("Entrando en ConfigurePlayer");
        player = HeroController.instance;

        aSource = player.gameObject.AddComponent<AudioSource>();
        aSource.loop = false;
        aSource.volume = 1f;
        aSource.mute = false;
        StartCoroutine(LoadSong(pathSong, aSource));
        player.OnDeath += PlaySong;
        isPlayerConfigured = true;
    }

    public bool IsPlaying()
    {
        if (GameManager._instance.GameState == GlobalEnums.GameState.PLAYING)
            return true;

        return false;
    }

    public IEnumerator LoadSong(string path, AudioSource a)
    {
        Logger.LogInfo("Entrando en LoadSong");
        using (var request = UnityWebRequestMultimedia.GetAudioClip("file:///" + path.Replace("\\", "/"), AudioType.UNKNOWN))
        {
            yield return request.SendWebRequest();
            song = DownloadHandlerAudioClip.GetContent(request);
            a.clip = song;
            if (song == null)
                Logger.LogInfo("song null");
            else
                Logger.LogInfo("song no null");
        }
    }

    public void PlaySong()
    {
        Logger.LogInfo("Entrando en PlaySong");
        aSource.Play();
    }

}
