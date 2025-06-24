namespace EvalSharp.Scoring;

/// <summary>
/// Represents the result of a metric score evaluation.
/// </summary>
public enum MetricScoreResult
{
    /// <summary>
    /// Indicates that the metric score passed the evaluation.
    /// </summary>
    Pass,
    /// <summary>
    /// Indicates that the metric score failed the evaluation.
    /// </summary>
    Fail
}