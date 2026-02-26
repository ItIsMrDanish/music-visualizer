using UnityEngine;

public class Instantiator : MonoBehaviour
{
    public GameObject cubePrefab;
    public MusicAnalyzer analyzer; // Træk jeres AudioManager herind
    public float maxScale = 20f;
    GameObject[] sampleCubes;

    void Start()
    {
        sampleCubes = new GameObject[analyzer.numBands];
        for (int i = 0; i < analyzer.numBands; i++)
        {
            GameObject instance = Instantiate(cubePrefab);
            instance.transform.position = this.transform.position + new Vector3(i * 1.2f, 0, 0);
            instance.transform.parent = this.transform;
            sampleCubes[i] = instance;
        }
    }

    void Update()
    {
        for (int i = 0; i < analyzer.numBands; i++)
        {
            if (sampleCubes != null)
            {
                // Vi bruger audioBandBuffer[i] for at få en glidende bevægelse
                float scaleY = (analyzer.audioBandBuffer[i] * maxScale) + 1;
                sampleCubes[i].transform.localScale = new Vector3(1, scaleY, 1);
            }
        }
    }
}