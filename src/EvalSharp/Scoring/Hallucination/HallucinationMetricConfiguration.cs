namespace EvalSharp.Scoring;

/// <summary>
/// Configuration for the hallucination metric.
/// </summary>
public class HallucinationMetricConfiguration : MetricConfiguration
{
    /// <summary>
    /// A boolean that, when set to True, makes an extra LLM call to provide a reason for the metric score. Default is True.
    /// </summary>
    public bool IncludeReason { get; set; } = true;
}