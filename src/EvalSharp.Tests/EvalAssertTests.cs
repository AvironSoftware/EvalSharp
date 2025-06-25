using EvalSharp.Models;
using EvalSharp.Scoring;
using Microsoft.Extensions.AI;
using Xunit.Abstractions;

namespace EvalSharp.Tests;

[Trait("Category","LLM")]
public class EvalAssertTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    public EvalAssertTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }


    [Fact]
    public async Task Testing_Assert_Two_Tests()
    {
        var context = new EvaluatorTestData
        {
            InitialInput = "Summarize the meeting.",
            ActualOutput = "The meeting summary is provided below...",
        };

        var ar_config = new AnswerRelevancyMetricConfiguration
        {
            IncludeReason = true,
            Threshold = 0.9 // Set a threshold for the test
        };

        var geval_config = new GEvalMetricConfiguration
        {
            Threshold = 0.5, // Set a threshold for the test
            Criteria = "Does the output correctly explain concepts, events, or processes based on the input prompt?"
        };



        var metrics = new List<Metric>
        {
            new AnswerRelevancyMetric(ChatClient.GetInstance(), ar_config),
            new GEvalMetric(ChatClient.GetInstance(), geval_config)
        };

        await EvalTest.AssertAsync(context, metrics, _testOutputHelper.WriteLine);
    }

    [Fact]
    public async Task Testing_Assert_Two_Tests_Console()
    {
        var context = new EvaluatorTestData
        {
            InitialInput = "Summarize the meeting.",
            ActualOutput = "The meeting summary is provided below...",
        };

        var ar_config = new AnswerRelevancyMetricConfiguration
        {
            IncludeReason = true,
            Threshold = 0.9 // Set a threshold for the test
        };

        var geval_config = new GEvalMetricConfiguration
        {
            Threshold = 0.5, // Set a threshold for the test
            Criteria = "Does the output correctly explain concepts, events, or processes based on the input prompt?"
        };



        var metrics = new List<Metric>
        {
            new AnswerRelevancyMetric(ChatClient.GetInstance(), ar_config),
            new GEvalMetric(ChatClient.GetInstance(), geval_config)
        };

        await EvalTest.AssertAsync(context, metrics, Console.WriteLine);
    }

    [Fact]
    public async Task Testing_Tool_Correctness()
    {
        var context = new EvaluatorTestData
        {
            ToolsCalled = new List<ToolCall>
            {
                new ToolCall { Name = "product_lookup", InputParameters = new Dictionary<string, object?> { { "product_id", 101 } } },
                new ToolCall
                {
                    Name = "add_to_cart",
                    InputParameters = new Dictionary<string, object?> { { "product_id", 101 }, { "quantity", 1 } }
                }
            },
            ExpectedTools = new List<ToolCall>
            {
                new ToolCall { Name = "product_lookup", InputParameters = new Dictionary<string, object?> { { "product_id", 101 } } },
                new ToolCall {
                    Name = "add_to_cart",
                    InputParameters = new Dictionary<string, object?> { { "product_id", 101 }, { "quantity", 1 } }
                }
            }
        };

        var tc_config = new ToolCorrectnessMetricConfiguration
        {
            ShouldExactMatch = true,
            Threshold = 0.6,
        };

        var metric = new ToolCorrectnessMetric(tc_config);


        await EvalTest.AssertAsync(context, metric, _testOutputHelper.WriteLine);
    }

    [Fact]
    public async Task Testing_Assert_Custom_Metric()
    {
        var context = new EvaluatorTestData
        {
            InitialInput = "What's the return policy for electronics?",
            ActualOutput = "Our return policy allows electronics to be returned within 30 days with the original receipt.",
            ExpectedOutput = "Electronics can be returned within 30 days if you have the original receipt.",
            RetrievalContext = ["Returns are accepted within 30 days for electronics, provided you have the original receipt."]
        };

        var custom_config = new FaithfulRelevancyConfiguration { IncludeReason = true, Threshold = 0.6 };

        var metric = new FaithfulRelevancyMetric(ChatClient.GetInstance(), custom_config);

        await EvalTest.AssertAsync(context, metric, _testOutputHelper.WriteLine);
    }

    [Fact]
    public async Task Testing_Assert_Custom_Metric_Two()
    {
        var context = new EvaluatorTestData
        {
            InitialInput = "Explain what causes rain.",
            ActualOutput = "Rain is caused by water vapor condensing into droplets and falling from clouds.",
            ExpectedOutput = "Rain is caused by water vapor condensing into droplets and falling from clouds.",
        };

        var custom_config = new MetricConfiguration();

        var metric = new ExactMatchMetric(custom_config);

        await EvalTest.AssertAsync(context, metric, _testOutputHelper.WriteLine);
    }
}

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


public class ExactMatchMetric : Metric<MetricConfiguration>
{
    public ExactMatchMetric(MetricConfiguration configuration) : base(configuration)
    {
    }

    public override async Task<MetricScore> ScoreAsync(EvaluatorTestData testData)
    {
        var matchMetric = MatchMetric.Exact();

        var matchResult = await matchMetric.ScoreAsync(testData);

        return matchResult;
    }
}