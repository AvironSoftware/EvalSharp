using System.Text.RegularExpressions;
using EvalSharp.Models.Enums;

namespace EvalSharp.Scoring;

/// <summary>
/// Represents a metric used to evaluate matches between expected and actual outputs.
/// Provides different modes of matching such as exact match, regex match, and match after a specific string occurrence.
/// </summary>
    public class MatchMetric : Metric
{
    private readonly StringComparison? _stringComparison;

    /// <summary>
    /// Creates a MatchMetric for exact matching.
    /// </summary>
    /// <param name="stringComparison">Optional string comparison mode to use for matching.</param>
    /// <returns>A MatchMetric configured for exact matching.</returns>
    public static MatchMetric Exact(StringComparison? stringComparison = null)
    {
        return new MatchMetric(MatchMetricMode.Exact, stringComparison, null, null);
    }

    /// <summary>
    /// Creates a MatchMetric for regex-based matching.
    /// </summary>
    /// <param name="matchRegexString">The regex pattern to match against.</param>
    /// <param name="stringComparison">Optional string comparison mode to use for matching.</param>
    /// <returns>A MatchMetric configured for regex-based matching.</returns>
    public static MatchMetric Regex(string matchRegexString, StringComparison? stringComparison = null)
    {
        return new MatchMetric(MatchMetricMode.Regex, stringComparison, matchRegexString, null);
    }

    /// <summary>
    /// Creates a MatchMetric for matching text after the occurrence of a specific string.
    /// </summary>
    /// <param name="searchString">The string to search for in the actual output.</param>
    /// <param name="stringComparisonForAnswer">Optional string comparison mode to use for matching the answer.</param>
    /// <returns>A MatchMetric configured for matching text after a specific string occurrence.</returns>
    public static MatchMetric AfterString(string searchString, StringComparison? stringComparisonForAnswer = null)
    {
        return new MatchMetric(MatchMetricMode.AfterOccurrenceOfString, stringComparisonForAnswer, null, searchString);
    }

    /// <summary>
    /// Initializes a new instance of the MatchMetric class.
    /// </summary>
    /// <param name="mode">The mode of matching to use.</param>
    /// <param name="stringComparison">Optional string comparison mode to use for matching.</param>
    /// <param name="matchRegexString">The regex pattern to match against, if applicable.</param>
    /// <param name="afterOccurrenceOfString">The string to search for in the actual output, if applicable.</param>
    private MatchMetric(MatchMetricMode mode, StringComparison? stringComparison, string? matchRegexString, string? afterOccurrenceOfString)
    {
        _stringComparison = stringComparison;
        AfterOccurrenceOfString = afterOccurrenceOfString;
        Mode = mode;

        if (matchRegexString != null)
        {
            MatchRegex = new Regex(matchRegexString);
        }
    }

    /// <summary>
    /// Gets the mode of matching used by this metric.
    /// </summary>
    private MatchMetricMode Mode { get; }

    /// <summary>
    /// Gets a value indicating whether case should be ignored during matching.
    /// </summary>
    private bool IgnoreCase { get; }

    /// <summary>
    /// Gets the regex pattern used for matching, if applicable.
    /// </summary>
    private Regex? MatchRegex { get; }

    /// <summary>
    /// Gets the string to search for in the actual output, if applicable.
    /// </summary>
    private string? AfterOccurrenceOfString { get; }

    /// <summary>
    /// Scores the test data based on the configured matching mode.
    /// </summary>
    /// <param name="testData">The test data containing expected and actual outputs.</param>
    /// <returns>A task representing the asynchronous scoring operation.</returns>
    public override Task<MetricScore> ScoreAsync(EvaluatorTestData testData)
    {
        return Mode switch
        {
            MatchMetricMode.Exact => ScoreExact(testData),
            MatchMetricMode.Regex => ScoreRegex(testData),
            MatchMetricMode.AfterOccurrenceOfString => ScoreAfterOccurrenceOfString(testData),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    /// <summary>
    /// Scores the test data based on matching text after a specific string occurrence.
    /// </summary>
    /// <param name="testData">The test data containing expected and actual outputs.</param>
    /// <returns>A task representing the asynchronous scoring operation.</returns>
    private Task<MetricScore> ScoreAfterOccurrenceOfString(EvaluatorTestData testData)
    {
        var afterOccurrenceOfString = AfterOccurrenceOfString!;
        var indexOfOccurrence = testData.ActualOutput!.IndexOf(afterOccurrenceOfString, StringComparison.Ordinal);
        if (indexOfOccurrence == -1)
        {
            return Task.FromResult(new MetricScore(testData)
            {
                Score = 0,
                Reasoning = $"The string '{AfterOccurrenceOfString}' was not found in the actual output.",
                Result = MetricScoreResult.Fail
            });
        }

        // Check for multiple occurrences
        if (testData.ActualOutput.IndexOf(afterOccurrenceOfString, indexOfOccurrence + afterOccurrenceOfString.Length, StringComparison.Ordinal) != -1)
        {
            return Task.FromResult(new MetricScore(testData)
            {
                Score = 0,
                Reasoning = $"The string '{afterOccurrenceOfString}' was found multiple times in the actual output.",
                Result = MetricScoreResult.Fail
            });
        }

        var afterOccurrence = testData.ActualOutput.Substring(indexOfOccurrence + afterOccurrenceOfString.Length);
        afterOccurrence = afterOccurrence.Trim();

        var equal = IgnoreCase
            ? afterOccurrence.Equals(testData.ExpectedOutput, StringComparison.OrdinalIgnoreCase)
            : afterOccurrence.Equals(testData.ExpectedOutput);

        return Task.FromResult(new MetricScore(testData)
        {
            Score = equal ? 1 : 0,
            Reasoning = equal
                ? "The expected output matches the actual output."
                : "The expected output does not match the actual output.",
            Result = equal ? MetricScoreResult.Pass : MetricScoreResult.Fail
        });
    }

    /// <summary>
    /// Scores the test data based on regex matching.
    /// </summary>
    /// <param name="testData">The test data containing expected and actual outputs.</param>
    /// <returns>A task representing the asynchronous scoring operation.</returns>
    /// <exception cref="ArgumentException">Thrown when the ActualOutput or ExpectedOutput is null or whitespace.</exception>
    private Task<MetricScore> ScoreRegex(EvaluatorTestData testData)
    {
        if (string.IsNullOrWhiteSpace(testData.ActualOutput))
        {
            throw new ArgumentException("String cannot be null or whitespace.", nameof(testData.ActualOutput));
        }
        if (string.IsNullOrWhiteSpace(testData.ExpectedOutput))
        {
            throw new ArgumentException("String cannot be null or whitespace.", nameof(testData.ExpectedOutput));
        }
        // Find the first match.
        var match = MatchRegex!.Match(testData.ActualOutput);

        // If a match was found, check whether there's a second match.
        if (match.Success && match.NextMatch().Success)
        {
            return Task.FromResult(new MetricScore(testData)
            {
                Score = 0,
                Reasoning = "Multiple matches were found in the actual output.",
                Result = MetricScoreResult.Fail
            });
        }

        // The evaluation is successful only if a match is found
        // and its value equals the expected output (ignoring case).
        var success = match.Success &&
                      (_stringComparison == null
                          ? match.Value.Equals(testData.ExpectedOutput)
                          : match.Value.Equals(testData.ExpectedOutput, _stringComparison.Value));


        return Task.FromResult(new MetricScore(testData)
        {
            Score = success ? 1 : 0,
            Reasoning = success
                ? "The expected output matches the actual output."
                : "The expected output does not match the actual output.",
            Result = success ? MetricScoreResult.Pass : MetricScoreResult.Fail
        });
    }

    /// <summary>
    /// Scores the test data based on exact matching.
    /// </summary>
    /// <param name="context">The test data containing expected and actual outputs.</param>
    /// <returns>A task representing the asynchronous scoring operation.</returns>
    private Task<MetricScore> ScoreExact(EvaluatorTestData context)
    {
        var equal = IgnoreCase
            ? context.ExpectedOutput!.Equals(context.ActualOutput, StringComparison.OrdinalIgnoreCase)
            : context.ExpectedOutput!.Equals(context.ActualOutput);

        return Task.FromResult(new MetricScore(context)
        {
            Score = equal ? 1 : 0,
            Reasoning = equal
                ? "The expected output matches the actual output."
                : "The expected output does not match the actual output.",
            Result = equal ? MetricScoreResult.Pass : MetricScoreResult.Fail
        });
    }
}