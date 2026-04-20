using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MusicSelector : MonoBehaviour
{
    public TMPro.TMP_Dropdown dropdown;

    public void OnMusicChanged()
    {
        MusicData.SelectedMusicIndex = dropdown.value;
        Debug.Log("Current Index: " + MusicData.SelectedMusicIndex);
    }
}