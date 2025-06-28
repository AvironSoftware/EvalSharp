using EvalSharp.Models.Enums;

namespace EvalSharp.Scoring;

/// <summary>
/// Configuration for the Tool Correctness Metric.
/// </summary>
public record ToolCorrectnessMetricConfiguration : MetricConfiguration
{
    /// <summary>
    /// A boolean that indicates if order should be considered apart of the metric score. Default is False.
    /// </summary>
    public bool ShouldExactMatch { get; set; } = false;

    /// <summary>
    /// A boolean that, when set to True, makes an extra LLM call to provide a reason for the metric score. Default is True.
    /// </summary>
    public bool ShouldConsiderOrdering { get; set; } = false;

    /// <summary>
    /// List of tool call parameters to include in scoring; defaults to <see cref="ToolCallParamsEnum.TOOL"/>.
    /// </summary>
    public List<ToolCallParamsEnum> EvaluationParams = [ToolCallParamsEnum.TOOL];
}