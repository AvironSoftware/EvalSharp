using EvalSharp.Scoring;
using Microsoft.Extensions.AI;
using Xunit.Abstractions;

namespace EvalSharp.Tests;

public class CustomComboTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly FaithfulRelevancyConfiguration _config;
    private readonly FaithfulRelevancyMetric _faithfulRelevancyMetric;

    public CustomComboTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        // Setting threshold higher as expected failures tend to result in a score of 0.5
        _config = new FaithfulRelevancyConfiguration { IncludeReason = true, Threshold = 0.6 };
        _faithfulRelevancyMetric = new FaithfulRelevancyMetric(ChatClient.GetInstance(), _config);
    }

    private async Task RunTestAsync(string initialInput, string actualOutput, string expectedOutput, List<string> retrievalContext, bool shouldPass)
    {
        var test = new
        {
            InitialInput = initialInput,
            ActualOutput = actualOutput,
            ExpectedOutput = expectedOutput,
            RetrievalContext = retrievalContext
        };
        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput,
            RetrievalContext = test.RetrievalContext
        };
        var score = await _faithfulRelevancyMetric.ScoreAsync(context);

        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.Score}");
        _testOutputHelper.WriteLine($"Result: {score.Result}");

        if (shouldPass)
        {
            Assert.True(score.Score >= _config.Threshold);
            Assert.Equal(MetricScoreResult.Pass, score.Result);
        }
        else
        {
            Assert.True(score.Score < _config.Threshold);
            Assert.Equal(MetricScoreResult.Fail, score.Result);
        }
    }

    [Fact]
    public async Task Joint_Test_Success_1() =>
        await RunTestAsync(
            "What's the return policy for electronics?",
            "Our return policy allows electronics to be returned within 30 days with the original receipt.",
            "Electronics can be returned within 30 days if you have the original receipt.",
            ["Returns are accepted within 30 days for electronics, provided you have the original receipt."],
            true);   // should pass
    [Fact]
    public async Task Joint_Test_Success_2() =>
        await RunTestAsync(
            "Does the medication cause drowsiness?",
            "Yes, one of the known side effects of this medication is drowsiness.",
            "Drowsiness is a common side effect of this medication.",
            ["Common side effects include drowsiness, dry mouth, and dizziness."],
            true);   // should pass
    [Fact]
    public async Task Joint_Test_Faithfulness_Fail() =>
        await RunTestAsync(
            "What's the return policy for electronics?",
            "All electronics have a 1-year return window.",
            "Electronics can be returned within 30 days if you have the original receipt.",
            ["Returns are accepted within 30 days for electronics, provided you have the original receipt."],
            false);  // should fail (faithfulness)
    [Fact]
    public async Task Joint_Test_Relevancy_Fail() =>
        await RunTestAsync(
            "Does the medication cause drowsiness?",
            "The medication should be stored at room temperature.",
            "Drowsiness is a common side effect of this medication.",
            ["Common side effects include drowsiness, dry mouth, and dizziness."],
            false);  // should fail (relevancy)
    [Fact]
    public async Task Joint_Test_Both_Fail_1() =>
        await RunTestAsync(
            "What is the interest rate for savings accounts?",
            "Our savings accounts include unlimited free movie tickets.",
            "The current interest rate for savings accounts is 2.5% annually.",
            ["Savings accounts accrue interest at a rate of 2.5% per year."],
            false);  // should fail (both metrics)
    [Fact]
    public async Task Joint_Test_Both_Fail_2() =>
        await RunTestAsync(
            "What are the side effects of Ibuprofen?",
            "Ibuprofen is a fruit high in vitamin C.",
            "Common side effects of Ibuprofen include nausea, dizziness, and stomach pain.",
            ["Ibuprofen side effects: nausea, dizziness, indigestion, and stomach pain."],
            false);  // should fail (both metrics)


    public class FaithfulRelevancyConfiguration : MetricConfiguration
    {
        public bool IncludeReason { get; set; }
    }

    public class FaithfulRelevancyMetric : Metric<FaithfulRelevancyConfiguration>
    {
        private readonly IChatClient _chatClient;
        public FaithfulRelevancyMetric(IChatClient chatClient, FaithfulRelevancyConfiguration configuration) : base(configuration)
        {
            _chatClient = chatClient;
        }

        public override async Task<MetricScore> ScoreAsync(EvaluatorTestData testData)
        {
            var relevancyMetric = new AnswerRelevancyMetric(_chatClient, new AnswerRelevancyMetricConfiguration());
            var faithfullnessMetric = new FaithfulnessMetric(_chatClient, new FaithfulnessMetricConfiguration());
            

            var relResult = await relevancyMetric.ScoreAsync(testData);
            var faithfullnessResult = await faithfullnessMetric.ScoreAsync(testData);

            return SetScoreReasonSuccess(testData, relResult, faithfullnessResult);
        }

        public MetricScore SetScoreReasonSuccess(EvaluatorTestData context, MetricScore relevancyMetric, MetricScore faithfulnessMetric)
        {
            
            var relevancyScore = relevancyMetric.Score;
            var relevancyReason = relevancyMetric.Reasoning;
            var faithfulnessScore = faithfulnessMetric.Score;
            var faithfulnessReason = faithfulnessMetric.Reasoning;

            // Custom logic to set score
            var compositeScore = Math.Min(relevancyScore, faithfulnessScore);

            var score = Configuration.StrictMode && compositeScore < Configuration.Threshold ? 0 : compositeScore;
            var reason = Configuration.IncludeReason ? relevancyReason + "\n" + faithfulnessReason : null;
            var success = score >= Configuration.Threshold;

            return new MetricScore(context)
            {
                Score = score,
                Reasoning = reason,
                Result = success ? MetricScoreResult.Pass : MetricScoreResult.Fail
            };

        }
    }
}