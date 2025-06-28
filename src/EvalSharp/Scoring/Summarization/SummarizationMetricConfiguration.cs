namespace EvalSharp.Scoring;

/// <summary>
/// Configuration for summarization metrics.
/// </summary>
public record SummarizationMetricConfiguration : LLMAsAJudgeMetricConfiguration
{
    /// <summary>
    /// Integer representing the maximum number of factual claims to extract from the original text (input); null lets the LLM choose.
    /// </summary>
    public int? TruthsExtractionLimit { get; set; }

    /// <summary>
    /// A list of assessment questions to evaluate the summarization. Null indicates that questions will be generated automatically.
    /// </summary>
    public List<string>? AssessmentQuestions { get; set; }

    /// <summary>
    /// Integer representing the number of assessment questions to generate when none are provided. Defaults to 5.
    /// </summary>
    public int NumQuestions { get; set; } = 5;
}