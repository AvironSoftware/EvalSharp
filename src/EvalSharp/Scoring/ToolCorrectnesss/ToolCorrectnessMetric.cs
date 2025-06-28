using EvalSharp.Models;
using EvalSharp.Models.Enums;

namespace EvalSharp.Scoring;

/// <summary>
/// Represents a metric for evaluating the correctness of tool calls in test data.
/// </summary>
/// <remarks>
/// This metric supports exact matching, ordering consideration, and parameter evaluation
/// based on the configuration provided.
/// </remarks>
public class ToolCorrectnessMetric : Metric<ToolCorrectnessMetricConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolCorrectnessMetric"/> class with the specified configuration.
    /// </summary>
    /// <param name="configuration">The configuration for the metric.</param>
    public ToolCorrectnessMetric(ToolCorrectnessMetricConfiguration configuration) : base(configuration)
    {
    }

    /// <summary>
    /// Scores the correctness of tool calls in the provided test data.
    /// </summary>
    /// <param name="testData">The test data containing the tools called and expected tools.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the metric score.</returns>
    /// <exception cref="ArgumentException">Thrown if ToolsCalled or ExpectedTools is null</exception>
    public override Task<MetricScore> ScoreAsync(EvaluatorTestData testData)
    {
        // Extract called and expected tools
        if (testData.ToolsCalled == null)
        {
            throw new ArgumentException($"Missing required {nameof(testData.ToolsCalled)} parameter.");
        }
        if (testData.ExpectedTools == null)
        {
            throw new ArgumentException($"Missing required {nameof(testData.ExpectedTools)} parameter.");
        }
        var toolsCalled = testData.ToolsCalled;
        var expectedTools = testData.ExpectedTools;
        var lcs = new List<ToolCall>();

        double score;
        if (Configuration.ShouldExactMatch)
        {
            score = CalculateExactMatchScore(toolsCalled, expectedTools);
        }
        else if (Configuration.ShouldConsiderOrdering)
        {
            var weightedLcs = ComputeWeightedLCS(toolsCalled, expectedTools);
            score = weightedLcs.Score / expectedTools.Count;
            lcs = weightedLcs.Lcs;
        }
        else
        {
            score = CalculateNonExactMatchScore(toolsCalled, expectedTools);
        }

        bool success = score >= (Configuration.StrictMode == true ? 1 : Configuration.Threshold);

        return Task.FromResult(new MetricScore(testData)
        {
            Score = score,
            Reasoning = GenerateReason(toolsCalled, expectedTools, lcs, score),
            Result = success ? MetricScoreResult.Pass : MetricScoreResult.Fail
        });
    }


    private double CalculateExactMatchScore(List<ToolCall> toolsCalled, List<ToolCall> expectedTools)
    {
        if (toolsCalled.Count != expectedTools.Count)
            return 0.0;

        for (int i = 0; i < toolsCalled.Count; i++)
        {
            if (toolsCalled[i].Name != expectedTools[i].Name)
            {
                return 0.0;
            }

            if (Configuration.EvaluationParams.Contains(ToolCallParamsEnum.INPUT_PARAMETERS) && !toolsCalled[i].InputParameters.SequenceEqual(expectedTools[i].InputParameters))
            {
                return 0.0;
            }

            if (Configuration.EvaluationParams.Contains(ToolCallParamsEnum.OUTPUT) && !toolsCalled[i].Output.Equals(expectedTools[i].Output))
            {
               return 0.0;
            }
        }
        return 1.0;
    }

    private double CalculateNonExactMatchScore(List<ToolCall> toolsCalled, List<ToolCall> expectedTools)
    {
        double totalScore = 0.0;
        var matchedCalledTools = new HashSet<ToolCall>();

        foreach (var expectedTool in expectedTools)
        {
            double bestScore = 0.0;
            ToolCall? bestMatch = null;

            foreach (var calledTool in toolsCalled)
            {
                if (matchedCalledTools.Contains(calledTool))
                    continue;

                double matchScore = expectedTool.Name == calledTool.Name ? 1.0 : 0.0;
                if (Configuration.EvaluationParams.Contains(ToolCallParamsEnum.INPUT_PARAMETERS))
                {
                    matchScore *= CompareDictionaries(expectedTool.InputParameters, calledTool.InputParameters);
                }

                if (Configuration.EvaluationParams.Contains(ToolCallParamsEnum.OUTPUT) && !expectedTool.Output.Equals(calledTool.Output))
                {
                    matchScore = 0.0;
                }

                if (matchScore > bestScore)
                {
                    bestScore = matchScore;
                    bestMatch = calledTool;
                }
            }

            if (bestScore > 0 && bestMatch != null)
            {
                totalScore += bestScore;
                matchedCalledTools.Add(bestMatch);
            }
        }

        return expectedTools.Count > 0 ? totalScore / expectedTools.Count : 0.0;
    }

    private (List<ToolCall> Lcs, double Score) ComputeWeightedLCS(List<ToolCall> expectedTools, List<ToolCall> toolsCalled)
    {
        int m = expectedTools.Count, n = toolsCalled.Count;
        double[,] dp = new double[m + 1, n + 1];
        List<ToolCall>[,] lcsTools = new List<ToolCall>[m + 1, n + 1];

        for (int i = 0; i <= m; i++)
        {
            for (int j = 0; j <= n; j++)
            {
                lcsTools[i, j] = [];
            }
        }

        for (int i = 1; i <= m; i++)
        {
            for (int j = 1; j <= n; j++)
            {
                if (expectedTools[i - 1].Name == toolsCalled[j - 1].Name)
                {
                    double score = 1.0;
                    if (Configuration.EvaluationParams.Contains(ToolCallParamsEnum.INPUT_PARAMETERS))
                    {
                        score *= CompareDictionaries(expectedTools[i - 1].InputParameters, toolsCalled[j - 1].InputParameters);
                    }

                    if (Configuration.EvaluationParams.Contains(ToolCallParamsEnum.OUTPUT) && !expectedTools[i - 1].Output.Equals(toolsCalled[j - 1].Output))
                    {
                        score = 0.0;
                    }

                    dp[i, j] = Math.Max(
                            dp[i - 1, j],
                            Math.Max(dp[i, j - 1], dp[i - 1, j - 1] + (score > 0 ? score : 0))
                        );
                    
                    if (expectedTools[i - 1].Name == toolsCalled[j - 1].Name && score > 0)
                    {
                        lcsTools[i, j] = new List<ToolCall>(lcsTools[i - 1, j - 1]) { expectedTools[i - 1] };
                    }
                    else
                    {
                        lcsTools[i, j] = dp[i - 1, j] > dp[i, j - 1] ? new List<ToolCall>(lcsTools[i - 1, j]) : new List<ToolCall>(lcsTools[i, j - 1]);
                    }
                }
                else
                {
                    if (dp[i - 1, j] > dp[i, j - 1])
                    {
                        dp[i, j] = dp[i - 1, j];
                        lcsTools[i, j] = new List<ToolCall>(lcsTools[i - 1, j]);
                    }
                    else
                    {
                        dp[i, j] = dp[i, j - 1];
                        lcsTools[i, j] = new List<ToolCall>(lcsTools[i, j - 1]);
                    }
                }
            }
        }
        var lcs = lcsTools[m, n];
        var totalScore = dp[m, n];

        return (lcs, totalScore);
    }

    private double CompareDictionaries(Dictionary<string, object?> dict1, Dictionary<string, object?> dict2)
    {
        if (Configuration.ShouldExactMatch)
            return dict1.SequenceEqual(dict2) ? 1.0 : 0.0;

        var allKeys = dict1.Keys.Union(dict2.Keys).ToList();

        int matched = allKeys.Count(key =>
                dict1.TryGetValue(key, out var v1) &&
                dict2.TryGetValue(key, out var v2) &&
                EqualityComparer<object?>.Default.Equals(v1, v2));

        return (double)matched / allKeys.Count;
    }


    private string GenerateReason(List<ToolCall> toolsCalled, List<ToolCall> expectedTools, List<ToolCall> lcs, double score)
    {
        var toolsCalledName = toolsCalled.Select(t => t.Name);
        var expectedToolsName = expectedTools.Select(t => t.Name);
        if (Configuration.ShouldExactMatch)
        {
            if (score == 1.0)
            {
                return "Exact match";
            }
            else
            {
                return $"Not an exact match: expected {toolsCalledName.ToFormattedList()}, called {expectedToolsName.ToFormattedList()}. See details above.";
            }
        }
        else if (Configuration.ShouldConsiderOrdering)
        {
            var lcsNames = lcs.Select(l => l.Name);
            var missing = expectedToolsName.Except(toolsCalledName).ToList();
            var outOfOrder = expectedToolsName.Except(lcsNames).ToList();

            if (score == 1.0)
            {
                return $"Correct ordering: all expected tools {expectedToolsName.ToFormattedList()} were called in the correct order.";
            }
            else
            {
                List<string> issues = [];
                if (missing.Count > 0)
                {
                    issues.Add($"missing tools {missing.ToFormattedList()}");
                }
                if (outOfOrder.Count > 0)
                {
                    issues.Add($"out-of-order tools {outOfOrder.ToFormattedList()}");
                }

                var issuesString = string.Join(" and ", issues);

                return $"Incorrect tool usage: {issuesString}; expected {expectedToolsName.ToFormattedList()}, called {toolsCalledName.ToFormattedList()}. See more details above.";
            }
        }
        else
        {
            if (score == 1.0)
            {
                return $"All expected tools {expectedToolsName.ToFormattedList()} were called (order not considered).";
            }
            else
            {
                var usedExpected = toolsCalledName.Intersect(expectedToolsName);
                var missing = expectedToolsName.Except(usedExpected).ToList();
                return $"Incomplete tool usage: missing tools {missing.ToFormattedList()}; expected {expectedToolsName.ToFormattedList()}, called {toolsCalledName.ToFormattedList()}. See more details above.";
            }
        }
    }
}
