using UnityEngine;

/// <summary>
/// Opdeler frekvensbånd fra MusicAnalyzer i tre overordnede zoner: Bas, Mellemtone og Diskant.
/// Indeholder nu en debug-funktion til at visualisere de tre peaks direkte i scenen.
/// </summary>
public class AudioSplitter : MonoBehaviour
{
    [Header("Setup")]
    public MusicAnalyzer analyzer;
    public bool showDebug = true; // Debug toggle

    [Header("Område Fordeling (%)")]
    [Tooltip("Hvor stor en procentdel af båndene der skal være bas (0.0 - 1.0)")]
    [Range(0f, 1f)] public float bassThreshold = 0.15f;
    [Tooltip("Hvor stor en procentdel der skal være mellemtone (0.0 - 1.0)")]
    [Range(0f, 1f)] public float midThreshold = 0.45f;

    [Header("Output Værdier (Read Only)")]
    public float bassPeak;
    public float midPeak;
    public float treblePeak;

    void Update()
    {
        if (analyzer == null || analyzer.audioBandBuffer == null || analyzer.audioBandBuffer.Length == 0)
            return;

        int totalBands = analyzer.audioBandBuffer.Length;

        // Beregn indekser for opdeling baseret på numBands fra analyzer
        int bassEnd = Mathf.FloorToInt(totalBands * bassThreshold);
        int midEnd = Mathf.FloorToInt(totalBands * midThreshold);

        // Nulstil peaks for denne frame
        bassPeak = 0;
        midPeak = 0;
        treblePeak = 0;

        for (int i = 0; i < totalBands; i++)
        {
            float currentValue = analyzer.audioBandBuffer[i];

            if (i < bassEnd)
            {
                if (currentValue > bassPeak) bassPeak = currentValue;
            }
            else if (i < midEnd)
            {
                if (currentValue > midPeak) midPeak = currentValue;
            }
            else
            {
                if (currentValue > treblePeak) treblePeak = currentValue;
            }
        }
    }

    // Tegner debug-visualiseringen i Scene-viewet eller Game-viewet vha. Gizmos
    private void OnDrawGizmos()
    {
        if (!showDebug || !Application.isPlaying) return;

        // Positionering for debug-søjler
        Vector3 startPos = transform.position + Vector3.up * 2f;
        float spacing = 2f;
        float barWidth = 1f;
        float heightMult = 5f;

        // Tegn Bas Søjle (Rød)
        DrawDebugBar(startPos, "Bas", bassPeak, Color.red, barWidth, heightMult);

        // Tegn Mellemtone Søjle (Grøn)
        DrawDebugBar(startPos + Vector3.right * spacing, "Mid", midPeak, Color.green, barWidth, heightMult);

        // Tegn Diskant Søjle (Blå)
        DrawDebugBar(startPos + Vector3.right * (spacing * 2), "Treble", treblePeak, Color.blue, barWidth, heightMult);
    }

    private void DrawDebugBar(Vector3 pos, string label, float value, Color color, float width, float heightMult)
    {
        Gizmos.color = color;
        Vector3 size = new Vector3(width, value * heightMult, width);
        Gizmos.DrawCube(pos + new Vector3(0, size.y / 2, 0), size);

        // Tekst (Dette vises i Scene-viewet)
#if UNITY_EDITOR
        UnityEditor.Handles.Label(pos + Vector3.up * (value * heightMult + 0.5f), $"{label}: {value:F2}");
#endif
    }
}