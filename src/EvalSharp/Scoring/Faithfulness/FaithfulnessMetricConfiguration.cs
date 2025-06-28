using EvalSharp.Scoring;

/// <summary>
/// Configuration for the Faithfulness metric.
/// </summary>
public record FaithfulnessMetricConfiguration : LLMAsAJudgeMetricConfiguration
{
    /// <summary>
    /// An integer specifying the number of truths to extract from the actual output. Leaving this NULL will let the LLM decide.
    /// </summary>
    public int? TruthsExtractionLimit { get; set; }
}
