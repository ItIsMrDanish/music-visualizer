using UnityEngine;
public class SpectrumManager : MonoBehaviour
{
    [Header("Setup Settings")]
    public MusicAnalyzer analyzer;
    public GameObject cubePrefab;

    [Header("Audio Spectrum Settings")]
    public float startFrequency = 20f;
    public float endFrequency = 15000f;
    public int numBands = 64;
    public float maxHeight = 15f;
    public float totalWidth = 25f;
    [Range(0.1f, 1f)] public float barSpacing = 0.8f;
    [Tooltip("Lower = Slower. Higher = Faster")] [Range(1f, 100f)] public float smoothSpeed = 10f;
    public DisplaySide displaySide = DisplaySide.Both;
    public enum DisplaySide { Top, Bottom, Both }

    [Header("Color Settings")]
    public Gradient spectrumGradient; // Lav en flot overgang i Inspectoren
    [Range(0f, 5f)] public float colorMultiplier = 1.0f; // Gør farverne mere intense/lysende

    private GameObject[] sampleCubes;
    private float[] currentYScale;
    private MeshRenderer[] cubeRenderers; // Gem referencer for bedre performance

    void Start() { SpawnCubes(); }

    void SpawnCubes()
    {
        if (sampleCubes != null) foreach (var cube in sampleCubes) Destroy(cube);

        sampleCubes = new GameObject[numBands];
        currentYScale = new float[numBands];
        cubeRenderers = new MeshRenderer[numBands];

        float cubeWidth = (totalWidth / numBands);
        float startX = -(totalWidth / 2f) + (cubeWidth / 2f);

        for (int i = 0; i < numBands; i++)
        {
            GameObject instance = Instantiate(cubePrefab, this.transform);
            instance.transform.localPosition = new Vector3(startX + (i * cubeWidth), 0, 0);
            instance.transform.localScale = new Vector3(cubeWidth * barSpacing, 0.1f, 1);

            sampleCubes[i] = instance;
            cubeRenderers[i] = instance.GetComponent<MeshRenderer>();
        }
    }

    void Update()
    {
        if (sampleCubes.Length != numBands) SpawnCubes();

        for (int i = 0; i < numBands; i++)
        {
            if (analyzer.audioBandBuffer == null || i >= analyzer.audioBandBuffer.Length) continue;

            // 1. Beregn højde
            float targetY = (analyzer.audioBandBuffer[i] * maxHeight) + 0.1f;
            currentYScale[i] = Mathf.Lerp(currentYScale[i], targetY, Time.deltaTime * smoothSpeed);

            // 2. Opdater skala og position
            Vector3 newScale = sampleCubes[i].transform.localScale;
            Vector3 newPos = sampleCubes[i].transform.localPosition;

            if (displaySide == DisplaySide.Top)
            {
                newScale.y = currentYScale[i];
                newPos.y = currentYScale[i] / 2f;
            }
            else if (displaySide == DisplaySide.Bottom)
            {
                newScale.y = currentYScale[i];
                newPos.y = -currentYScale[i] / 2f;
            }
            else
            {
                newScale.y = currentYScale[i] * 2f;
                newPos.y = 0;
            }

            sampleCubes[i].transform.localScale = newScale;
            sampleCubes[i].transform.localPosition = newPos;

            // 3. Opdater farve baseret på gradient og lydstyrke
            if (cubeRenderers[i] != null)
            {
                // Find farven baseret på placering (0 = bas, 1 = diskant)
                float t = (float)i / numBands;
                Color baseColor = spectrumGradient.Evaluate(t);

                // Gør farven lysere/stærkere jo højere søjlen er
                float intensity = (analyzer.audioBandBuffer[i] * colorMultiplier) + 0.5f;
                cubeRenderers[i].material.color = baseColor * intensity;

                // Hvis du bruger en Shader med Emission, kan du også gøre dette:
                // cubeRenderers[i].material.SetColor("_EmissionColor", baseColor * intensity);
            }
        }
    }
}