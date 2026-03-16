using UnityEngine;

public class SpectrumManager : MonoBehaviour
{
    [Header("Setup Settings")]
    public MusicAnalyzer analyzer;
    public GameObject cubePrefab;

    [Header("Audio Spectrum Settings")]
    public float startFrequency = 30f;
    public float endFrequency = 15000f;
    public int numBands = 64;
    public float maxHeight = 15f;
    [Range(0.1f, 1f)] public float barThickness = 0.8f;
    [Tooltip("Lower = Slower. Higher = Faster")][Range(1f, 100f)] public float smoothSpeed = 10f;
    public DisplaySide displaySide = DisplaySide.Both;
    public enum DisplaySide { Top, Bottom, Both }

    [Header("Shape Settings")]
    public ShapeMode shape = ShapeMode.Line;
    public float shapeSize = 35f;
    public enum ShapeMode { Line, Circle, Square, Triangle }

    [Header("Color Settings")]
    public Gradient spectrumGradient;
    [Range(0f, 5f)] public float colorMultiplier = 1.0f;

    private GameObject[] sampleCubes;
    private float[] currentYScale;
    private MeshRenderer[] cubeRenderers;

    void Start() { SpawnCubes(); }

    void SpawnCubes()
    {
        if (sampleCubes != null) foreach (var cube in sampleCubes) Destroy(cube);

        sampleCubes = new GameObject[numBands];
        currentYScale = new float[numBands];
        cubeRenderers = new MeshRenderer[numBands];

        for (int i = 0; i < numBands; i++)
        {
            GameObject instance = Instantiate(cubePrefab, this.transform);

            // Beregn position og rotation baseret på valgt form
            SetObjectLayout(instance.transform, i);

            sampleCubes[i] = instance;
            cubeRenderers[i] = instance.GetComponent<MeshRenderer>();
        }
    }

    // Denne metode beregner hvor hver søjle skal stå og kigge hen
    void SetObjectLayout(Transform trans, int index)
    {
        float t = (float)index / numBands;
        float width = (shapeSize / numBands);

        switch (shape)
        {
            case ShapeMode.Line:
                float startX = -(shapeSize / 2f) + (width / 2f);
                trans.localPosition = new Vector3(startX + (index * width), 0, 0);
                trans.localRotation = Quaternion.identity;
                trans.localScale = new Vector3(width * barThickness, 0.1f, barThickness);
                break;

            case ShapeMode.Circle:
                float angle = t * Mathf.PI * 2f;
                float radius = shapeSize / 2f;
                trans.localPosition = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                trans.localRotation = Quaternion.LookRotation(trans.localPosition - transform.position);
                trans.localScale = new Vector3((radius * 2f * Mathf.PI / numBands) * barThickness, 0.1f, barThickness);
                break;

            case ShapeMode.Square:
                // Simpel kvadrat-omkreds logik
                float sideBands = numBands / 4f;
                float sideLength = shapeSize / 1.5f;
                Vector3 pos = Vector3.zero;
                if (index < sideBands) pos = new Vector3(index * (sideLength / sideBands), 0, 0);
                else if (index < sideBands * 2) pos = new Vector3(sideLength, 0, (index - sideBands) * (sideLength / sideBands));
                else if (index < sideBands * 3) pos = new Vector3(sideLength - (index - sideBands * 2) * (sideLength / sideBands), 0, sideLength);
                else pos = new Vector3(0, 0, sideLength - (index - sideBands * 3) * (sideLength / sideBands));

                trans.localPosition = pos - new Vector3(sideLength / 2f, 0, sideLength / 2f);
                trans.localRotation = Quaternion.identity;
                trans.localScale = new Vector3(barThickness, 0.1f, barThickness);
                break;

            case ShapeMode.Triangle:
                float triSide = numBands / 3f;
                float triLen = shapeSize / 1f;
                Vector3 triPos = Vector3.zero;
                if (index < triSide) triPos = Vector3.Lerp(new Vector3(0, 0, triLen), new Vector3(triLen / 2f, 0, 0), (index / triSide));
                else if (index < triSide * 2) triPos = Vector3.Lerp(new Vector3(triLen / 2f, 0, 0), new Vector3(-triLen / 2f, 0, 0), (index - triSide) / triSide);
                else triPos = Vector3.Lerp(new Vector3(-triLen / 2f, 0, 0), new Vector3(0, 0, triLen), (index - triSide * 2) / triSide);

                trans.localPosition = triPos;
                trans.localRotation = Quaternion.identity;
                trans.localScale = new Vector3(barThickness, 0.1f, barThickness);
                break;
        }
    }

    void Update()
    {
        if (sampleCubes.Length != numBands) SpawnCubes();

        for (int i = 0; i < numBands; i++)
        {
            if (analyzer.audioBandBuffer == null || i >= analyzer.audioBandBuffer.Length) continue;

            float targetY = (analyzer.audioBandBuffer[i] * maxHeight) + 0.1f;
            currentYScale[i] = Mathf.Lerp(currentYScale[i], targetY, Time.deltaTime * smoothSpeed);

            Vector3 newScale = sampleCubes[i].transform.localScale;
            Vector3 newPos = sampleCubes[i].transform.localPosition;

            // Juster Y-skala og position baseret på displaySide
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

            if (cubeRenderers[i] != null)
            {
                float t = (float)i / numBands;
                Color baseColor = spectrumGradient.Evaluate(t);
                float intensity = (analyzer.audioBandBuffer[i] * colorMultiplier) + 0.75f;
                cubeRenderers[i].material.color = baseColor * intensity;
            }
        }
    }

    // Hjælpefunktion til at opdatere layout hvis man ændrer form i Inspectoren mens spillet kører
    private void OnValidate() { if (Application.isPlaying && sampleCubes != null) SpawnCubes(); }
}