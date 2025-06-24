using EvalSharp.Scoring;

namespace EvalSharp
{
    /// <summary>
    /// Represents a collection of metric results for a specific test case.
    /// </summary>
    public class MetricResultCollection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetricResultCollection"/> class with the specified test data and results.
        /// </summary>
        /// <param name="test">The test data used to evaluate metrics. Cannot be null.</param>
        /// <param name="results">A list of ValueTuple&lt;Metric, MetricScore&gt;</param>
        public MetricResultCollection(EvaluatorTestData test, List<(Metric Metric, MetricScore Score)> results)
        {
            Check.NotNull(test);
            Test = test;
            Results = results;
        }

        /// <summary>
        /// Test data associated with the metric results.
        /// </summary>
        public EvaluatorTestData Test { get; set; }

        /// <summary>
        /// The collection of metrics and their associated scores.
        /// </summary>
        public List<(Metric Metric, MetricScore Score)> Results { get; init; } = [];

        /// <summary>
        /// Gets the average score of all metrics for this test.
        /// </summary>
        public double AverageScore => Results.Count == 0 ? 0 : Results.Average(s => s.Score.Score);

        /// <summary>
        /// Gets the total score of all metrics for this test.
        /// </summary>
        public double TotalScore => Results.Sum(r => r.Score.Score);

        /// <summary>
        /// Gets the total number of metrics that passed.
        /// </summary>
        public int TotalPassed => Results.Count(s => s.Score.Result == MetricScoreResult.Pass);

        /// <summary>
        /// Gets the total number of metrics that failed.
        /// </summary>
        public int TotalFailed => Results.Count(s => s.Score.Result == MetricScoreResult.Fail);

    }
}
