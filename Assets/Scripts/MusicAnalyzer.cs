using UnityEngine;

/// <summary>
/// Analyzes incoming audio and splits it into frequency bands using GetSpectrumData.
/// Attach to a GameObject with an AudioSource (or leave AudioSource empty to use the AudioListener).
/// Exposes raw spectrum, aggregated frequency bands, smoothed buffers and normalized audio bands for visualization.
/// </summary>
public class MusicAnalyzer : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("If null, AudioListener.GetSpectrumData will be used.")]
    public AudioSource audioSource;

    [Header("Spectrum")]
    [Tooltip("Number of samples used for FFT. Should be a power of two (64..8192).")]
    [Range(64, 8192)]
    public int sampleSize = 512;
    public FFTWindow fftWindow = FFTWindow.Blackman;

    [Header("Bands")]
    [Tooltip("Number of frequency bands to produce. Commonly 8 or 64.")]
    [Range(1, 64)]
    public int numBands = 8;

    // raw spectrum samples from GetSpectrumData
    [HideInInspector]
    public float[] samples;

    // aggregated frequency bands (linear or logarithmic depending on implementation below)
    [HideInInspector]
    public float[] freqBands;

    // smoothing buffers for each band (useful for visuals)
    [HideInInspector]
    public float[] bandBuffers;

    // how quickly each buffer decreases
    private float[] bufferDecrease;

    // highest value seen per band for normalization
    [HideInInspector]
    public float[] freqBandHighest;

    // normalized bands 0..1 (using freqBandHighest)
    [HideInInspector]
    public float[] audioBand;
    [HideInInspector]
    public float[] audioBandBuffer;

    // overall amplitude and buffered amplitude (0..1)
    [HideInInspector]
    public float amplitude, amplitudeBuffer;
    private float amplitudeHighest;

    void Start()
    {
        if (audioSource == null)
        {
            // It's fine to use AudioListener for global audio if no specific AudioSource is provided.
            // Many visualizers prefer using an AudioSource attached to the same GameObject.
        }

        // enforce power-of-two sample sizes sensibly: clamp to available range (user-controlled via inspector)
        sampleSize = Mathf.Clamp(sampleSize, 64, 8192);

        // initialize arrays
        samples = new float[sampleSize];
        freqBands = new float[numBands];
        bandBuffers = new float[numBands];
        bufferDecrease = new float[numBands];
        freqBandHighest = new float[numBands];
        audioBand = new float[numBands];
        audioBandBuffer = new float[numBands];

        // initialize highest values to small epsilon to avoid division by zero
        for (int i = 0; i < numBands; i++)
        {
            freqBandHighest[i] = 0.0001f;
            bufferDecrease[i] = 0.005f;
        }

        amplitudeHighest = 0.0001f;
    }

    void Update()
    {
        GetSpectrum();
        CreateFrequencyBands();
        BandBuffer();
        CreateAudioBands();
        GetAmplitude();
    }

    /// <summary>
    /// Fill the samples[] using the assigned AudioSource or the AudioListener if none assigned.
    /// </summary>
    private void GetSpectrum()
    {
        if (audioSource != null)
        {
            audioSource.GetSpectrumData(samples, 0, fftWindow);
        }
        else
        {
            AudioListener.GetSpectrumData(samples, 0, fftWindow);
        }
    }

    /// <summary>
    /// Aggregate raw spectrum samples into freqBands[].
    /// This uses a logarithmic-like split: each band contains twice the number of samples as the previous band,
    /// until we run out of samples. This approximates how humans perceive frequency.
    /// For other band strategies, replace this logic.
    /// </summary>
    private void CreateFrequencyBands()
    {
        // Reset bands
        for (int i = 0; i < numBands; i++)
            freqBands[i] = 0f;

        int sampleIndex = 0;
        // Determine band sizes using powers of two weighting but ensure all samples are consumed.
        for (int i = 0; i < numBands; i++)
        {
            // Calculate how many samples go into this band
            int samplesInBand = (int)Mathf.Pow(2, i) * 2;
            // If using many bands and limited sampleSize, clamp to remaining samples
            if (sampleIndex + samplesInBand > sampleSize)
            {
                samplesInBand = sampleSize - sampleIndex;
            }

            // Guard for the unlikely case of zero
            if (samplesInBand <= 0)
                samplesInBand = 1;

            float average = 0f;
            // Weighted average to favor higher-frequency samples within the band
            for (int j = 0; j < samplesInBand && sampleIndex < sampleSize; j++)
            {
                average += samples[sampleIndex] * (sampleIndex + 1);
                sampleIndex++;
            }

            average /= sampleIndex > 0 ? sampleIndex : 1;
            freqBands[i] = average * 10f; // scale up for easier visualization
        }

        // If there are leftover samples (in case our band sizing didn't consume all), fold them into the last band
        while (sampleIndex < sampleSize)
        {
            freqBands[numBands - 1] += samples[sampleIndex] * 10f;
            sampleIndex++;
        }
    }

    /// <summary>
    /// Smooth freqBands into bandBuffers. The buffer rises instantly and falls slowly.
    /// </summary>
    private void BandBuffer()
    {
        for (int i = 0; i < numBands; i++)
        {
            if (freqBands[i] > bandBuffers[i])
            {
                bandBuffers[i] = freqBands[i];
                bufferDecrease[i] = 0.005f;
            }
            else if (freqBands[i] < bandBuffers[i])
            {
                bandBuffers[i] -= bufferDecrease[i];
                bufferDecrease[i] *= 1.2f; // accelerate decrease over time
                if (bandBuffers[i] < 0f)
                    bandBuffers[i] = 0f;
            }
        }
    }

    /// <summary>
    /// Normalize bands to 0..1 using the highest observed value per band for adaptive scaling.
    /// Also output buffered normalized values.
    /// </summary>
    private void CreateAudioBands()
    {
        for (int i = 0; i < numBands; i++)
        {
            // track highest value seen
            if (freqBands[i] > freqBandHighest[i])
            {
                freqBandHighest[i] = freqBands[i];
            }

            // normalize
            audioBand[i] = Mathf.Clamp01(freqBands[i] / freqBandHighest[i]);
            audioBandBuffer[i] = Mathf.Clamp01(bandBuffers[i] / freqBandHighest[i]);
        }
    }

    /// <summary>
    /// Compute overall amplitude from normalized bands and maintain a buffered amplitude.
    /// </summary>
    private void GetAmplitude()
    {
        float currentAmplitude = 0f;
        float currentAmplitudeBuffer = 0f;

        for (int i = 0; i < numBands; i++)
        {
            currentAmplitude += audioBand[i];
            currentAmplitudeBuffer += audioBandBuffer[i];
        }

        if (currentAmplitude > amplitudeHighest)
        {
            amplitudeHighest = currentAmplitude;
        }

        amplitude = amplitudeHighest > 0 ? currentAmplitude / amplitudeHighest : 0f;
        amplitudeBuffer = amplitudeHighest > 0 ? currentAmplitudeBuffer / amplitudeHighest : 0f;
    }

    // If inspector changes are made at edit time, keep arrays consistent.
    private void OnValidate()
    {
        sampleSize = Mathf.Clamp(sampleSize, 64, 8192);
        if (numBands < 1) numBands = 1;

        samples = new float[sampleSize];

        if (freqBands == null || freqBands.Length != numBands)
        {
            freqBands = new float[numBands];
            bandBuffers = new float[numBands];
            bufferDecrease = new float[numBands];
            freqBandHighest = new float[numBands];
            audioBand = new float[numBands];
            audioBandBuffer = new float[numBands];

            for (int i = 0; i < numBands; i++)
            {
                freqBandHighest[i] = 0.0001f;
                bufferDecrease[i] = 0.005f;
            }
        }
    }
}