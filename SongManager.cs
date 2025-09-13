using System.Collections;
using System;
using UnityEngine;
using UnityEngine.Networking;
using BepInEx.Logging;

public class CoroutineRequest
{
  public IEnumerator coroutine;
  public Action<bool> onComplete;

  public CoroutineRequest(IEnumerator coroutine, Action<bool> onComplete = null)
  {
    this.coroutine = coroutine;
    this.onComplete = onComplete;
  }
}

public class SongManager
{
  private AudioSource song;
  private Transform playerTransform;
  private ManualLogSource logger;
  private CoroutineRequest pendingCoroutine;
  private bool playerReady = false;
  private bool needsReload = false;
  private bool isMoving = false;
  private bool wasPreviouslyMoving = false;
  private bool isFading = false;
  private float lastPosition;
  private float fadeSpeed = 2.0f;
  private float maxVolume = 1.0f;
  private string currentSongPath = "";

  public SongManager(ManualLogSource logger)
  {
    this.logger = logger;
  }

  public void Update()
  {
    if (!playerReady)
    {
      InitPlayerAndSong();
      return;
    }

    if (needsReload && song != null)
    {
      pendingCoroutine = new CoroutineRequest(ReloadSong());
      needsReload = false;
      return;
    }

    if (song != null && song.clip != null)
    {
      isMoving = HasMoved(playerTransform);

      if (isMoving != wasPreviouslyMoving)
      {
        HandleMovementChange();
      }

      wasPreviouslyMoving = isMoving;
    }
  }

  public void RequestReload(string newSongPath)
  {
    if (newSongPath != currentSongPath)
    {
      currentSongPath = newSongPath;
      needsReload = true;
    }
  }

  public CoroutineRequest GetPendingCoroutine()
  {
    return pendingCoroutine;
  }

  public void ClearPendingCoroutine()
  {
    pendingCoroutine = null;
  }

  private void InitPlayerAndSong()
  {
    if (HeroController.instance == null) return;

    playerTransform = HeroController.instance.transform;
    lastPosition = playerTransform.position.x;
    song = playerTransform.gameObject.AddComponent<AudioSource>();
    song.loop = true;
    song.volume = 0f;
    song.mute = false;

    if (!string.IsNullOrEmpty(currentSongPath))
    {
      pendingCoroutine = new CoroutineRequest(LoadInitialSong());
    }

    logger.LogInfo("HeroController instance ready and AudioSource created");
    playerReady = true;
  }

  private void HandleMovementChange()
  {
    if (isMoving)
    {
      if (!song.isPlaying)
      {
        song.Play();
        logger.LogInfo("Song Started");
      }
      StartFade(maxVolume);
    }
    else
    {
      StartFade(0f);
    }
  }

  private void StartFade(float targetVolume)
  {
    if (isFading) return;
    
    pendingCoroutine = new CoroutineRequest(FadeCoroutine(targetVolume));
  }

  private IEnumerator FadeCoroutine(float targetVolume)
  {
    isFading = true;
    float startVolume = song.volume;
    float elapsed = 0f;
    float duration = Mathf.Abs(targetVolume - startVolume) / fadeSpeed;

    while (elapsed < duration)
    {
      elapsed += Time.deltaTime;
      float progress = elapsed / duration;
      song.volume = Mathf.Lerp(startVolume, targetVolume, progress);
      yield return null;
    }

    song.volume = targetVolume;
    isFading = false;
  }

  private IEnumerator LoadInitialSong()
  {
    yield return SetAudioClip(currentSongPath);
  }

  private IEnumerator ReloadSong()
  {
    logger.LogInfo("Reloading song...");

    if (song != null)
    {
      bool wasPlaying = song.isPlaying;
      song.Stop();
      song.clip = null;

      yield return SetAudioClip(currentSongPath);

      if (wasPlaying && song != null && song.clip != null)
      {
        song.Play();
        song.volume = isMoving ? maxVolume : 0f;
      }
    }
  }

  private bool HasMoved(Transform player)
  {
    float distance = Math.Abs(lastPosition - player.position.x);
    bool hasMoved = distance > 0.01f;
    UpdatePosition();
    return hasMoved;
  }

  private IEnumerator SetAudioClip(string path)
  {
    if (string.IsNullOrEmpty(path))
    {
      logger.LogWarning("Path empty: song cannot be loaded");
      yield break;
    }

    if (!System.IO.File.Exists(path))
    {
      logger.LogError("File not found: " + path);
      yield break;
    }

    string url = "file:///" + path.Replace("\\", "/");
    using (var www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.OGGVORBIS))
    {
      yield return www.SendWebRequest();

      if (www.result != UnityWebRequest.Result.Success)
      {
        logger.LogError("Error loading song: " + www.error);
      }
      else
      {
        AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
        if (song != null)
        {
          song.clip = clip;
          song.Play();
          song.volume = 0f;
          logger.LogInfo("Song loaded successfully: " + path);
        }
      }
    }
  }

  private void UpdatePosition()
  {
    lastPosition = playerTransform.position.x;
  }
}