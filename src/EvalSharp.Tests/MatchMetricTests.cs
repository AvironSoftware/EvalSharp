using EvalSharp.Scoring;

namespace EvalSharp.Tests.MatchMetricTests;

public class MatchMetricTests
{
    [Fact]
    public async Task ExactMatch_ShouldPass_WhenOutputsAreIdentical()
    {
        // Arrange
        var expected = "Hello";
        var actual = "Hello";
        var test = new
        {
            ActualOutput = actual,
            ExpectedOutput = expected,
            InitialInput = expected
        };
        var context = new EvaluatorTestData
        {
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput,
            InitialInput = test.InitialInput
        };
        var metric = MatchMetric.Exact();

        // Act
        var score = await metric.ScoreAsync(context);

        // Assert
        Assert.Equal(1, score.Score);
        Assert.Equal(MetricScoreResult.Pass, score.Result);
        Assert.Contains("matches", score.Reasoning);
    }

    [Fact]
    public async Task ExactMatch_ShouldFail_WhenOutputsDiffer()
    {
        // Arrange
        var expected = "Hello";
        var actual = "hello";
        var test = new
        {
            ActualOutput = actual,
            ExpectedOutput = expected,
            InitialInput = expected
        };
        var context = new EvaluatorTestData
        {
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput,
            InitialInput = test.InitialInput
        };
        var metric = MatchMetric.Exact();

        // Act
        var score = await metric.ScoreAsync(context);

        // Assert
        Assert.Equal(0, score.Score);
        Assert.Equal(MetricScoreResult.Fail, score.Result);
        Assert.Contains("does not match", score.Reasoning);
    }

    [Fact]
    public async Task RegexMatch_ShouldPass_WhenSingleMatchAndEqualsExpected()
    {
        // Arrange
        var expected = "world";
        var actual = "Hello world, how are you?";
        var test = new
        {
            ActualOutput = actual,
            ExpectedOutput = expected,
            InitialInput = expected
        };
        var context = new EvaluatorTestData
        {
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput,
            InitialInput = test.InitialInput
        };
        var metric = MatchMetric.Regex("world");

        // Act
        var score = await metric.ScoreAsync(context);

        // Assert
        Assert.Equal(1, score.Score);
        Assert.Equal(MetricScoreResult.Pass, score.Result);
    }

    [Fact]
    public async Task RegexMatch_ShouldFail_WhenNoMatchFound()
    {
        // Arrange
        var expected = "foo";
        var actual = "Hello world, how are you?";
        var test = new
        {
            ActualOutput = actual,
            ExpectedOutput = expected,
            InitialInput = expected
        };

        var context = new EvaluatorTestData
        {
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput,
            InitialInput = test.InitialInput
        };
        var metric = MatchMetric.Regex("foo");

        // Act
        var score = await metric.ScoreAsync(context);

        // Assert
        Assert.Equal(0, score.Score);
        Assert.Equal(MetricScoreResult.Fail, score.Result);
        Assert.Contains("does not match", score.Reasoning);
    }

    [Fact]
    public async Task RegexMatch_ShouldFail_WhenMultipleMatchesFound()
    {
        // Arrange
        var expected = "foo";
        var actual = "foo foo";  // two occurrences of "foo"
        var test = new
        {
            ActualOutput = actual,
            ExpectedOutput = expected,
            InitialInput = expected
        };
        var context = new EvaluatorTestData
        {
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput,
            InitialInput = test.InitialInput
        };
        var metric = MatchMetric.Regex("foo");

        // Act
        var score = await metric.ScoreAsync(context);

        // Assert
        Assert.Equal(0, score.Score);
        Assert.Equal(MetricScoreResult.Fail, score.Result);
        Assert.Contains("Multiple matches", score.Reasoning);
    }

    [Fact]
    public async Task RegexMatch_CaseInsensitive_ShouldPass_WhenMatchesDifferInCase()
    {
        // Arrange
        var expected = "world";
        var actual = "Hello WORLD, how are you?";
        var test = new
        {
            ActualOutput = actual,
            ExpectedOutput = expected,
            InitialInput = expected
        };
        var context = new EvaluatorTestData
        {
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput,
            InitialInput = test.InitialInput
        };
        // Providing StringComparison.OrdinalIgnoreCase so that case differences are ignored.
        var metric = MatchMetric.Regex("WORLD", StringComparison.OrdinalIgnoreCase);

        // Act
        var score = await metric.ScoreAsync(context);

        // Assert
        Assert.Equal(1, score.Score);
        Assert.Equal(MetricScoreResult.Pass, score.Result);
    }

    [Fact]
    public async Task AfterOccurrenceOfString_ShouldPass_WhenSearchStringFoundAndFollowingTextMatchesExpected()
    {
        // Arrange
        var expected = "Hello";
        var actual = "Some irrelevant text. Answer: Hello";

        var test = new
        {
            ActualOutput = actual,
            ExpectedOutput = expected,
            InitialInput = expected
        };
        var context = new EvaluatorTestData
        {
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput,
            InitialInput = test.InitialInput
        };
        var metric = MatchMetric.AfterString("Answer:");

        // Act
        var score = await metric.ScoreAsync(context);

        // Assert
        Assert.Equal(1, score.Score);
        Assert.Equal(MetricScoreResult.Pass, score.Result);
    }

    [Fact]
    public async Task AfterOccurrenceOfString_ShouldFail_WhenSearchStringNotFound()
    {
        // Arrange
        var expected = "Hello";
        var actual = "Some text without the key string";

        var test = new
        {
            ActualOutput = actual,
            ExpectedOutput = expected,
            InitialInput = expected
        };
        var context = new EvaluatorTestData
        {
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput,
            InitialInput = test.InitialInput
        };
        var metric = MatchMetric.AfterString("Answer:");

        // Act
        var score = await metric.ScoreAsync(context);

        // Assert
        Assert.Equal(0, score.Score);
        Assert.Equal(MetricScoreResult.Fail, score.Result);
        Assert.Contains("was not found", score.Reasoning);
    }

    [Fact]
    public async Task AfterOccurrenceOfString_ShouldFail_WhenMultipleOccurrencesFound()
    {
        // Arrange
        var expected = "foo";
        var actual = "Prefix Answer: foo and then Answer: bar";

        var test = new
        {
            ActualOutput = actual,
            ExpectedOutput = expected,
            InitialInput = expected
        };
        var context = new EvaluatorTestData
        {
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput,
            InitialInput = test.InitialInput
        };
        var metric = MatchMetric.AfterString("Answer:");

        // Act
        var score = await metric.ScoreAsync(context);

        // Assert
        Assert.Equal(0, score.Score);
        Assert.Equal(MetricScoreResult.Fail, score.Result);
        Assert.Contains("found multiple times in the actual output", score.Reasoning);
    }
}