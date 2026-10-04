using UnityEngine;

public class AgentEvaluator : MonoBehaviour
{
    private int totalEpisodes = 0;
    private int successfulEpisodes = 0;
    private int maxEvalEpisodes = 100;

    public void RecordEpisode(bool success)
    {
        totalEpisodes++;
        if (success) successfulEpisodes++;

        float successRate = ((float)successfulEpisodes / totalEpisodes) * 100f;
        Debug.Log($"[EVAL] Episode: {totalEpisodes}/{maxEvalEpisodes} | Success Rate: {successRate:F1}%");

        if (totalEpisodes >= maxEvalEpisodes)
        {
            Debug.Log($"--- FINAL EVALUATION RESULT ---");
            Debug.Log($"Total Tested: {totalEpisodes}");
            Debug.Log($"Success Rate: {successRate:F2}%");
            Time.timeScale = 0f; // Pause editor when done
        }
    }
}