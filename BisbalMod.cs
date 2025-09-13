using System.Collections;
using System;
using UnityEngine;
using BepInEx;
using UnityEngine.Networking;
using BepInEx.Configuration;
using System.IO;
[BepInPlugin("com.animaleco.silksong.bisbalmod", "BisbalMod", "1.0.0")]
public sealed class BisbalMod : BaseUnityPlugin
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

public void Awake()
{
    songPath = Config.Bind(
        "General",
        "SongPath",
        @"C:\MyMusic\Bisbal.ogg",
        "Absolute path to the music file (.ogg)"
    );

    lastKnownSongPath = songPath.Value;

    songPath.SettingChanged += (sender, args) => {
        Logger.LogInfo("SongPath cambiado via BepInEx: marcando para recargar canción...");
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

        Logger.LogInfo($"Monitoreando archivo de configuración: {configFilePath}");

        configWatcher = new FileSystemWatcher();
        configWatcher.Path = configDirectory;
        configWatcher.Filter = configFileName;
        configWatcher.NotifyFilter = NotifyFilters.LastWrite;

        configWatcher.Changed += OnConfigFileChanged;
        configWatcher.EnableRaisingEvents = true;

        Logger.LogInfo("FileSystemWatcher configurado correctamente");
    }
    catch (System.Exception ex)
    {
        Logger.LogError($"Error configurando FileSystemWatcher: {ex.Message}");
    }
}

private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
{
    var now = System.DateTime.Now;
    if ((now - lastConfigChangeTime).TotalSeconds < CONFIG_CHECK_COOLDOWN)
        return;

    lastConfigChangeTime = now;

    Logger.LogInfo("Archivo de configuración modificado externamente");
    
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
            Logger.LogInfo($"Nueva ruta detectada: {newSongPath}");
            
            songPath.Value = newSongPath;
            lastKnownSongPath = newSongPath;
            needsReload = true;
        }
    }
    catch (System.Exception ex)
    {
        Logger.LogError($"Error leyendo configuración: {ex.Message}");
        
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
        Logger.LogError($"Error en fallback de recarga: {ex2.Message}");
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
                  return line.Substring("SongPath = ".Length);
            }
        }
    }
    catch (System.Exception ex)
    {
        Logger.LogError($"Error leyendo archivo de configuración: {ex.Message}");
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
    song.loop = true;
    song.volume = 1f;
    song.mute = false;
    
    StartCoroutine(SetAudioClip(songPath.Value));
    Logger.LogInfo("HeroController.instance listo y AudioSource creado");
    playerReady = true;
}

private IEnumerator ReloadSong()
{
    Logger.LogInfo("Recargando canción...");
    
    if (song != null)
    {
        bool wasPlaying = song.isPlaying;
        song.Stop();
        song.clip = null;
        
        yield return StartCoroutine(SetAudioClip(songPath.Value));
        
        if (wasPlaying && song != null && song.clip != null)
            song.Play();
    }
}

private bool HasMoved(Transform player)
{
    float distance = Math.Abs(lastPosition - player.position.x);
    bool hasMoved = distance > 0.01f;
    UpdatePosition();
    return hasMoved;
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

private IEnumerator SetAudioClip(string path)
{
    if (string.IsNullOrEmpty(path))
    {
        Logger.LogWarning("Path empty: song cant not be loaded");
        yield break;
    }

    if (!System.IO.File.Exists(path))
    {
        Logger.LogError("Archivo no encontrado: " + path);
        yield break;
    }

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

private void UpdatePosition()
{
    lastPosition = playerTransform.position.x;
}
}
