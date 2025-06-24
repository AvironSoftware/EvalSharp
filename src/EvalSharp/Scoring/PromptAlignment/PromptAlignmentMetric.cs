using EvalSharp.Models;
using EvalSharp.Models.Enums;
using EvalSharp.Scoring.PromptAlignment;
using Microsoft.Extensions.AI;

namespace EvalSharp.Scoring;
/// <summary>
/// Represents a metric for evaluating prompt alignment in chat-based systems.
/// </summary>
public class PromptAlignmentMetric : Metric<PromptAlignmentMetricConfiguration>, IChatClientMetric
{
    /// <summary>
    /// Gets the chat client used for interacting with the language model.
    /// </summary>
    public IChatClient ChatClient { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptAlignmentMetric"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client used for language model interactions.</param>
    /// <param name="configuration">The configuration for the prompt alignment metric.</param>
    public PromptAlignmentMetric(IChatClient chatClient, PromptAlignmentMetricConfiguration configuration) : base(configuration)
    {
        ChatClient = chatClient;
    }

    /// <summary>
    /// Scores the provided test data based on prompt alignment.
    /// </summary>
    /// <param name="testData">The test data containing input and output for evaluation.</param>
    /// <returns>A <see cref="MetricScore"/> representing the evaluation result.</returns>
    /// <exception cref="ArgumentException">Thrown if InitialInput, ActualOutput are null or whitespace.</exception>
    /// <exception cref="ArgumentException">Thrown if PromptInstructions are null or empty.</exception>
    public override async Task<MetricScore> ScoreAsync(EvaluatorTestData testData)
    {
        if (string.IsNullOrWhiteSpace(testData.InitialInput))
        {
            throw new ArgumentException("String cannot be null or whitespace.", nameof(testData.InitialInput));
        }
        if (string.IsNullOrWhiteSpace(testData.ActualOutput))
        {
            throw new ArgumentException("String cannot be null or whitespace.", nameof(testData.ActualOutput));
        }
        if (Configuration.PromptInstructions == null || Configuration.PromptInstructions.Count == 0)
        {
            throw new ArgumentException("'PromptInstructions' must not be empty.", nameof(Configuration.PromptInstructions));
        }

        // Step 1: Generate Verdicts
        var verdicts = await GenerateVerdictsAsync(testData);

        // Step 2: Calculate Score
        double score = verdicts.ScoreYesIdk();

        // Step 3: Generate Reason (if enabled)
        string reason = Configuration.IncludeReason ? await GenerateReasonAsync(testData, verdicts, score) : "";

        // Step 4: Determine success
        bool success = score >= Configuration.Threshold;

        return new MetricScore(testData)
        {
            Score = score,
            Reasoning = reason,
            Result = success ? MetricScoreResult.Pass : MetricScoreResult.Fail
        };
    }

    /// <summary>
    /// Generates verdicts for the given test data using the language model.
    /// </summary>
    /// <param name="testData">The test data containing input and output for evaluation.</param>
    /// <returns>An array of <see cref="VerdictModel"/> representing the verdicts.</returns>
    private async Task<VerdictModel[]> GenerateVerdictsAsync(EvaluatorTestData testData)
    {
        string prompt = PromptAlignmentTemplate.GenerateVerdicts(
            Configuration.PromptInstructions,
            testData.InitialInput!,
            testData.ActualOutput!
        );

        var response = await ChatClient.GetStructuredResponseFromLLM<VerdictsModel>(prompt);
        return response.Verdicts ?? [];
    }

    /// <summary>
    /// Generates reasoning for the score based on unaligned verdicts.
    /// </summary>
    /// <param name="testData">The test data containing input and output for evaluation.</param>
    /// <param name="verdicts">The verdicts generated during evaluation.</param>
    /// <param name="score">The calculated score.</param>
    /// <returns>A string representing the reasoning for the score.</returns>
    private async Task<string> GenerateReasonAsync(EvaluatorTestData testData, VerdictModel[] verdicts, double score)
    {
        var unalignmentReasons = verdicts.GetReasons(VerdictEnum.No);

        string prompt = PromptAlignmentTemplate.GenerateReason(
            unalignmentReasons,
            testData.InitialInput!,
            testData.ActualOutput!,
            score
        );

        var response = await ChatClient.GetStructuredResponseFromLLM<ReasonResponse>(prompt);
        return response.Reason;
    }
}
