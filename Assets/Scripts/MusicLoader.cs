using UnityEngine;
using System.Collections.Generic;

public class MusicLoader : MonoBehaviour
{
    public AudioSource audioSource;
    public List<AudioClip> musicTracks;

    void Start()
    {
        int index = MusicData.SelectedMusicIndex;

        if (index < musicTracks.Count)
        {
            audioSource.clip = musicTracks[index];
            audioSource.Play();
        }
    }
}