using System.Collections;
using System;
using UnityEngine;
using BepInEx;
using UnityEngine.Networking;
using BepInEx.Configuration;
using System.IO;
using GlobalEnums;
using static IPlayOnDie;

[BepInPlugin("com.animaleco.silksong.mymod", "MyMod", "1.0.0")]
public class MyMod : BaseUnityPlugin, IPlayOnDie
{
   string pathSound = @"C:\Program Files (x86)\Steam\steamapps\common\Hollow Knight Silksong\Sounds\Cry.ogg";
   private bool isPlayerConfigured = false;

   public void PrintDeath()
   {
      Logger.LogInfo("RIP");

   }

   public void Update()
   {
      // Check if is playing
      if (GameManager._instance.GameState == GlobalEnums.GameState.PLAYING)
      {
         if (!isPlayerConfigured)
            ConfigurePlayer();
      }
   }

   public void ConfigurePlayer()
   {
      Logger.LogInfo("Enter configured player");
      HeroController.instance.OnDeath += PrintDeath;
      isPlayerConfigured = true;
      Logger.LogInfo("Configured player");
   }

    public void SetOnDieFunction(Action function)
    {
        throw new NotImplementedException();
    }

    public bool IsPlaying()
    {
        throw new NotImplementedException();
    }

    public AudioClip LoadSong()
    {
        throw new NotImplementedException();
    }

    public void PlaySong()
    {
        throw new NotImplementedException();
    }
}
