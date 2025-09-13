using System.Collections;
using UnityEngine;
using BepInEx;
using BepInEx.Configuration;

[BepInPlugin("com.animaleco.silksong.bisbalmod", "BisbalMod", "1.0.0")]
public sealed class BisbalMod : BaseUnityPlugin
{
  private SongManager songManager;
  private ConfigWatcher configWatcher;
  private ConfigEntry<string> songPath;

  public void Awake()
  {
    songPath = Config.Bind(
        "General",
        "SongPath",
        @"C:\MyMusic\Bisbal.ogg",
        "Absolute path to the music file (.ogg)"
    );

    songManager = new SongManager(Logger);
    configWatcher = new ConfigWatcher(Config, Logger);

    songPath.SettingChanged += (sender, args) =>
    {
      Logger.LogInfo("SongPath changed via BepInEx: reloading song...");
      songManager.RequestReload(songPath.Value);
    };

    configWatcher.OnConfigChanged += (newPath) =>
    {
      Logger.LogInfo($"New path detected: {newPath}");
      songPath.Value = newPath;
      songManager.RequestReload(newPath);
    };

    configWatcher.StartWatching();
  }

  public void Update()
  {
    if (!IsPlaying()) return;
    
    songManager.Update();

    // Handle coroutine requests from SongManager
    var coroutineRequest = songManager.GetPendingCoroutine();
    if (coroutineRequest != null)
    {
      StartCoroutine(coroutineRequest.coroutine);
      songManager.ClearPendingCoroutine();
    }
  }

  private bool IsPlaying()
  {
    return GameManager._instance.GameState == GlobalEnums.GameState.PLAYING;
  }
}
