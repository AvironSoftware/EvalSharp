namespace EvalSharp.Scoring;


/// <summary>
/// Represents the score of a metric for a specific test case.
/// </summary>
public class MetricScore
{
    
    /// <summary>
    /// The test data.
    /// </summary>
    public EvaluatorTestData TestData { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricScore"/> class with the specified test data.
    /// </summary>
    /// <param name="testData">The data that was tested.</param>
    public MetricScore(EvaluatorTestData testData)
    {
        Check.NotNull(testData);
        TestData = testData;
    }
    
    /// <summary>
    /// The pass/fail result of the metric score evaluation.
    /// </summary>
    public required MetricScoreResult Result { get; init; }
    
    /// <summary>
    /// The score value of the metric, which is between 0 and 1.
    /// </summary>
    public required double Score { get; init; }
    
    /// <summary>
    /// The LLM's reasoning behind the score.
    /// </summary>
    public string? Reasoning { get; init; }
}