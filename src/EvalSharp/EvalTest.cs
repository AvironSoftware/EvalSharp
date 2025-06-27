using EvalSharp.Exceptions;
using EvalSharp.Helpers;
using EvalSharp.Scoring;
using System.Text;

namespace EvalSharp
{
    /// <summary>
    /// Provides methods for evaluating metrics against test data and asserting results.
    /// </summary>
    public static class EvalTest
    {
        /// <summary>
        /// Runs one against the context and asserts that all meet the minimum threshold.
        /// Fails the test if the metric score is below the threshold.
        /// </summary>
        /// <param name="metric">A metric to evaluate.</param>
        /// <param name="testData">The evaluation test data</param>
        /// <param name="sink">An optional action to handle evaluation messages.</param>
        public static async Task AssertAsync(
            EvaluatorTestData testData,
            Metric metric,
            Action<string>? sink = null)
        {
            await AssertAsync(testData, [metric], sink);
        }
        /// <summary>
        /// Runs one or more metrics against the context and asserts that all meet the minimum threshold.
        /// Fails the test if any metric score is below the threshold.
        /// </summary>
        /// <param name="metrics">A list of metrics to evaluate.</param>
        /// <param name="testData">The evaluation test data</param>
        /// <param name="sink">An optional action to handle evaluation messages.</param>
        public static async Task AssertAsync(
            EvaluatorTestData testData,
            List<Metric> metrics,
            Action<string>? sink = null)
        {
            if (metrics == null || metrics.Count == 0)
                throw new ArgumentException("At least one metric must be provided.", nameof(metrics));

            ArgumentNullException.ThrowIfNull(testData);

            var tests = metrics.Select(async metric =>
            {
                var score = await metric.ScoreAsync(testData);
                var meta = metric.Meta();
                return (meta, score);
            });

            var allMetrics = await Task.WhenAll(tests);
            var failedMetrics = allMetrics.Where(m => m.score.Result == MetricScoreResult.Fail);

            if (sink != null)
            {
                Console.OutputEncoding = Encoding.UTF8;
                var headers = new[] { "Test case", "Metric", "Score", "Status", "Overall Success Rate" };

                var totalScore = allMetrics.Sum(a => a.score.Score) / allMetrics.Length * 100;
                var rows = allMetrics.Select((data, index) => new string[]
                {
                    (index == 0) ? "1" : "",
                    data.meta.Name,
                    $"{data.score.Score} (threshold={data.meta.Threshold}, evaluation model={data.meta.Model}, reason={data.score.Reasoning})",
                    data.score.Result == MetricScoreResult.Pass ? "Pass" : "Fail",
                    (index == 0) ? $"{totalScore:F2}%" : ""
                }).ToList();

                var consolePrint = BuildTable(headers, [.. rows]);
                sink(consolePrint);
            }

            if (failedMetrics.Any())
            {
                var message = string.Join(',', failedMetrics.Select(m => $"{m.meta.Name} (score: {m.score.Score:F2}, threshold: {m.meta.Threshold}, strict: {m.meta.StrictMode})"));
                throw new EvalFailException($"Metrics: {message} failed.");
            }

        }

        private static string BuildTable(string[] headers, List<string[]> rows)
        {
            var table = new TableBuilder();
            foreach (var header in headers)
            {
                table.AddColumn(header);
            }

            foreach (var row in rows)
            {
                table.AddRow(row);
            }

            string tableString = table.Build();
            return tableString;
        }
    }
}
