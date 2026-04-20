using UnityEngine;
using UnityEngine.SceneManagement;

public class VisualizerSelector : MonoBehaviour
{
    public void LoadScene(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }
    public void QuitGame()
    {
        Application.Quit();
    }
}