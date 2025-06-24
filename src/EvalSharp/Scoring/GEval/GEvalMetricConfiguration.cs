using EvalSharp.Scoring;

/// <summary>
/// Configuration for GEval metrics, which allows specifying criteria or evaluation steps for model output evaluation.
/// </summary>
public class GEvalMetricConfiguration : MetricConfiguration
{
    /// <summary>
    /// A string criteria that you specify will be given to an LLM and turned into a set of evaluation steps that the LLM will use to evaluate the model's output. If EvaluationSteps are provided, this property will be ignored.
    /// </summary>
    public string? Criteria { get; set; }

    /// <summary>
    /// List of strings that represent each step the LLM should use to evaluate.
    /// </summary>
    public List<string>? EvaluationSteps { get; set; }
}
