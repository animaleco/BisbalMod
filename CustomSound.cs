using System.Collections;
using System;
using UnityEngine;
using BepInEx;
using UnityEngine.Networking;
using BepInEx.Configuration;
using System.IO;

[BepInPlugin("com.animaleco.silksong.customsound", "CustomSound", "1.0.0")]
public sealed class CustomSound  : BaseUnityPlugin
{
  private AudioSource song;
  private Transform playerTransform;
  private float lastPosition;
  private bool playerReady = false;
  private ConfigEntry<string> songPath;
  private bool needsReload = false;
  private FileSystemWatcher configWatcher;
  private string configFilePath;
  private string lastKnownSongPath = "";
  private System.DateTime lastConfigChangeTime = System.DateTime.MinValue;
  private const float CONFIG_CHECK_COOLDOWN = 1.0f;

  // Fade variables
  private bool isMoving = false;
  private bool wasPreviouslyMoving = false;
  private float fadeSpeed = 2.0f;
  private float targetVolume = 1.0f;
  private float maxVolume = 1.0f;
  private Coroutine fadeCoroutine;

  public void Awake()
  {
    songPath = Config.Bind(
        "General",
        "SongPath",
        @"C:\MyMusic\Bisbal.ogg",
        "Absolute path to the music file (.ogg)"
    );

    lastKnownSongPath = songPath.Value;

    songPath.SettingChanged += (sender, args) =>
    {
      Logger.LogInfo("SongPath changed via BepInEx: reloading song...");
      lastKnownSongPath = songPath.Value;
      needsReload = true;
    };

    SetupConfigFileWatcher();
  }

  private void SetupConfigFileWatcher()
  {
    try
    {
      configFilePath = Config.ConfigFilePath;
      string configDirectory = Path.GetDirectoryName(configFilePath);
      string configFileName = Path.GetFileName(configFilePath);

      Logger.LogInfo($"Monitoring config file: {configFilePath}");

      configWatcher = new FileSystemWatcher();
      configWatcher.Path = configDirectory;
      configWatcher.Filter = configFileName;
      configWatcher.NotifyFilter = NotifyFilters.LastWrite;

      configWatcher.Changed += OnConfigFileChanged;
      configWatcher.EnableRaisingEvents = true;

      Logger.LogInfo("FileSystemWatcher configured successfully");
    }
    catch (System.Exception ex)
    {
      Logger.LogError($"Error configuring FileSystemWatcher: {ex.Message}");
    }
  }

  private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
  {
    var now = System.DateTime.Now;
    if ((now - lastConfigChangeTime).TotalSeconds < CONFIG_CHECK_COOLDOWN)
      return;
    
    lastConfigChangeTime = now;
    Logger.LogInfo("Config file modified externally");
    StartCoroutine(CheckConfigChangeDelayed());
  }

  private IEnumerator CheckConfigChangeDelayed()
  {
    yield return new WaitForSeconds(0.5f);

    try
    {
      string newSongPath = ReadSongPathFromConfig();

      if (!string.IsNullOrEmpty(newSongPath) && newSongPath != lastKnownSongPath)
      {
        Logger.LogInfo($"New path detected: {newSongPath}");
        songPath.Value = newSongPath;
        lastKnownSongPath = newSongPath;
        needsReload = true;
      }
    }
    catch (System.Exception ex)
    {
      Logger.LogError($"Error reading config: {ex.Message}");
      try
      {
        Config.Reload();
        if (songPath.Value != lastKnownSongPath)
        {
          lastKnownSongPath = songPath.Value;
          needsReload = true;
        }
      }
      catch (System.Exception ex2)
      {
        Logger.LogError($"Error in fallback reload: {ex2.Message}");
      }
    }
  }

  private string ReadSongPathFromConfig()
  {
    try
    {
      if (!File.Exists(configFilePath))
        return null;

      using (var reader = new StreamReader(configFilePath))
      {
        string line;
        bool inGeneralSection = false;

        while ((line = reader.ReadLine()) != null)
        {
          line = line.Trim();

          if (line.Equals("[General]", System.StringComparison.OrdinalIgnoreCase))
          {
            inGeneralSection = true;
            continue;
          }

          if (line.StartsWith("[") && line.EndsWith("]"))
          {
            inGeneralSection = false;
            continue;
          }

          if (inGeneralSection && line.StartsWith("SongPath = "))
          {
            return line.Substring("SongPath = ".Length);
          }
        }
      }
    }
    catch (System.Exception ex)
    {
      Logger.LogError($"Error reading config file: {ex.Message}");
    }

    return null;
  }

  public void Start()
  {
  }

  public void Update()
  {
    if (!IsPlaying()) return;

    if (!playerReady)
      InitPlayerAndSong();

    if (needsReload && playerReady && song != null)
    {
      StartCoroutine(ReloadSong());
      needsReload = false;
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

  private void InitPlayerAndSong()
  {
    playerTransform = HeroController.instance.transform;
    lastPosition = playerTransform.position.x;
    song = playerTransform.gameObject.AddComponent<AudioSource>();
    song.loop = true;
    song.volume = 0f;
    song.mute = false;

    StartCoroutine(SetAudioClip(songPath.Value));
    Logger.LogInfo("HeroController instance ready and AudioSource created");
    playerReady = true;
  }

  private void HandleMovementChange()
  {
    if (isMoving)
    {
      if (!song.isPlaying)
      {
        song.Play();
        Logger.LogInfo("Song Started");
      }
      StartFade(maxVolume);
    }
    else
    {
      StartFade(0f);
    }
  }

  private void StartFade(float targetVol)
  {
    targetVolume = targetVol;

    if (fadeCoroutine != null)
      StopCoroutine(fadeCoroutine);

    fadeCoroutine = StartCoroutine(FadeCoroutine());
  }

  private IEnumerator FadeCoroutine()
  {
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
    fadeCoroutine = null;
  }

  private IEnumerator ReloadSong()
  {
    Logger.LogInfo("Reloading song...");

    if (song != null)
    {
      bool wasPlaying = song.isPlaying;
      song.Stop();
      song.clip = null;

      yield return StartCoroutine(SetAudioClip(songPath.Value));

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

  private bool IsPlaying()
  {
    if (GameManager._instance.GameState == GlobalEnums.GameState.PLAYING)
      return true;
    return false;
  }

  private IEnumerator SetAudioClip(string path)
  {
    if (string.IsNullOrEmpty(path))
    {
      Logger.LogWarning("Path empty: song cannot be loaded");
      yield break;
    }

    if (!System.IO.File.Exists(path))
    {
      Logger.LogError("File not found: " + path);
      yield break;
    }

    string url = "file:///" + path.Replace("\\", "/");
    using (var www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(url, UnityEngine.AudioType.OGGVORBIS))
    {
      yield return www.SendWebRequest();

      if (www.result != UnityWebRequest.Result.Success)
      {
        Logger.LogError("Error loading song: " + www.error);
      }
      else
      {
        AudioClip clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
        if (song != null)
        {
          song.clip = clip;
          song.Play();
          song.volume = 0f;
          Logger.LogInfo("Song loaded successfully: " + path);
        }
      }
    }
  }

  private void UpdatePosition()
  {
    lastPosition = playerTransform.position.x;
  }
}
