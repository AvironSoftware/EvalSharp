using EvalSharp.Models;
using EvalSharp.Scoring.GEval;
using Microsoft.Extensions.AI;
using OpenAI.Chat;

namespace EvalSharp.Scoring;
/// <summary>
/// Represents a metric for evaluating the output of a language model using GEval.
/// </summary>
public class GEvalMetric : Metric<GEvalMetricConfiguration>, IChatClientMetric
{
    /// <summary>
    /// Gets the chat client used for interacting with the language model.
    /// </summary>
    public IChatClient ChatClient { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GEvalMetric"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client used for interacting with the language model.</param>
    /// <param name="configuration">The configuration for the GEval metric.</param>
    public GEvalMetric(IChatClient chatClient, GEvalMetricConfiguration configuration) : base(configuration)
    {
        ChatClient = chatClient;
    }

    /// <summary>
    /// Configures the metric with a series of evaluation steps.
    /// </summary>
    /// <param name="evaluationSteps">The evaluation steps to use.</param>
    /// <returns>The configured <see cref="GEvalMetric"/> instance.</returns>
    public GEvalMetric WithEvaluationSteps(IEnumerable<string> evaluationSteps)
    {
        Check.IfNullOrEmptyOrStringsHaveNoValue(evaluationSteps, nameof(evaluationSteps));
        Configuration.EvaluationSteps = [.. evaluationSteps];
        return this;
    }

    /// <summary>
    /// Configures the metric with specific evaluation criteria.
    /// </summary>
    /// <param name="criteria">The criteria to use for evaluation.</param>
    /// <returns>The configured <see cref="GEvalMetric"/> instance.</returns>
    public GEvalMetric WithCriteria(string criteria)
    {
        Check.NullOrWhitespaceString(criteria, nameof(criteria));
        Configuration.Criteria = criteria;
        return this;
    }

    /// <summary>
    /// Scores the test data using the configured evaluation steps and criteria.
    /// </summary>
    /// <param name="testData">The test data to evaluate.</param>
    /// <returns>A task representing the asynchronous operation, containing the computed metric score.</returns>
    /// <exception cref="ArgumentException">Thrown when InitialInput or ActualOutput is null or whitespace.</exception>
    /// <exception cref="ArgumentException">Thrown when EvaluationSteps are null or empty AND Criteria is null or whitespace.</exception>
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

        // 1. Determine the evaluation steps.
        List<string> evaluationStepList;
        if (Configuration.EvaluationSteps == null || Configuration.EvaluationSteps.Count == 0)
        {
            Check.NullOrWhitespaceString(Configuration.Criteria);

            // Build the prompt to generate evaluation steps from the criteria.
            string stepsPrompt = GEvalTemplate.GenerateEvaluationSteps(Configuration.Criteria!);

            // Call the LLM to get a structured response with the steps.
            StepsResponse stepsResponse = await ChatClient.GetStructuredResponseFromLLM<StepsResponse>(stepsPrompt);

            // Format steps as a numbered list.
            evaluationStepList = stepsResponse.Steps.Select((step, i) => $"{i + 1}. {step}").ToList();
        }
        else
        {
            evaluationStepList = Configuration.EvaluationSteps;
        }

        var evaluationPrompt = GEvalTemplate.GenerateEvaluation(
                                    testData.InitialInput!,
                                    testData.ActualOutput!,
                                    testData.ExpectedOutput,
                                    evaluationStepList);

        // 4. Call the ChatCompletionService to obtain the evaluation result.
        var (evalResponse, chatMessageContent) = await ChatClient.GetStructuredResponseFromLLMWithOriginalResponse<EvaluationResponse>(
            evaluationPrompt,
            options =>
            {
                //lmao this is the hackiest thing ever
                if (ChatClient.GetType().Name.Contains("OpenAI"))
                {
                    options.RawRepresentationFactory = _ => new ChatCompletionOptions
                    {
                        TopLogProbabilityCount = 20,
                        IncludeLogProbabilities = true
                    };
                }
            });

        // 5. (Optional) Compute a weighted score if log probabilities are available.
        var scoreToUse = GenerateWeightedSummedScore(evalResponse.Score, chatMessageContent);

        // 6. Normalize the score from 0-10 to 0-1.
        double normalizedScore = scoreToUse / 10.0;

        // 7. Apply strict mode logic if enabled.
        bool success;
        if (Configuration.StrictMode)
        {
            if (normalizedScore < 1.0)
            {
                normalizedScore = 0.0;
            }
            success = normalizedScore >= 1.0;
        }
        else
        {
            success = normalizedScore >= Configuration.Threshold;
        }

        // 8. Return the computed metric score.
        return new MetricScore(testData)
        {
            Score = normalizedScore,
            Reasoning = evalResponse.Reason,
            Result = success ? MetricScoreResult.Pass : MetricScoreResult.Fail
        };
    }

    /// <summary>
    /// Computes a weighted summed score based on log probabilities from the chat response.
    /// </summary>
    /// <param name="rawScore">The raw score provided by the evaluation.</param>
    /// <param name="chatMessageContent">The chat response containing log probabilities.</param>
    /// <returns>The computed weighted summed score.</returns>
    private double GenerateWeightedSummedScore(int rawScore, ChatResponse chatMessageContent)
    {
        string rawScoreStr = rawScore.ToString();
        ChatTokenLogProbabilityDetails? scoreToken;

        var foundLogProbs = chatMessageContent.TryGetLogprobs(out var logprobs);
        if (foundLogProbs)
        {
            scoreToken = logprobs!.First(token => token.Token == rawScoreStr);
        }
        else
        {
            return rawScore;
        }

        double minLogProb = Math.Log(0.01);
        Dictionary<int, double> tokenLinearProbabilities = new Dictionary<int, double>();
        double sumLinearProbability = 0.0;

        foreach (var topToken in scoreToken.TopLogProbabilities)
        {
            if (topToken.LogProbability < minLogProb)
                continue;
            // Ensure the token represents a valid integer.
            if (!int.TryParse(topToken.Token, out int tokenScore))
                continue;
            double linearProb = Math.Exp(topToken.LogProbability);
            if (tokenLinearProbabilities.ContainsKey(tokenScore))
            {
                tokenLinearProbabilities[tokenScore] += linearProb;
            }
            else
            {
                tokenLinearProbabilities[tokenScore] = linearProb;
            }
            sumLinearProbability += linearProb;
        }

        double sumOfWeightedScores = tokenLinearProbabilities.Sum(kvp => kvp.Key * kvp.Value);
        double weightedSummedScore = sumOfWeightedScores / sumLinearProbability;
        return weightedSummedScore;
    }
}
