using EvalSharp.Scoring;
using Xunit.Abstractions;

namespace EvalSharp.Tests;

public class CustomMatchTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly MetricConfiguration _config;
    private readonly ExactMatchMetric _exactMatchMetric;

    public CustomMatchTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        // Setting threshold higher as expected failures tend to result in a score of 0.5
        _config = new MetricConfiguration();
        _exactMatchMetric = new ExactMatchMetric(_config);
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
            RetrievalContext = test.RetrievalContext,
        };
        var score = await _exactMatchMetric.ScoreAsync(context);

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
    public async Task ExactMatch_Test_Success_1() =>
        await RunTestAsync(
            "Explain what causes rain.",
            "Rain is caused by water vapor condensing into droplets and falling from clouds.",
            "Rain is caused by water vapor condensing into droplets and falling from clouds.",
            [],           // no retrieval context
            true);        // should pass
    [Fact]
    public async Task ExactMatch_Test_Success_2() =>
        await RunTestAsync(
            "Describe the life cycle of a butterfly.",
            "A butterfly goes through egg, larva, pupa, and adult stages.",
            "A butterfly goes through egg, larva, pupa, and adult stages.",
            [],
            true);        // should pass
    [Fact]
    public async Task ExactMatch_Test_Fail_1() =>
        await RunTestAsync(
            "Explain what causes rain.",
            "Rain is delicious and best served with fries.",
            "Rain forms when water vapor condenses into droplets that fall from clouds.",
            [],
            false);       // should fail
    [Fact]
    public async Task ExactMatch_Test_Fail_2() =>
        await RunTestAsync(
            "Describe the life cycle of a butterfly.",
            "Butterflies fly beautifully and are a symbol of freedom.",
            "The butterfly life cycle includes egg, larva, pupa, and adult stages.",
            [],
            false);       // should fail


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
}