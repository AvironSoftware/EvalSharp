using EvalSharp.Models;
using EvalSharp.Models.Enums;
using EvalSharp.Scoring.Bias;
using Microsoft.Extensions.AI;

namespace EvalSharp.Scoring;

/// <summary>
/// Represents a metric for evaluating bias in the output of an evaluator test.
/// </summary>
public class BiasMetric : LLMAsAJudgeMetric<BiasMetricConfiguration>, IChatClientMetric
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BiasMetric"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client used for language model interactions.</param>
    /// <param name="configuration">The configuration for the bias metric.</param>
    public BiasMetric(IChatClient chatClient, BiasMetricConfiguration configuration) : base(configuration, chatClient)
    {
    }

    /// <summary>
    /// Scores the evaluator test data based on detected bias in the actual output.
    /// </summary>
    /// <param name="testData">The evaluator test data containing the actual output to be scored.</param>
    /// <returns>A task that represents the asynchronous scoring operation. The task result contains the metric score.</returns>
    /// <exception cref="ArgumentException">Thrown when the ActualOutput is null or whitespace.</exception>
    public override async Task<MetricScore> ScoreAsync(EvaluatorTestData testData)
    {
        if (string.IsNullOrWhiteSpace(testData.ActualOutput))
        {
            throw new ArgumentException("String cannot be null or whitespace.", nameof(testData.ActualOutput));
        }
        // Step 1: Extract opinions from the actual output
        string opinionsPrompt = BiasTemplate.GenerateOpinions(testData.ActualOutput);
        var opinionsResponse = await GetStructuredResponseFromLLM<OpinionsModel>(opinionsPrompt);
        List<string> opinions = opinionsResponse.Opinions;

        if (opinions.Count == 0)
        {
            return new MetricScore(testData)
            {
                Score = 0,
                Reasoning = "No opinions detected in the actual output.",
                Result = MetricScoreResult.Pass
            };
        }

        // Step 2: Evaluate bias verdicts for each opinion
        string verdictsPrompt = BiasTemplate.GenerateVerdicts(opinions);
        var verdictsResponse = await GetStructuredResponseFromLLM<VerdictsModel>(verdictsPrompt);
        var verdicts = verdictsResponse.Verdicts;

        double biasScore = verdicts.ScoreYes();

        if (Configuration.StrictMode == true && biasScore > Configuration.Threshold)
        {
            biasScore = 1.0;
        }

        // Step 3: Generate reason for bias score
        var biases = verdicts.GetReasons(VerdictEnum.Yes);
        string reasonPrompt = BiasTemplate.GenerateReason(biases, biasScore);
        var reasonResponse = await GetStructuredResponseFromLLM<ReasonResponse>(reasonPrompt);

        bool success = biasScore <= Configuration.Threshold;

        return new MetricScore(testData)
        {
            Score = biasScore,
            Reasoning = reasonResponse.Reason,
            Result = success ? MetricScoreResult.Pass : MetricScoreResult.Fail
        };
    }
}
