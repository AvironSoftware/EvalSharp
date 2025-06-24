using EvalSharp.Scoring;

/// <summary>
/// Configuration for the Faithfulness metric.
/// </summary>
public class FaithfulnessMetricConfiguration : MetricConfiguration
{
    /// <summary>
    /// An integer specifying the number of truths to extract from the actual output. Leaving this NULL will let the LLM decide.
    /// </summary>
    public int? TruthsExtractionLimit { get; set; }

    /// <summary>
    /// A boolean that, when set to True, makes an extra LLM call to provide a reason for the metric score. Default is True.
    /// </summary>
    public bool IncludeReason { get; set; } = true;
}
