using EvalSharp.Scoring;
using Xunit.Abstractions;

namespace EvalSharp.Tests;

public class FaithfulnessTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly FaithfulnessMetricConfiguration _config;
    private readonly FaithfulnessMetric _faithfulnessMetric;

    public FaithfulnessTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        // Setting threshold higher as expected failures tend to result in a score of 0.5
        _config = new FaithfulnessMetricConfiguration { IncludeReason = true, Threshold = 0.6 };
        _faithfulnessMetric = new FaithfulnessMetric(ChatClient.GetInstance(), _config);
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
        var score = await _faithfulnessMetric.ScoreAsync(context);

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
    public async Task Faithfulness_Test_Chatbot_Success() =>
        await RunTestAsync("I need help with my account; I mentioned earlier that my account number is 12345, but I cannot log in.",
            "Your account number is 12345. Try resetting your password using the 'Forgot Password' link.",
            "Since you mentioned account number 12345 earlier and stated you're having trouble logging in, please try resetting your password using the 'Forgot Password' link. If the issue persists, let me escalate your ticket for further assistance.",
            ["Previous conversation where the user mentioned account number 12345 and issues with logging in."], true);

    [Fact]
    public async Task Faithfulness_Test_Chatbot_Fail() =>
        await RunTestAsync("I need help with my account; I mentioned earlier that my account number is 12345, but I cannot log in.",
            "Your account number appears to be 99999. Please use that instead.",
            "Since you mentioned account number 12345 earlier and stated you're having trouble logging in, please try resetting your password using the 'Forgot Password' link. If the issue persists, let me escalate your ticket for further assistance.",
            ["Previous conversation where the user mentioned account number 12345 and issues with logging in."], false);

    [Fact]
    public async Task Faithfulness_Test_Healthcare_Success() =>
        await RunTestAsync("I've been having a fever and a persistent cough for three days. What should I do?",
            "Drink plenty of water and rest. If symptoms worsen, consult a doctor.",
            "Given that you've had a fever and persistent cough for three days, it's advisable to monitor your symptoms closely. If they worsen or persist beyond a few days, please consult a healthcare professional. In the meantime, rest, stay hydrated, and consider taking appropriate medication.",
            ["Earlier messages mentioned that the patient has been experiencing a fever and cough for three days."], true);

    [Fact]
    public async Task Faithfulness_Test_Healthcare_Fail() =>
        await RunTestAsync("I've been having a fever and a persistent cough for three days. What should I do?",
            "You should go for a 10-mile run to clear your system.",
            "Given that you've had a fever and persistent cough for three days, it's advisable to monitor your symptoms closely. If they worsen or persist beyond a few days, please consult a healthcare professional. In the meantime, rest, stay hydrated, and consider taking appropriate medication.",
            ["Earlier messages mentioned that the patient has been experiencing a fever and cough for three days."], false);

    [Fact]
    public async Task Faithfulness_Test_Legal_Success() =>
        await RunTestAsync("I'm planning to start a business in California. Do I need any special licenses?",
            "You should check with the California Business Portal, as licensing requirements depend on your industry.",
            "Since you're planning to start a business in California, you'll need to consult state and local regulations. Requirements vary by industry, so it's best to check with the California Business Portal or a legal advisor for your specific needs.",
            ["User previously stated they are setting up a business in California, along with industry context."], true);

    [Fact]
    public async Task Faithfulness_Test_Legal_Fail() =>
        await RunTestAsync("I'm planning to start a business in California. Do I need any special licenses?",
            "All businesses in California must obtain a federal business license.",
            "Since you're planning to start a business in California, you'll need to consult state and local regulations. Requirements vary by industry, so it's best to check with the California Business Portal or a legal advisor for your specific needs.",
            ["User previously stated they are setting up a business in California, along with industry context."], false);

    [Fact]
    public async Task Faithfulness_Test_Summarization_Success() =>
        await RunTestAsync("Summarize the key findings of this report: The report details increased sales, but also highlights concerns over supply chain disruptions.",
            "The report states that sales have increased but warns about ongoing supply chain issues.",
            "The report indicates that while sales have increased, there are significant concerns about supply chain disruptions that could affect future performance.",
            ["Report context includes discussions on increased sales as well as detailed analysis of supply chain issues in the latter sections."], true);

    [Fact]
    public async Task Faithfulness_Test_Summarization_Fail() =>
        await RunTestAsync("Summarize the key findings of this report: The report details increased sales, but also highlights concerns over supply chain disruptions.",
            "The report shows increased sales but does not mention any challenges.",
            "The report indicates that while sales have increased, there are significant concerns about supply chain disruptions that could affect future performance.",
            ["Report context includes discussions on increased sales as well as detailed analysis of supply chain issues in the latter sections."], false);

    [Fact]
    public async Task Faithfulness_Test_Translation_Success() =>
        await RunTestAsync("Translate 'Break a leg' into French, keeping in mind that it's an idiom used to wish someone good luck in a performance.",
            "Dans un contexte théâtral, on dit 'Merde' en français pour souhaiter bonne chance.",
            "In a theatrical context, 'Break a leg' is best translated to 'Merde' in French, which is commonly used to wish someone good luck on stage.",
            ["The context notes that 'Break a leg' is an idiom for wishing good luck in a performance, not a literal instruction."], true);

    [Fact]
    public async Task Faithfulness_Test_Translation_Fail() =>
        await RunTestAsync("Translate 'Break a leg' into French, keeping in mind that it's an idiom used to wish someone good luck in a performance.",
            "Casser un bras.",
            "In a theatrical context, 'Break a leg' is best translated to 'Merde' in French, which is commonly used to wish someone good luck on stage.",
            ["The context notes that 'Break a leg' is an idiom for wishing good luck in a performance, not a literal instruction."], false);

    [Fact]
    public async Task Faithfulness_Test_Tutoring_Success() =>
        await RunTestAsync("Can you explain why plants need sunlight, referring to our discussion on photosynthesis last week?",
            "Plants need sunlight for photosynthesis, which helps them convert carbon dioxide and water into glucose and oxygen.",
            "As discussed in our previous lesson on photosynthesis, plants require sunlight to convert carbon dioxide and water into glucose and oxygen, which is vital for their growth and energy production.",
            ["Previous lesson included a detailed explanation of photosynthesis and the role of sunlight in the process."], true);

    [Fact]
    public async Task Faithfulness_Test_Tutoring_Fail() =>
        await RunTestAsync("Can you explain why plants need sunlight, referring to our discussion on photosynthesis last week?",
            "Plants need sunlight to stay warm and grow taller.",
            "As discussed in our previous lesson on photosynthesis, plants require sunlight to convert carbon dioxide and water into glucose and oxygen, which is vital for their growth and energy production.",
            ["Previous lesson included a detailed explanation of photosynthesis and the role of sunlight in the process."], false);
}