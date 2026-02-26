using UnityEngine;
public class MusicAnalyzer : MonoBehaviour
{
    public AudioSource audioSource;
    public SpectrumManager settings; // Reference til controlleren
    [HideInInspector] public FFTWindow fftWindow = FFTWindow.Blackman;
    [Range(2, 8192)] public int sampleSize = 4096;

    [HideInInspector] public float[] audioBandBuffer;
    private float[] samples;
    private float[] freqBands;
    private float[] bandBuffers;
    private float[] bufferDecrease;
    private float[] freqBandHighest;
    void Start()
    {
        InitializeArrays();
    }
    void InitializeArrays()
    {
        if (settings == null) return;

        samples = new float[sampleSize];
        freqBands = new float[settings.numBands];
        bandBuffers = new float[settings.numBands];
        bufferDecrease = new float[settings.numBands];
        freqBandHighest = new float[settings.numBands];
        audioBandBuffer = new float[settings.numBands];

        for (int i = 0; i < settings.numBands; i++)
        {
            freqBandHighest[i] = 0.0001f;
        }
    }
    void Update()
    {
        if (settings == null) return;

        // Hvis settings ændres i runtime, opdater arrays
        if (freqBands.Length != settings.numBands || samples.Length != sampleSize)
            InitializeArrays();

        GetSpectrum();
        CalculateBands();
        ApplyBuffer();
    }
    void GetSpectrum()
    {
        if (audioSource != null) audioSource.GetSpectrumData(samples, 0, fftWindow);
        else AudioListener.GetSpectrumData(samples, 0, fftWindow);
    }
    void CalculateBands()
    {
        float sampleRate = AudioSettings.outputSampleRate;
        float hzPerBin = (sampleRate / 2f) / sampleSize;

        int startBin = Mathf.FloorToInt(settings.startFrequency / hzPerBin);
        int endBin = Mathf.CeilToInt(settings.endFrequency / hzPerBin);

        startBin = Mathf.Clamp(startBin, 0, sampleSize - 1);
        int totalBinsInRange = Mathf.Clamp(endBin - startBin, 1, sampleSize);

        for (int i = 0; i < settings.numBands; i++)
        {
            float tStart = (float)i / settings.numBands;
            float tEnd = (float)(i + 1) / settings.numBands;

            int binStart = startBin + Mathf.FloorToInt(Mathf.Pow(tStart, 1.5f) * totalBinsInRange);
            int binEnd = startBin + Mathf.FloorToInt(Mathf.Pow(tEnd, 1.5f) * totalBinsInRange);
            if (binEnd <= binStart) binEnd = binStart + 1;

            float average = 0f;
            int count = 0;
            for (int j = binStart; j < binEnd && j < sampleSize; j++)
            {
                average += samples[j];
                count++;
            }
            if (count > 0) average /= count;
            freqBands[i] = average * 100f;
        }
    }
    void ApplyBuffer()
    {
        for (int i = 0; i < settings.numBands; i++)
        {
            if (freqBands[i] > bandBuffers[i])
            {
                bandBuffers[i] = freqBands[i];
                bufferDecrease[i] = 0.005f;
            }
            else
            {
                bandBuffers[i] -= bufferDecrease[i];
                bufferDecrease[i] *= 1.2f;
            }

            if (freqBands[i] > freqBandHighest[i]) freqBandHighest[i] = freqBands[i];
            audioBandBuffer[i] = Mathf.Clamp01(bandBuffers[i] / freqBandHighest[i]);
        }
    }
}