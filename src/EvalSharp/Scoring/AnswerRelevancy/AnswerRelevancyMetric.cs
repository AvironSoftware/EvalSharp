using Microsoft.Extensions.AI;
using System.Text.Json;
using EvalSharp.Models;
using EvalSharp.Models.Enums;
using EvalSharp.Scoring.AnswerRelevancy;

namespace EvalSharp.Scoring;


/// <summary>
/// Represents a metric for evaluating the relevancy of an answer based on the provided test data.
/// </summary>
public class AnswerRelevancyMetric : Metric<AnswerRelevancyMetricConfiguration>, IChatClientMetric
{
    /// <summary>
    /// Gets the chat client used for interacting with the LLM.
    /// </summary>
    public IChatClient ChatClient { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AnswerRelevancyMetric"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client used for LLM interactions.</param>
    /// <param name="configuration">The configuration for the metric.</param>
    public AnswerRelevancyMetric(IChatClient chatClient, AnswerRelevancyMetricConfiguration configuration) : base(configuration)
    {
        ChatClient = chatClient;
    }

    /// <summary>
    /// Scores the relevancy of an answer based on the provided test data.
    /// </summary>
    /// <param name="testData">The test data containing the input and actual output.</param>
    /// <returns>A <see cref="MetricScore"/> representing the score and reasoning.</returns>
    /// <exception cref="ArgumentException">Thrown if the InitialInput or ActualOutput is null or whitespace.</exception>
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

        string input = testData.InitialInput;
        string actualOutput = testData.ActualOutput;

        // Step 1: Generate statements from the actual output
        string statementsPrompt = AnswerRelevancyTemplate.GenerateStatements(actualOutput);
        var statementsResponse = await ChatClient.GetStructuredResponseFromLLM<StatementsResponse>(statementsPrompt);
        List<string> statements = statementsResponse?.Statements ?? new List<string>();

        // Step 2: Generate verdicts using the input and the generated statements
        string statementsJson = JsonSerializer.Serialize(statements);
        string verdictsPrompt = AnswerRelevancyTemplate.GenerateVerdicts(input, statementsJson);
        var verdictsResponse = await ChatClient.GetStructuredResponseFromLLM<VerdictsModel>(verdictsPrompt);
        VerdictModel[] verdicts = verdictsResponse?.Verdicts ?? [];

        // Step 3: Calculate the relevancy score
        double score;
        if (verdicts.Length == 0)
        {
            score = 1;
        }
        else
        {
            score = verdicts.ScoreYesIdk();
            if (Configuration.StrictMode && score < Configuration.Threshold)
            {
                score = 0;
            }
        }

        // Step 4: Generate a reason for the score, if enabled
        string? reason = null;
        if (Configuration.IncludeReason)
        {
            var irrelevantReasons = verdicts.GetReasons(VerdictEnum.No);
            string reasonPrompt = AnswerRelevancyTemplate.GenerateReason(irrelevantReasons, input, score.ToString("F2"));
            var reasonResponse = await ChatClient.GetStructuredResponseFromLLM<ReasonResponse>(reasonPrompt);
            reason = reasonResponse.Reason;
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

}