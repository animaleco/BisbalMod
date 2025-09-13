using System;
using System.IO;
using BepInEx.Configuration;
using BepInEx.Logging;

public class ConfigWatcher
{
  private FileSystemWatcher configWatcher;
  private DateTime lastConfigChangeTime = DateTime.MinValue;
  private ConfigFile config;
  private ManualLogSource logger;
  private string configFilePath;
  private string lastKnownSongPath = "";
  private const float CONFIG_CHECK_COOLDOWN = 1.0f;
  public event Action<string> OnConfigChanged;
  
  public ConfigWatcher(ConfigFile config, ManualLogSource logger)
    {
        this.config = config;
        this.logger = logger;
    }

  public void StartWatching()
  {
    try
    {
      configFilePath = config.ConfigFilePath;
      string configDirectory = Path.GetDirectoryName(configFilePath);
      string configFileName = Path.GetFileName(configFilePath);

      logger.LogInfo($"Monitoring config file: {configFilePath}");

      configWatcher = new FileSystemWatcher
      {
        Path = configDirectory,
        Filter = configFileName,
        NotifyFilter = NotifyFilters.LastWrite
      };

      configWatcher.Changed += OnConfigFileChanged;
      configWatcher.EnableRaisingEvents = true;

      logger.LogInfo("FileSystemWatcher configured successfully");
    }
    catch (Exception ex)
    {
      logger.LogError($"Error configuring FileSystemWatcher: {ex.Message}");
    }
  }

  private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
  {
    var now = DateTime.Now;
    if ((now - lastConfigChangeTime).TotalSeconds < CONFIG_CHECK_COOLDOWN)
      return;

    lastConfigChangeTime = now;
    logger.LogInfo("Config file modified externally");
    
    System.Threading.Tasks.Task.Delay(500).ContinueWith(_ => CheckConfigChange());
  }

  private void CheckConfigChange()
  {
    try
    {
      string newSongPath = ReadSongPathFromConfig();

      if (!string.IsNullOrEmpty(newSongPath) && newSongPath != lastKnownSongPath)
      {
        lastKnownSongPath = newSongPath;
        OnConfigChanged?.Invoke(newSongPath);
      }
    }
    catch (Exception ex)
    {
      logger.LogError($"Error reading config: {ex.Message}");
      try
      {
        config.Reload();
      }
      catch (Exception ex2)
      {
        logger.LogError($"Error in fallback reload: {ex2.Message}");
      }
    }
  }

  private string ReadSongPathFromConfig()
  {
    if (!File.Exists(configFilePath))
      return null;

    using var reader = new StreamReader(configFilePath);
    string line;
    bool inGeneralSection = false;

    while ((line = reader.ReadLine()) != null)
    {
      line = line.Trim();

      if (line.Equals("[General]", StringComparison.OrdinalIgnoreCase))
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

    return null;
  }
}