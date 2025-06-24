using EvalSharp.Models;
using EvalSharp.Models.Enums;
using EvalSharp.Scoring.Hallucination;
using Microsoft.Extensions.AI;

namespace EvalSharp.Scoring;
/// <summary>
/// Represents a metric for evaluating hallucination in LLM outputs.
/// </summary>
public class HallucinationMetric : Metric<HallucinationMetricConfiguration>, IChatClientMetric
{
    /// <summary>
    /// Gets the chat client used for generating responses and structured data.
    /// </summary>
    public IChatClient ChatClient { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HallucinationMetric"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client used for generating responses.</param>
    /// <param name="configuration">The configuration for the hallucination metric.</param>
    public HallucinationMetric(IChatClient chatClient, HallucinationMetricConfiguration configuration) : base(configuration)
    {
        ChatClient = chatClient;
    }

    /// <summary>
    /// Scores the test data based on hallucination metrics.
    /// </summary>
    /// <param name="testData">The test data to evaluate.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the metric score.</returns>
    /// <exception cref="ArgumentException">Thrown when the ActualOutput is null or whitespace.</exception>
    /// <exception cref="ArgumentException">Thrown when the Context is null.</exception>
    public override async Task<MetricScore> ScoreAsync(EvaluatorTestData testData)
    {
        if (string.IsNullOrWhiteSpace(testData.ActualOutput))
        {
            throw new ArgumentException("String cannot be null or whitespace.", nameof(testData.ActualOutput));
        }
        if (testData.Context == null)
        {
            throw new ArgumentException("List cannot be null or whitespace.", nameof(testData.Context));
        }

        // Step 1: Generate Verdicts
        var verdicts = await GenerateVerdicts(testData);

        // Step 2: Compute Hallucination Score
        var score = verdicts.ScoreNo();

        // Step 3: Generate Explanation (if enabled)
        var reason = Configuration.IncludeReason ? await GenerateReason(testData, score, verdicts) : "";

        // Step 4: Determine success
        bool success = score <= Configuration.Threshold;

        return new MetricScore(testData)
        {
            Score = score,
            Reasoning = reason,
            Result = success ? MetricScoreResult.Pass : MetricScoreResult.Fail
        };
    }

    /// <summary>
    /// Generates verdicts for the given test data.
    /// </summary>
    /// <param name="context">The test data context.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the verdict models.</returns>
    private async Task<VerdictModel[]> GenerateVerdicts(EvaluatorTestData context)
    {
        string prompt = HallucinationTemplate.GenerateVerdicts(context.ActualOutput!, context.Context!);
        return (await ChatClient.GetStructuredResponseFromLLM<VerdictsModel>(prompt)).Verdicts;
    }

    /// <summary>
    /// Generates reasoning for the hallucination score.
    /// </summary>
    /// <param name="context">The test data context.</param>
    /// <param name="score">The computed hallucination score.</param>
    /// <param name="verdicts">The verdict models.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the reasoning string.</returns>
    private async Task<string> GenerateReason(EvaluatorTestData context, double score, VerdictModel[] verdicts)
    {
        var factualAlignments = verdicts.GetReasons(VerdictEnum.Yes);
        var contradictions = verdicts.GetReasons(VerdictEnum.No);
        string prompt = HallucinationTemplate.GenerateReason(factualAlignments, contradictions, score);
        var reasonResponse = await ChatClient.GetStructuredResponseFromLLM<ReasonResponse>(prompt);
        return reasonResponse.Reason;
    }
}
