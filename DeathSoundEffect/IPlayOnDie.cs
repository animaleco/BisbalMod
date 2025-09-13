using UnityEngine;
using System;

namespace MyMod;

public interface IPlayOnDie
{
    // Add a function to OnDie() event
    void SetOnDieFunction(Action function);
    // Add audio source, adjust volume... And play song
    void ConfigurePlayer();
    // Is player playing? GlobalEnums..
    bool IsPlaying();
    // Load the song from disk. Bepinex resources? How to add the file to the mod?
    AudioClip LoadSong();
    // Play song
    void PlaySong();
}
