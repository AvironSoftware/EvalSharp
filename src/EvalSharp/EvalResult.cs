namespace EvalSharp;

/// <summary>
/// Represents the result of evaluation tests.
/// </summary>
public class EvalResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EvalResult"/> class.
    /// </summary>
    public EvalResult()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvalResult"/> class with the specified test results.
    /// </summary>
    /// <param name="tests">A collection of metric result collections representing the test results.</param>
    public EvalResult(IEnumerable<MetricResultCollection> tests)
    {
        Tests = [.. tests];
    }

    /// <summary>
    /// Collection of tests that executed against the desired metrics.
    /// </summary>
    public List<MetricResultCollection> Tests { get; } = new();

    /// <summary>
    /// Gets the number of tests.
    /// </summary>
    public int NumberOfTestCases => Tests.Min(m => m.Results.Count);

    /// <summary>
    /// Gets the average score across all test results.
    /// </summary>
    public double AverageScore => Tests.Sum(m => m.Results.Count) == 0 ? 0 : TotalScore / Tests.Sum(m => m.Results.Count);

    /// <summary>
    /// Get total score across all test results.
    /// </summary>
    public double TotalScore => Tests.Sum(r => r.TotalScore);
}
