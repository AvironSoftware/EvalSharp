using EvalSharp.Models;
using EvalSharp.Models.Enums;
using EvalSharp.Scoring.Faithfulness;
using Microsoft.Extensions.AI;

namespace EvalSharp.Scoring;

/// <summary>
/// Represents a metric for evaluating the faithfulness of a chat client's responses.
/// </summary>
public class FaithfulnessMetric : LLMAsAJudgeMetric<FaithfulnessMetricConfiguration>, IChatClientMetric
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FaithfulnessMetric"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client used for generating responses.</param>
    /// <param name="configuration">The configuration settings for the faithfulness metric.</param>
    public FaithfulnessMetric(IChatClient chatClient, FaithfulnessMetricConfiguration configuration) : base(configuration, chatClient)
    {
    }

    /// <summary>
    /// Scores the faithfulness of the test data based on extracted truths, claims, and their evaluation.
    /// </summary>
    /// <param name="testData">The test data containing input, output, and retrieval context.</param>
    /// <returns>A <see cref="MetricScore"/> representing the evaluation result.</returns>
    /// <exception cref="ArgumentException">Thrown when ActualOutput is null or whitespace.</exception>
    /// <exception cref="ArgumentException">Thrown when RetrievalContext is null.</exception>
    public override async Task<MetricScore> ScoreAsync(EvaluatorTestData testData)
    {
        if (string.IsNullOrWhiteSpace(testData.ActualOutput))
        {
            throw new ArgumentException("String cannot be null or whitespace.", nameof(testData.ActualOutput));
        }
        if (testData.RetrievalContext == null)
        {
            throw new ArgumentException("List cannot be null.", nameof(testData.RetrievalContext));
        }

        // Step 1: Extract Truths
        var truths = await ExtractTruths(testData, Configuration.TruthsExtractionLimit);

        // Step 2: Extract Claims
        var claims = await ExtractClaims(testData);

        // Step 3: Evaluate Claims Against Truths
        var verdicts = await EvaluateClaims(testData, claims, truths);

        // Step 4: Compute Score
        var score = verdicts.ScoreYesIdk();

        // Step 5: Generate Explanation (if enabled)
        var reason = Configuration.IncludeReason ? await GenerateReason(testData, score, verdicts) : "";

        // Step 6: Determine success
        bool success = score >= (Configuration.StrictMode == true ? 1 : Configuration.Threshold);

        return new MetricScore(testData)
        {
            Score = score,
            Reasoning = reason,
            Result = success ? MetricScoreResult.Pass : MetricScoreResult.Fail
        };
    }

    /// <summary>
    /// Extracts truths from the retrieval context of the test data.
    /// </summary>
    /// <param name="context">The test data containing the retrieval context.</param>
    /// <param name="truthsExtractionLimit">The maximum number of truths to extract.</param>
    /// <returns>An array of extracted truths.</returns>
    private async Task<string[]> ExtractTruths(EvaluatorTestData context, int? truthsExtractionLimit)
    {
        string joinedContext = string.Join("\n\n", context.RetrievalContext!);
        string prompt = FaithfulnessTemplate.GenerateTruths(joinedContext, truthsExtractionLimit);
        return (await GetStructuredResponseFromLLM<TruthsModel>(prompt)).Truths;
    }

    /// <summary>
    /// Extracts claims from the actual output of the test data.
    /// </summary>
    /// <param name="context">The test data containing the actual output.</param>
    /// <returns>An array of extracted claims.</returns>
    private async Task<string[]> ExtractClaims(EvaluatorTestData context)
    {
        string prompt = FaithfulnessTemplate.GenerateClaims(context.ActualOutput!);
        return (await GetStructuredResponseFromLLM<ClaimsModel>(prompt)).Claims;
    }

    /// <summary>
    /// Evaluates the claims against the extracted truths.
    /// </summary>
    /// <param name="context">The test data containing the retrieval context.</param>
    /// <param name="claims">The claims to evaluate.</param>
    /// <param name="truths">The truths to evaluate against.</param>
    /// <returns>An array of verdicts for each claim.</returns>
    private async Task<VerdictModel[]> EvaluateClaims(EvaluatorTestData context, string[] claims, string[] truths)
    {
        if (claims.Length == 0) return Array.Empty<VerdictModel>();

        string prompt = FaithfulnessTemplate.GenerateVerdicts(claims, truths);
        return (await GetStructuredResponseFromLLM<VerdictsModel>(prompt)).Verdicts;
    }

    /// <summary>
    /// Generates reasoning for the score based on contradictions in the verdicts.
    /// </summary>
    /// <param name="context">The test data containing the retrieval context.</param>
    /// <param name="score">The computed score.</param>
    /// <param name="verdicts">The verdicts for the claims.</param>
    /// <returns>A string representing the reasoning for the score.</returns>
    private async Task<string> GenerateReason(EvaluatorTestData context, double score, VerdictModel[] verdicts)
    {
        var contradictions = verdicts.GetReasons(VerdictEnum.No);
        string prompt = FaithfulnessTemplate.GenerateReason(score, contradictions);
        var reasonResponse = await GetStructuredResponseFromLLM<ReasonResponse>(prompt);
        return reasonResponse.Reason;
    }
}
