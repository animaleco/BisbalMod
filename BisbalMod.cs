using System.Collections;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using GlobalEnums;
using GlobalSettings;
using TeamCherry.SharedUtils;
using UnityEngine;
using BepInEx;
using UnityEngine.PlayerLoop;
using GenericVariableExtension;
using HutongGames.PlayMaker.Actions;
using System.Media;
using InControl;
using HutongGames.PlayMaker;
using System.Collections.ObjectModel;
using System.IO;
using UnityEngine.Networking;

[BepInPlugin("com.animaleco.silksong.bisbalmod", "BisbalMod", "1.0.0")]
public sealed class BisbalMod : BaseUnityPlugin
{
  private AudioSource song;
  private HeroController player;
  private float lastPosition;
  private bool playerReady = false;

  public void Update()
  {
    // Inicialización del player y el audio una sola vez
    if (!playerReady && HeroController.instance != null)
    {
      InitPlayerAndSong();
    }

    // Si todo está listo, controlar la reproducción
    if (player != null && song != null && song.clip != null)
    {
      if (HasMoved(player))
      {
        StartPlay(song);
      }
      else
      {
        StopPlay(song);
      }
    }
  }
  private void InitPlayerAndSong()
  {
    player = HeroController.instance;
    lastPosition = player.transform.position.x;

    song = player.gameObject.AddComponent<AudioSource>();
    song.loop = true;
    song.volume = 1f;
    song.mute = false;

    string pathSong = @"C:\Program Files (x86)\Steam\steamapps\common\Hollow Knight Silksong\Mods\Bisbal.ogg";
    StartCoroutine(GetSongCoroutine(pathSong));

    Logger.LogInfo("HeroController.instance listo y AudioSource creado");
    playerReady = true;
  }

  private bool HasMoved(HeroController player)
  {
    float currentPosition = player.transform.position.x;

    if (Mathf.Abs(currentPosition - lastPosition) > 0.01f)
    {
      lastPosition = currentPosition;
      return true;
    }
    return false;
  }


  private AudioSource GetSong(string path)
  {
    byte[] fileData = File.ReadAllBytes(path);

    string url = "file:///" + path.Replace("\\", "/");
    var www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(url, UnityEngine.AudioType.OGGVORBIS);
    var asyncOp = www.SendWebRequest();
    while (!asyncOp.isDone) { }

    AudioClip clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);

    if (song != null)
      song.clip = clip;

    return song;
  }
  private bool StartPlay(AudioSource song)
  {
    if (song != null && song.clip != null && !song.isPlaying)
    {
      song.Play();
      Logger.LogInfo("Song Reproducing");
      return true;
    }
    return false;
  }
  private bool StopPlay(AudioSource song)
  {
    if (song != null && song.isPlaying)
    {
      song.Pause();
      Logger.LogInfo("Song Paused");
      return true;
    }
    return false;
  }
  private bool IsPlaying()
  {
    return song != null && song.isPlaying;
  }
  private IEnumerator GetSongCoroutine(string path)
  {
    string url = "file:///" + path.Replace("\\", "/");
    using (var www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(url, UnityEngine.AudioType.OGGVORBIS))
    {
      yield return www.SendWebRequest();

      if (www.result != UnityWebRequest.Result.Success)
      {
        Logger.LogError("Error al cargar la canción: " + www.error);
      }
      else
      {
        AudioClip clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
        if (song != null)
        {
          song.clip = clip;
          Logger.LogInfo("Canción cargada correctamente: " + path);
        }
      }
    }
  }
}