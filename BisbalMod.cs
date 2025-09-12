using System.Collections;
using System;
using UnityEngine;
using BepInEx;
using System.IO;
using UnityEngine.Networking;

[BepInPlugin("com.animaleco.silksong.bisbalmod", "BisbalMod", "1.0.0")]
public sealed class BisbalMod : BaseUnityPlugin
{
  private AudioSource song;
  private Transform playerTransform;
  private float lastPosition;
  private bool playerReady = false;
 string pathSong = @"C:\Program Files (x86)\Steam\steamapps\common\Hollow Knight Silksong\Mods\Bisbal.ogg";


  public void Start()
  {
    song = GetSong(pathSong);
    song.loop = true;
    song.volume = 1f;
    song.mute = false;
  }
  
  public void Update()
  {
    if (!IsPlaying())
      return;

    if (!playerReady)
      InitPlayerAndSong();

    if (song != null && song.clip != null)
    {
      if (HasMoved(playerTransform))
        PlaySong(song);
      else
        PauseSong(song);
    }
  }
  
  private void InitPlayerAndSong()
  {
    playerTransform = HeroController.instance.transform;
    lastPosition = playerTransform.position.x;

    song = playerTransform.gameObject.AddComponent<AudioSource>();

    StartCoroutine(GetSongCoroutine(pathSong));

    Logger.LogInfo("HeroController.instance listo y AudioSource creado");
    playerReady = true;
  }

  private bool HasMoved(Transform player)
  {
    float distance = Math.Abs(lastPosition - player.position.x);
    bool hasMoved = distance > 0.01f;
    UpdatePosition();
    return hasMoved;
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

  private bool PlaySong(AudioSource song)
  {
    if (song != null && song.clip != null && !song.isPlaying)
    {
      song.Play();
      Logger.LogInfo("Song Reproducing");
      return true;
    }
    return false;
  }

  private bool PauseSong(AudioSource song)
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
    if (GameManager._instance.GameState == GlobalEnums.GameState.PLAYING)
      return true;
    return false;
  }

  private IEnumerator GetSongCoroutine(string path)
  {
    string url = "file:///" + path.Replace("\\", "/");
    using (var www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(url, UnityEngine.AudioType.OGGVORBIS))
    {
      yield return www.SendWebRequest();

      if (www.result != UnityWebRequest.Result.Success)
        Logger.LogError("Error al cargar la canción: " + www.error);

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
  
  private void UpdatePosition()
  {
    lastPosition = playerTransform.position.x;
  }
}