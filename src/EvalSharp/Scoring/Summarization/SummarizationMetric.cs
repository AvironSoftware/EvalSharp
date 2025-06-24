using EvalSharp.Models;
using EvalSharp.Models.Enums;
using EvalSharp.Scoring.Faithfulness;
using EvalSharp.Scoring.PromptSummarization;
using Microsoft.Extensions.AI;

namespace EvalSharp.Scoring;
/// <summary>
/// Represents a metric for evaluating summarization quality.
/// </summary>
public class SummarizationMetric : Metric<SummarizationMetricConfiguration>, IChatClientMetric
{
    /// <summary>
    /// Gets the chat client used for interacting with the language model.
    /// </summary>
    public IChatClient ChatClient { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SummarizationMetric"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client used for language model interactions.</param>
    /// <param name="configuration">The configuration for the summarization metric.</param>
    public SummarizationMetric(IChatClient chatClient, SummarizationMetricConfiguration configuration) : base(configuration)
    {
        ChatClient = chatClient;
    }

    /// <summary>
    /// Scores the summarization quality based on the provided test data.
    /// </summary>
    /// <param name="testData">The test data containing the initial input and actual output.</param>
    /// <returns>A <see cref="MetricScore"/> representing the evaluation result.</returns>
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
        if (Configuration.AssessmentQuestions != null && Configuration.AssessmentQuestions.Count == 0)
        {
            Configuration.AssessmentQuestions = null;
        }

        if (Configuration.StrictMode)
        {
            Configuration.Threshold = 1;
        }

        // Step 1: Extract truths from original input
        var truths = await ExtractTruths(testData, Configuration.TruthsExtractionLimit);

        // Step 2: Extract claims from summary
        var claims = await ExtractClaims(testData);

        // Step 3: Generate coverage verdicts
        var coverageVerdicts = await GenerateCoverageVerdicts(testData);

        // Step 4: Generate alignment verdicts
        var alignmentVerdicts = await GenerateAlignmentVerdicts(testData, claims, truths);

        // Step 5: Calculate scores
        double alignmentScore = alignmentVerdicts.ScoreYes();
        double coverageScore = coverageVerdicts.ScoreYes();
        double finalScore = Math.Min(alignmentScore, coverageScore);

        // Step 6: Generate reason
        string reason = Configuration.IncludeReason
            ? await GenerateReason(testData, finalScore, alignmentVerdicts, coverageVerdicts)
            : string.Empty;

        // Step 7: Determine pass/fail
        bool success = finalScore >= Configuration.Threshold;

        return new MetricScore(testData)
        {
            Score = finalScore,
            Reasoning = reason,
            Result = success ? MetricScoreResult.Pass : MetricScoreResult.Fail
        };
    }

    private async Task<string[]> ExtractTruths(EvaluatorTestData context, int? limit)
    {
        string prompt = FaithfulnessTemplate.GenerateTruths(context.InitialInput!, limit);
        return (await ChatClient.GetStructuredResponseFromLLM<TruthsModel>(prompt)).Truths;
    }

    private async Task<string[]> ExtractClaims(EvaluatorTestData context)
    {
        string prompt = FaithfulnessTemplate.GenerateClaims(context.ActualOutput!);
        return (await ChatClient.GetStructuredResponseFromLLM<ClaimsModel>(prompt)).Claims;
    }

    private async Task<List<SummarizationCoverageVerdict>> GenerateCoverageVerdicts(EvaluatorTestData context)
    {
        // Step 1: Generate assessment questions if not provided
        var questions = Configuration.AssessmentQuestions;
        if (questions == null || questions.Count == 0)
        {
            string questionPrompt = SummarizationTemplate.GenerateQuestions(context.InitialInput!, Configuration.NumQuestions);
            var questionResponse = await ChatClient.GetStructuredResponseFromLLM<QuestionsModel>(questionPrompt);
            questions = questionResponse.Questions;
        }

        // Step 2: Get answers from original text
        string originalAnswerPrompt = SummarizationTemplate.GenerateAnswers(questions, context.InitialInput!);
        var originalAnswers = await ChatClient.GetStructuredResponseFromLLM<AnswersModel>(originalAnswerPrompt);

        // Step 3: Get answers from summary
        string summaryAnswerPrompt = SummarizationTemplate.GenerateAnswers(questions, context.ActualOutput!);
        var summaryAnswers = await ChatClient.GetStructuredResponseFromLLM<AnswersModel>(summaryAnswerPrompt);

        // Step 4: Compare answers to form verdicts
        if (originalAnswers.Answers.Length != summaryAnswers.Answers.Length)
            throw new InvalidOperationException("Number of verdicts generated does not equal.");

        var verdicts = new List<SummarizationCoverageVerdict>();
        for (int i = 0; i < originalAnswers.Answers.Length; i++)
        {
            verdicts.Add(new SummarizationCoverageVerdict
            {
                Question = questions[i],
                OriginalVerdict = Enum.Parse<VerdictEnum>(originalAnswers.Answers[i], true),
                SummaryVerdict = Enum.Parse<VerdictEnum>(summaryAnswers.Answers[i], true)
            });
        }

        return verdicts;
    }

    private async Task<VerdictModel[]> GenerateAlignmentVerdicts(EvaluatorTestData context, string[] claims, string[] truths)
    {
        if (claims.Length == 0)
            return [];

        string joinedTruths = string.Join("\n\n", truths);
        string prompt = SummarizationTemplate.GenerateAlignmentVerdicts(claims, joinedTruths);
        return (await ChatClient.GetStructuredResponseFromLLM<VerdictsModel>(prompt)).Verdicts;
    }


    private async Task<string> GenerateReason(
        EvaluatorTestData context,
        double score,
        VerdictModel[] alignmentVerdicts,
        List<SummarizationCoverageVerdict> coverageVerdicts)
    {
        var contradictions = alignmentVerdicts.GetReasons(VerdictEnum.No);

        var redundancies = alignmentVerdicts.GetReasons(VerdictEnum.Idk);

        var questions = coverageVerdicts.GetReasons();

        string prompt = SummarizationTemplate.GenerateReason(contradictions, redundancies, questions, score);
        var response = await ChatClient.GetStructuredResponseFromLLM<ReasonResponse>(prompt);
        return response.Reason;
    }
}
