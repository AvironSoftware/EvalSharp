using Spectre.Console;
using System.Text;
using EvalSharp.Scoring;

namespace EvalSharp;

/// <summary>
/// Responsible for printing evaluation results in various formats such as tabulated and detailed reports.
/// </summary>
public class EvalResultPrinter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EvalResultPrinter"/> class with a collection of metric results.
    /// </summary>
    /// <param name="results">The collection of metric results to be evaluated.</param>
    public EvalResultPrinter(IEnumerable<MetricResultCollection> results)
    {
        Result = new EvalResult(results);
        Console.OutputEncoding = Encoding.UTF8;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvalResultPrinter"/> class with a precomputed evaluation result.
    /// </summary>
    /// <param name="result">The precomputed evaluation result.</param>
    public EvalResultPrinter(EvalResult result)
    {
        Result = result;
        Console.OutputEncoding = Encoding.UTF8;
    }

    /// <summary>
    /// Gets or sets the evaluation result to be printed.
    /// </summary>
    public EvalResult Result { get; set; }

    /// <summary>
    /// Prints a detailed report of the evaluation results, including test case details and metrics.
    /// </summary>
    public void PrintReport()
    {
        var sb = new StringBuilder();

        foreach (var testCase in Result.Tests)
        {
            sb.AppendLine(PrintReportForTestCase(testCase!));

            var input = testCase.Test.InitialInput == null ? "None" : testCase.Test.InitialInput;
            var actualOutput = testCase.Test.ActualOutput == null ? "None" : testCase.Test.ActualOutput;
            var expectedOutput = testCase.Test.ExpectedOutput == null ? "None" : testCase.Test.ExpectedOutput;
            var context = testCase.Test.Context == null ? "None" : testCase.Test.Context.ToFormattedList();
            var retrievalContext = testCase.Test.RetrievalContext == null ? "None" : testCase.Test.RetrievalContext.ToFormattedList();

            sb.AppendLine("For test case:");
            sb.AppendLine();
            sb.AppendLine($"  - input: {input}");
            sb.AppendLine($"  - actual output: {actualOutput}");
            sb.AppendLine($"  - expected output: {expectedOutput}");
            sb.AppendLine($"  - context: {context}");
            sb.AppendLine($"  - retrieval context: {retrievalContext}");
        }
        var report = sb.ToString();
        report = report.Replace("{", "{{").Replace("}", "}}");

        PrintTabulated();
        PrintOverallTabulated();
        
        AnsiConsole.Write(report);
    }

    /// <summary>
    /// Prints a detailed report for a specific test case.
    /// </summary>
    /// <param name="testCase">The test case to be printed.</param>
    /// <returns>A string containing the detailed report for the test case.</returns>
    private static string PrintReportForTestCase(MetricResultCollection testCase)
    {
        var sb = new StringBuilder();

        sb.AppendLine(string.Join("", Enumerable.Repeat('=', 70)) + "\n");
        sb.AppendLine("Metrics Summary\n");
        foreach (var metricOutput in testCase.Results.Where(r => r.Score != null).OrderBy(m => m.Metric.Name))
        {
            var meta = metricOutput.Metric.Meta();
            var result = metricOutput.Score!;
            var emoji = result.Result == MetricScoreResult.Pass ? "✅" : "❌";

            sb.AppendLine($"  - {emoji} ({meta.Name}) (score: {result.Score}");
            if (meta.Threshold != null)
            {
                sb.Append($", threshold: {meta.Threshold}");
            }
            if (meta.StrictMode != null)
            {
                sb.Append($", strict: {meta.StrictMode}");
            }
            sb.Append($", reason: {metricOutput.Score?.Reasoning}");
            sb.Append(", error: None"); // maybe include error?
            sb.Append(')');

            sb.AppendLine();
        }
        return sb.ToString();
    }

    /// <summary>
    /// Prints a tabulated summary of test results.
    /// </summary>
    private void PrintTabulated()
    {
        var table = new Table
        {
            Title = new TableTitle("[italic]Test Results[/]"),
        };

        table.Centered();
        table.ShowRowSeparators();

        int count = 0;

        foreach (var row in Result.Tests)
        {
            //header
            if (count == 0)
            {
                table.AddColumn("Test case");
                foreach (var metric in row.Results.OrderBy(r => r.Metric.Name))
                {
                    table.AddColumn(metric.Metric.Name);
                }
                count++;
            }
            var cols = new List<string> { count.ToString() };

            foreach (var col in row.Results.OrderBy(r => r.Metric.Name))
            {
                cols.Add(col.Score.Result == MetricScoreResult.Pass ? "✅" : "❌");
            }

            table.AddRow(cols.ToArray());

            count++;
        }

        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Prints a tabulated summary of overall metric results.
    /// </summary>
    private void PrintOverallTabulated()
    {
        var table = new Table
        {
            Title = new TableTitle("[italic]Metric Results[/]"),
        };

        table.Centered();
        table.ShowRowSeparators();
        // Add 4 columns to the table
        table.AddColumn("Metric");
        table.AddColumn("Score");
        table.AddColumn("Status");
        table.AddColumn("Overall Success Rate");

        var grouped = Result.Tests.SelectMany(Results => Results.Results).GroupBy(x => x.Metric).OrderBy(m => m.Key.Name);

        foreach (var group in grouped)
        {
            var meta = group.Key.Meta();
            var totalScore = group.Sum(x => x.Score.Score);
            var testCount = group.Count();
            var score = (totalScore / testCount);

            var scoreText = $"{totalScore} (threshold={meta.Threshold}, strict: {meta.StrictMode}, evaluation model={meta.Model})";
            var pass = meta.StrictMode == true ? score == 1.0 : score >= meta.Threshold;
            var passText = pass ? $"[green]Pass[/]" : $"[red]Fail[/]";
            var overallSuccess = score * 100;

            table.AddRow(new Text(meta.Name), new Text(scoreText), new Markup(passText), new Text($"{overallSuccess}.0%"));

        }

        AnsiConsole.Write(table);
    }
}