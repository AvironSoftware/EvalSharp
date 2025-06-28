using EvalSharp.Models;
using EvalSharp.Scoring.TaskCompletion;
using Microsoft.Extensions.AI;

namespace EvalSharp.Scoring;
/// <summary>
/// Represents a metric for evaluating task completion based on user goals and task outcomes.
/// </summary>
public class TaskCompletionMetric : LLMAsAJudgeMetric<TaskCompletionMetricConfiguration>, IChatClientMetric
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TaskCompletionMetric"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client used for language model interactions.</param>
    /// <param name="configuration">The configuration for the task completion metric.</param>
    public TaskCompletionMetric(IChatClient chatClient, TaskCompletionMetricConfiguration configuration) : base(configuration, chatClient)
    {
    }

    /// <summary>
    /// Scores the given test data based on task completion criteria.
    /// </summary>
    /// <param name="testData">The test data containing input, output, and tool usage information.</param>
    /// <returns>A <see cref="MetricScore"/> representing the evaluation result.</returns>
    /// <exception cref="ArgumentException">Thrown if the InitialInput or ActualOutput is null or whitespace.</exception>
    /// <exception cref="ArgumentException">Thrown if ToolsCalled is null</exception>
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
        if (testData.ToolsCalled == null)
        {
            throw new ArgumentException($"Missing required {nameof(testData.ToolsCalled)} parameter.");
        }

        // Step 1: Extract User Goal and Task Outcome
        var (userGoal, taskOutcome) = await ExtractGoalAndOutcome(testData);

        // Step 2: Generate Verdict
        var (verdict, reason) = await GenerateVerdict(testData, userGoal, taskOutcome);

        // Step 3: Calculate Score
        double score = CalculateScore(verdict);

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
    /// Extracts the user goal and task outcome from the test data.
    /// </summary>
    /// <param name="context">The test data context.</param>
    /// <returns>A tuple containing the user goal and task outcome.</returns>
    private async Task<(string UserGoal, string TaskOutcome)> ExtractGoalAndOutcome(EvaluatorTestData context)
    {
        string prompt = TaskCompletionTemplate.GenerateGoalAndOutcome(
            context.InitialInput!,
            context.ActualOutput!,
            context.ToolsCalled!
        );

        var response = await GetStructuredResponseFromLLM<GoalAndOutcome>(prompt);
        return (response.UserGoal, response.TaskOutcome);
    }

    /// <summary>
    /// Generates a verdict and reasoning based on the user goal and task outcome.
    /// </summary>
    /// <param name="context">The test data context.</param>
    /// <param name="userGoal">The user's goal extracted from the test data.</param>
    /// <param name="taskOutcome">The task outcome extracted from the test data.</param>
    /// <returns>A tuple containing the verdict and reasoning.</returns>
    private async Task<(double Verdict, string? Reason)> GenerateVerdict(EvaluatorTestData context, string userGoal, string taskOutcome)
    {
        string prompt = TaskCompletionTemplate.GenerateVerdict(userGoal, taskOutcome);

        var response = await GetStructuredResponseFromLLM<ScoredVerdictModel>(prompt);
        return (response.Verdict, response.Reason);
    }

    /// <summary>
    /// Calculates the score based on the verdict and configuration settings.
    /// </summary>
    /// <param name="verdict">The verdict value.</param>
    /// <returns>The calculated score.</returns>
    private double CalculateScore(double verdict)
    {
        return Configuration.StrictMode == true && verdict < Configuration.Threshold ? 0 : verdict;
    }
}
