using EvalSharp.Models;
using EvalSharp.Scoring.ContextualRecall;
using Microsoft.Extensions.AI;

namespace EvalSharp.Scoring;


/// <summary>
/// Represents a metric for evaluating contextual recall in a chat-based system.
/// </summary>
public class ContextualRecallMetric : LLMAsAJudgeMetric<ContextualRecallMetricConfiguration>, IChatClientMetric
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContextualRecallMetric"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client used for LLM interactions.</param>
    /// <param name="configuration">The configuration for the metric.</param>
    public ContextualRecallMetric(IChatClient chatClient, ContextualRecallMetricConfiguration configuration) : base(configuration, chatClient)
    {
    }

    /// <summary>
    /// Scores the test data based on contextual recall.
    /// </summary>
    /// <param name="testData">The test data to evaluate.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the metric score.</returns>
    /// <exception cref="ArgumentException">Thrown when ExpectedOutput is null or whitespace.</exception>
    /// <exception cref="ArgumentException">Thrown when RetrievalContext is null.</exception>
    public override async Task<MetricScore> ScoreAsync(EvaluatorTestData testData)
    {
        if (string.IsNullOrWhiteSpace(testData.ExpectedOutput))
        {
            throw new ArgumentException("String cannot be null or whitespace.", nameof(testData.ExpectedOutput));
        }
        if (testData.RetrievalContext == null)
        {
            throw new ArgumentException("List cannot be null or whitespace.", nameof(testData.RetrievalContext));
        }

        var verdictsPrompt = ContextualRecallTemplate.GenerateVerdicts(testData.ExpectedOutput, testData.RetrievalContext);

        // 2. Get verdicts from LLM
        var verdicts = await GetStructuredResponseFromLLM<VerdictsModel>(verdictsPrompt);

        // 3. Calculate score based on verdicts
        double score = verdicts.Verdicts.ScoreYes();

        // 4. Generate reason if needed
        string reason = await GenerateReason(testData.ExpectedOutput!, verdicts.Verdicts, score);

        // 5. Apply strict mode if enabled
        if (Configuration.StrictMode == true && score < 1.0)
        {
            score = 0.0;
        }

        // 6. Determine if successful based on threshold
        bool success = score >= Configuration.Threshold;

        // 7. Return the computed metric score
        return new MetricScore(testData)
        {
            Score = score,
            Reasoning = reason,
            Result = success ? MetricScoreResult.Pass : MetricScoreResult.Fail
        };
    }

    /// <summary>
    /// Generates reasoning for the score based on unaligned verdicts.
    /// </summary>
    /// <param name="expectedOutput">The expected output of the LLM</param>
    /// <param name="verdicts">The verdicts generated during evaluation.</param>
    /// <param name="score">The calculated score.</param>
    /// <returns>A string representing the reasoning for the score.</returns>
    private async Task<string> GenerateReason(string expectedOutput, VerdictModel[] verdicts, double score)
    {
        var reasonPrompt = ContextualRecallTemplate.GenerateReason(expectedOutput, verdicts, score);

        var reasonResponse = await GetStructuredResponseFromLLM<ReasonResponse>(reasonPrompt);
        return reasonResponse.Reason;
    }
} 