using UnityEngine;
using System;
using System.Collections;

namespace MyMod;

public interface IPlayOnDie
{
    // Add audio source, adjust volume... And play song
    void ConfigurePlayer();
    // Is player playing? GlobalEnums..
    bool IsPlaying();
    // Load the song from disk. Bepinex resources? How to add the file to the mod?
    IEnumerator LoadSong(string path, AudioSource a);
    // Play song
    void PlaySong();
}
