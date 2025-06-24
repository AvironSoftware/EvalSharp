
using EvalSharp.Scoring;
using EvalSharp.Scoring.PromptAlignment;
using Xunit.Abstractions;

namespace EvalSharp.Tests;

public class PromptAlignmentTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public PromptAlignmentTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    private async Task RunTestAsync(string initialInput, string actualOutput, string expectedOutput, List<string> instructions, bool shouldPass)
    {
        var config = new PromptAlignmentMetricConfiguration { IncludeReason = true, Threshold = 0.5, PromptInstructions = instructions };
        var promptAlignmentMetric = new PromptAlignmentMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = initialInput,
            ActualOutput = actualOutput,
            ExpectedOutput = expectedOutput,
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput
        };

        var score = await promptAlignmentMetric.ScoreAsync(context);

        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.Score}");
        _testOutputHelper.WriteLine($"Result: {score.Result}");

        if (shouldPass)
        {
            Assert.True(score.Score >= config.Threshold);
            Assert.Equal(MetricScoreResult.Pass, score.Result);
        }
        else
        {
            Assert.True(score.Score < config.Threshold);
            Assert.Equal(MetricScoreResult.Fail, score.Result);
        }
    }

    [Fact]
    public async Task PromptAlign_Test_MedicalExplanation_Success() =>
        await RunTestAsync(
            "Summarize this medical report for a patient in plain English. Do not include any medical jargon.",
            "The report says your heart is healthy and there are no signs of serious problems. Your blood pressure and cholesterol levels are normal.",
            "The report says your heart is healthy and there are no signs of serious problems. Your blood pressure and cholesterol levels are normal.",
            new List<string> { "Summarize for a patient", "Use plain English", "Avoid medical jargon" },
            true);

    [Fact]
    public async Task PromptAlign_Test_MedicalExplanation_Fail() =>
        await RunTestAsync(
            "Summarize this medical report for a patient in plain English. Do not include any medical jargon.",
            "The patient has no signs of myocardial infarction or arrhythmia, and LDL/HDL levels are within normal limits.",
            "The report says your heart is healthy and there are no signs of serious problems. Your blood pressure and cholesterol levels are normal.",
            new List<string> { "Summarize for a patient", "Use plain English", "Avoid medical jargon" },
            false);

    [Fact]
    public async Task PromptAlign_Test_CustomerSupport_Success() =>
        await RunTestAsync(
            "Apologize, explain the refund policy, and provide a link to the return form.",
            "We're very sorry for the inconvenience. Our refund policy allows returns within 30 days. You can fill out the return form here: example.com/returns",
            "We're very sorry for the inconvenience. Our refund policy allows returns within 30 days. You can fill out the return form here: example.com/returns",
            new List<string> { "Include apology", "Explain refund policy", "Provide link to return form" },
            true);

    [Fact]
    public async Task PromptAlign_Test_CustomerSupport_Fail() =>
        await RunTestAsync(
            "Apologize, explain the refund policy, and provide a link to the return form.",
            "You can read our refund policy on our website. Let us know if you have any questions.",
            "We're very sorry for the inconvenience. Our refund policy allows returns within 30 days. You can fill out the return form here: example.com/returns",
            new List<string> { "Include apology", "Explain refund policy", "Provide link to return form" },
            false);

    [Fact]
    public async Task PromptAlign_Test_TutorExplanation_Success() =>
        await RunTestAsync(
            "Explain the Pythagorean Theorem in 3 sentences using a real-world example.",
            "The Pythagorean Theorem says that in a right triangle, the square of the hypotenuse equals the sum of the squares of the other two sides. For example, if a ladder leans against a wall and forms a right triangle, you can use this formula to find the ladder's length. This helps in construction and home improvement tasks.",
            "The Pythagorean Theorem says that in a right triangle, the square of the hypotenuse equals the sum of the squares of the other two sides. For example, if a ladder leans against a wall and forms a right triangle, you can use this formula to find the ladder's length. This helps in construction and home improvement tasks.",
            new List<string> { "Explain in 3 sentences", "Use a real-world example" },
            true);

    [Fact]
    public async Task PromptAlign_Test_TutorExplanation_Fail() =>
        await RunTestAsync(
            "Explain the Pythagorean Theorem in 3 sentences using a real-world example.",
            "The Pythagorean Theorem is a² + b² = c². It's a formula. Thanks.",
            "The Pythagorean Theorem says that in a right triangle, the square of the hypotenuse equals the sum of the squares of the other two sides. For example, if a ladder leans against a wall and forms a right triangle, you can use this formula to find the ladder's length. This helps in construction and home improvement tasks.",
            new List<string> { "Explain in 3 sentences", "Use a real-world example" },
            false);
}