using EvalSharp.Scoring;
using EvalSharp.Scoring.Hallucination;
using Xunit.Abstractions;

namespace EvalSharp.Tests;

public class HallucinationTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HallucinationMetricConfiguration _config;
    private readonly HallucinationMetric _hallucinationMetric;

    public HallucinationTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _config = new HallucinationMetricConfiguration { IncludeReason = true, Threshold = 0.5 };
        _hallucinationMetric = new HallucinationMetric(ChatClient.GetInstance(),_config);
    }

    private async Task RunTestAsync(string input, string actualOutput, List<string> context, bool shouldPass)
    {
        var test = new
        {
            InitialInput = input,
            ActualOutput = actualOutput,
            Context = context
        };
        var evalContext = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            Context = test.Context
        };
        var score = await _hallucinationMetric.ScoreAsync(evalContext);

        _testOutputHelper.WriteLine($"Total score: {score.Score}");
        _testOutputHelper.WriteLine($"Result: {score.Result}");

        if (shouldPass)
        {
            Assert.True(score.Score <= _config.Threshold);
            Assert.Equal(MetricScoreResult.Pass, score.Result);
        }
        else
        {
            Assert.True(score.Score > _config.Threshold);
            Assert.Equal(MetricScoreResult.Fail, score.Result);
        }
    }

    // --- Successful Hallucination Tests ---
    [Fact]
    public async Task Hallucination_FactChecking_Success() =>
        await RunTestAsync("Who was the first president of the United States?",
            "The first president of the United States was George Washington.",
            new List<string> { "Verified historical records state George Washington was the first U.S. president." },
            true);

    [Fact]
    public async Task Hallucination_Legal_Success() =>
        await RunTestAsync("Does California require a business license for all businesses?",
            "California's licensing requirements vary by industry and location. It's best to check with the California Business Portal.",
            new List<string> { "California Business Portal states that licensing rules depend on business type and location." },
            true);

    [Fact]
    public async Task Hallucination_Medical_Success() =>
        await RunTestAsync("Can I take ibuprofen if I have a stomach ulcer?",
            "Ibuprofen is not recommended for individuals with stomach ulcers as it may worsen the condition.",
            new List<string> { "Medical guidelines warn against NSAID use in patients with ulcers." },
            true);

    [Fact]
    public async Task Hallucination_CustomerSupport_Success() =>
        await RunTestAsync("How can I reset my password on your website?",
            "Go to the login page and click 'Forgot Password' to receive a reset link via email.",
            new List<string> { "Official company FAQ explains the password reset process." },
            true);

    [Fact]
    public async Task Hallucination_CodeGeneration_Success() =>
        await RunTestAsync("How do I open a file in Python?",
            "You can open a file in Python using `open('file.txt', 'r')`.",
            new List<string> { "Python documentation states that `open(filename, mode)` is used to open files." },
            true);

    // --- Failing Hallucination Tests ---
    [Fact]
    public async Task Hallucination_FactChecking_Fail() =>
        await RunTestAsync("Who was the first president of the United States?",
            "The first president of the United States was John Adams.",
            new List<string> { "Verified historical records state George Washington was the first U.S. president." },
            false);

    [Fact]
    public async Task Hallucination_Legal_Fail() =>
        await RunTestAsync("Does California require a business license for all businesses?",
            "All businesses in California require a federal license to operate.",
            new List<string> { "California Business Portal states that licensing rules depend on business type and location." },
            false);

    [Fact]
    public async Task Hallucination_Medical_Fail() =>
        await RunTestAsync("Can I take ibuprofen if I have a stomach ulcer?",
            "Ibuprofen is perfectly safe for ulcer patients and can help reduce inflammation.",
            new List<string> { "Medical guidelines warn against NSAID use in patients with ulcers." },
            false);

    [Fact]
    public async Task Hallucination_CustomerSupport_Fail() =>
        await RunTestAsync("How can I reset my password on your website?",
            "Simply email our CEO, and he will reset it for you.",
            new List<string> { "Official company FAQ explains the password reset process." },
            false);

    [Fact]
    public async Task Hallucination_CodeGeneration_Fail() =>
        await RunTestAsync("How do I open a file in Python?",
            "You can open a file using `file.open('file.txt', 'r')`.",
            new List<string> { "Python documentation states that `open(filename, mode)` is used to open files." },
            false);
}