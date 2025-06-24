using Microsoft.Extensions.AI;
using System.Text.Json;
using EvalSharp.Models;
using EvalSharp.Models.Enums;
using EvalSharp.Scoring.ContextualPrecision;

namespace EvalSharp.Scoring;

/// <summary>
/// Represents a metric for evaluating contextual precision in a retrieval-based system.
/// </summary>
/// <remarks>
/// This metric calculates the precision of retrieved items in the context of a given input and expected output.
/// It uses a chat client to interact with an AI model for generating verdicts and reasoning.
/// </remarks>
public class ContextualPrecisionMetric : Metric<ContextualPrecisionMetricConfiguration>, IChatClientMetric
{
    /// <summary>
    /// Gets the chat client used for interacting with the AI model.
    /// </summary>
    public IChatClient ChatClient { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextualPrecisionMetric"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client used for AI interactions.</param>
    /// <param name="configuration">The configuration for the metric.</param>
    public ContextualPrecisionMetric(IChatClient chatClient, ContextualPrecisionMetricConfiguration configuration)
        : base(configuration)
    {
        ChatClient = chatClient;
    }

    /// <summary>
    /// Scores the test data based on contextual precision.
    /// </summary>
    /// <param name="testData">The test data containing input, expected output, and retrieval context.</param>
    /// <returns>A <see cref="MetricScore"/> representing the score and reasoning.</returns>
    /// <exception cref="ArgumentException">Thrown if the RetrievalContext is null or empty.</exception>
    /// <exception cref="ArgumentException">Thrown if the ExpectedOutput or InitialInput is null or whitespace.</exception>
    public override async Task<MetricScore> ScoreAsync(EvaluatorTestData testData)
    {
        if (testData.RetrievalContext == null || testData.RetrievalContext.Count == 0)
        {
            throw new ArgumentException("List is required.", nameof(testData.RetrievalContext));
        }

        if (string.IsNullOrWhiteSpace(testData.ExpectedOutput))
        {
            throw new ArgumentException("String cannot be null or whitespace.", nameof(testData.ExpectedOutput));
        }
        if (string.IsNullOrWhiteSpace(testData.InitialInput))
        {
            throw new ArgumentException("String cannot be null or whitespace.", nameof(testData.InitialInput));
        }

        string input = testData.InitialInput;
        string expectedOutput = testData.ExpectedOutput!;
        List<string> retrievalContext = testData.RetrievalContext;
        
        // Step 1: Generate verdicts using the input, expected output, and retrieval context
        string verdictsPrompt = ContextualPrecisionTemplate.GenerateVerdicts(input, expectedOutput, retrievalContext);
        var verdictsResponse =
            await ChatClient.GetStructuredResponseFromLLM<VerdictsModel>(verdictsPrompt);
        VerdictModel[] verdicts = verdictsResponse?.Verdicts ?? [];
        // Step 2: Calculate the contextual precision score
        double score = CalculateScore(verdicts);
        if (Configuration.StrictMode && score < Configuration.Threshold)
        {
            score = 0;
        }

        // Step 3: Generate a reason for the score, if enabled
        string? reason = null;
        if (Configuration.IncludeReason)
        {
            // Serialize verdicts to JSON (for inclusion in the prompt)
            string verdictsJson = JsonSerializer.Serialize(verdicts);
            string reasonPrompt = ContextualPrecisionTemplate.GenerateReason(input, verdictsJson, score.ToString("F2"));
            var reasonResponse =
                await ChatClient.GetStructuredResponseFromLLM<ReasonResponse>(reasonPrompt);
            reason = reasonResponse?.Reason;
        }

        bool success = score >= Configuration.Threshold;
        MetricScoreResult result = success ? MetricScoreResult.Pass : MetricScoreResult.Fail;
        var metricScore = new MetricScore(testData)
        {
            Score = score,
            Reasoning = reason,
            Result = result
        };
        return metricScore;
    }

    private static double CalculateScore(VerdictModel[] verdicts)
    {
        int n = verdicts.Length;
        if (n == 0)
        {
            return 0;
        }

        // Convert verdicts to a binary list (1 for "yes", 0 otherwise)
        List<int> binary = new List<int>();
        foreach (var v in verdicts)
        {
            binary.Add(v.Verdict == VerdictEnum.Yes ? 1 : 0);
        }

        double sumWeighted = 0;
        int relevantCount = 0;
        for (int k = 0; k < binary.Count; k++)
        {
            if (binary[k] == 1)
            {
                relevantCount++;
                double precisionAtK = (double)relevantCount / (k + 1);
                sumWeighted += precisionAtK;
            }
        }

        if (relevantCount == 0)
        {
            return 0;
        }

        return sumWeighted / relevantCount;
    }
}