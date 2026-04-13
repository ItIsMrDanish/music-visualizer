using UnityEngine;

public class MusicResponse : MonoBehaviour
{
    [Header("Setup")]
    public AudioSplitter splitter;

    [Header("Frequency Peak Selection")]
    [Tooltip("Vælg hvilken del af musikken dette objekt skal reagere på")]
    public PeakSource selectedPeak = PeakSource.Bass;
    public enum PeakSource { Bass, Mid, Treble }

    [Header("Effect: Pulse (Scale)")]
    public bool usePulse;
    public Vector3 pulseIntensity = new Vector3(1, 1, 1);
    [Range(1f, 30f)] public float pulseSmooth = 10f;

    [Header("Effect: Light (Intensity)")]
    public bool useLight;
    public Light targetLight;
    public float lightMaxIntensity = 5f;
    [Range(1f, 30f)] public float lightSmooth = 10f;

    [Header("Effect: Camera (FOV)")]
    public bool useCameraZoom;
    public Camera targetCamera;
    public float zoomAmount = 10f;
    [Range(1f, 30f)] public float zoomSmooth = 10f;

    [Header("Effect: Color (Material)")]
    public bool useColorChange;
    public MeshRenderer targetRenderer;
    public Gradient colorGradient;
    [Range(0f, 5f)] public float colorMultiplier = 1f;

    // Interne variabler til smoothing
    private Vector3 startScale;
    private float startFOV;
    private float currentPeakValue;
    private float smoothedPeak;

    void Start()
    {
        startScale = transform.localScale;
        if (targetCamera != null) startFOV = targetCamera.fieldOfView;
    }

    void Update()
    {
        if (splitter == null) return;

        // 1. Hent den valgte peak-værdi
        switch (selectedPeak)
        {
            case PeakSource.Bass: currentPeakValue = splitter.bassPeak; break;
            case PeakSource.Mid: currentPeakValue = splitter.midPeak; break;
            case PeakSource.Treble: currentPeakValue = splitter.treblePeak; break;
        }

        // 2. Kør de valgte effekter
        if (usePulse) HandlePulse();
        if (useLight) HandleLight();
        if (useCameraZoom) HandleCamera();
        if (useColorChange) HandleColor();
    }

    void HandlePulse()
    {
        Vector3 targetScale = startScale + (pulseIntensity * currentPeakValue);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * pulseSmooth);
    }

    void HandleLight()
    {
        if (targetLight == null) return;
        float targetInt = currentPeakValue * lightMaxIntensity;
        targetLight.intensity = Mathf.Lerp(targetLight.intensity, targetInt, Time.deltaTime * lightSmooth);
    }

    void HandleCamera()
    {
        if (targetCamera == null) return;
        float targetFOV = startFOV - (currentPeakValue * zoomAmount);
        targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, targetFOV, Time.deltaTime * zoomSmooth);
    }

    void HandleColor()
    {
        if (targetRenderer == null) return;
        Color targetColor = colorGradient.Evaluate(currentPeakValue) * (currentPeakValue * colorMultiplier + 0.5f);
        targetRenderer.material.color = Color.Lerp(targetRenderer.material.color, targetColor, Time.deltaTime * 5f);
    }
}