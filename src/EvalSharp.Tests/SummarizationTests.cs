using EvalSharp.Scoring;
using Xunit.Abstractions;

namespace EvalSharp.Tests;

public class SummarizationTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public SummarizationTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    private async Task RunTestAsync(string input, string actualOutput, bool shouldPass, List<string>? assessmentQuestions = null)
    {
        var config = new SummarizationMetricConfiguration
        {
            IncludeReason = true,
            Threshold = 0.5,
            AssessmentQuestions = assessmentQuestions
        };
        var summarizationMetric = new SummarizationMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = input,
            ActualOutput = actualOutput,
        };
        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput
        };

        var score = await summarizationMetric.ScoreAsync(context);
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
    public async Task Summarization_Test_News_Summary_Success() =>
        await RunTestAsync(
            "The president announced a new climate policy today aimed at reducing carbon emissions by 30% over the next decade. Experts say this could significantly affect global warming trends.",
            "The president introduced a climate policy to cut carbon emissions by 30% in 10 years, which experts believe will impact global warming.",
            true
        );

    [Fact]
    public async Task Summarization_Test_News_Summary_Fail() =>
        await RunTestAsync(
            "The president announced a new climate policy today aimed at reducing carbon emissions by 30% over the next decade. Experts say this could significantly affect global warming trends.",
            "The president declared war today as a response to increasing pollution levels.",
            false
        );

    [Fact]
    public async Task Summarization_Test_Legal_Summary_Success() =>
        await RunTestAsync(
            "The contract stipulates that any disputes will be settled via arbitration in the state of New York. It also details payment schedules and confidentiality agreements.",
            "The contract mandates arbitration in New York for disputes and includes payment terms and confidentiality clauses.",
            true
        );

    [Fact]
    public async Task Summarization_Test_Legal_Summary_Fail() =>
        await RunTestAsync(
            "The contract stipulates that any disputes will be settled via arbitration in the state of New York. It also details payment schedules and confidentiality agreements.",
            "The contract allows public lawsuits for disputes and has no mention of payment terms.",
            false
        );

    [Fact]
    public async Task Summarization_Test_Research_Summary_Success() =>
        await RunTestAsync(
            "A study on sleep patterns found that adults who sleep 7-8 hours a night perform better on cognitive tests and report better mental health. The study analyzed 10,000 adults across the US.",
            "A US study of 10,000 adults found that sleeping 7-8 hours improves mental health and cognitive performance.",
            true
        );

    [Fact]
    public async Task Summarization_Test_Research_Summary_Fail() =>
        await RunTestAsync(
            "A study on sleep patterns found that adults who sleep 7-8 hours a night perform better on cognitive tests and report better mental health. The study analyzed 10,000 adults across the US.",
            "The study recommends sleeping only 4 hours to improve performance and mental stability.",
            false
        );

    [Fact]
    public async Task Summarization_Test_Meeting_Summary_Success() =>
        await RunTestAsync(
            "In the meeting, the team decided to prioritize the mobile app redesign in Q2, defer backend optimization to Q3, and assigned task owners for each feature.",
            "The team chose to focus on the mobile app redesign in Q2, push backend work to Q3, and assigned owners to tasks.",
            true
        );

    [Fact]
    public async Task Summarization_Test_Meeting_Summary_Fail() =>
        await RunTestAsync(
            "In the meeting, the team decided to prioritize the mobile app redesign in Q2, defer backend optimization to Q3, and assigned task owners for each feature.",
            "The team canceled all Q2 plans and decided not to assign any tasks.",
            false
        );


    [Fact]
    public async Task Summarization_Test_Research_Summary_With_Questions_Success() =>
        await RunTestAsync(
            "A study on sleep patterns found that adults who sleep 7-8 hours a night perform better on cognitive tests and report better mental health. The study analyzed 10,000 adults across the US.",
            "A US study of 10,000 adults found that sleeping 7-8 hours improves mental health and cognitive performance.",
            true,
            [
                "Does the summary mention that the study included 10,000 adults?",
                "Does the summary correctly identify 7-8 hours of sleep as the optimal range?",
                "Does the summary mention improved cognitive performance as a result?",
                "Does the summary include improved mental health as a finding?",
                "Does the summary indicate that the study was conducted in the US?"
            ]
        );

    [Fact]
    public async Task Summarization_Test_Research_Summary_With_Questions_Fail() =>
        await RunTestAsync(
            "A study on sleep patterns found that adults who sleep 7-8 hours a night perform better on cognitive tests and report better mental health. The study analyzed 10,000 adults across the US.",
            "The study recommends sleeping only 4 hours to improve performance and mental stability.",
            false,
            [
                "Does the summary mention that the study included 10,000 adults?",
                "Does the summary correctly identify 7-8 hours of sleep as the optimal range?",
                "Does the summary mention improved cognitive performance as a result?",
                "Does the summary include improved mental health as a finding?",
                "Does the summary indicate that the study was conducted in the US?"
            ]
        );

    [Fact]
    public async Task Summarization_Test_Eval_Success() =>
        await RunTestAsync(
            """
            The 'coverage score' is calculated as the percentage of assessment questions
            for which both the summary and the original document provide a 'yes' answer. This
            method ensures that the summary not only includes key information from the original
            text but also accurately represents it. A higher coverage score indicates a
            more comprehensive and faithful summary, signifying that the summary effectively
            encapsulates the crucial points and details from the original content.
            """,
            """
            The coverage score quantifies how well a summary captures and
            accurately represents key information from the original text,
            with a higher score indicating greater comprehensiveness.
            """,
            true,
            [
                "Is the coverage score based on a percentage of 'yes' answers?",
                "Does the score ensure the summary's accuracy with the source?",
                "Does a higher score mean a more comprehensive summary?"
            ]
        );
}