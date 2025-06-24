using EvalSharp.Scoring;
using Xunit.Abstractions;

namespace EvalSharp.Tests;

public class ContextualRecallTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    public ContextualRecallTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task Testing_Chatbot_Success()
    {
        var config = new ContextualRecallMetricConfiguration {  };

        var contextualPrecMetric = new ContextualRecallMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "I need help with my account; I mentioned earlier that my account number is 12345, but I cannot log in.",
            ActualOutput = "Please reset your password using the forgot password link.",
            ExpectedOutput = "Since you mentioned account number 12345 earlier and stated you're having trouble logging in, please try resetting your password using the 'Forgot Password' link. If the issue persists, let me escalate your ticket for further assistance.",
            RetrievalContext = "Previous conversation where the user mentioned account number 12345 and issues with logging in."
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput,
            RetrievalContext = [test.RetrievalContext]
        };
        var score = await contextualPrecMetric.ScoreAsync(context);

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.Score}");
        _testOutputHelper.WriteLine($"Result: {score.Result}");

        //Assert
        Assert.True(score.Score >= config.Threshold);
        Assert.Equal(MetricScoreResult.Pass, score.Result);
    }

    [Fact]
    public async Task Testing_Healthcare_Success()
    {
        var config = new ContextualRecallMetricConfiguration();
        var contextualPrecMetric = new ContextualRecallMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "I've been having a fever and a persistent cough for three days. What should I do?",
            ActualOutput = "Take some over-the-counter medicine and rest.",
            ExpectedOutput = "Given that you've had a fever and persistent cough for three days, it's advisable to monitor your symptoms closely. If they worsen or persist beyond a few days, please consult a healthcare professional. In the meantime, rest, stay hydrated, and consider taking appropriate medication.",
            RetrievalContext = "Earlier messages mentioned that the patient has been experiencing a fever and cough for three days."
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput,
            RetrievalContext = [test.RetrievalContext]
        };
        var score = await contextualPrecMetric.ScoreAsync(context);

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.Score}");
        _testOutputHelper.WriteLine($"Result: {score.Result}");

        //Assert
        Assert.True(score.Score >= config.Threshold);
        Assert.Equal(MetricScoreResult.Pass, score.Result);
    }

    [Fact]
    public async Task Testing_Legal_Success()
    {
        var config = new ContextualRecallMetricConfiguration();
        var contextualPrecMetric = new ContextualRecallMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "I'm planning to start a business in California. Do I need any special licenses?",
            ActualOutput = "Yes, you may need licenses.",
            ExpectedOutput = "Since you're planning to start a business in California, you'll need to consult state and local regulations. Requirements vary by industry, so it's best to check with the California Business Portal or a legal advisor for your specific needs.",
            RetrievalContext = "User previously stated they are setting up a business in California, along with industry context."
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput,
            RetrievalContext = [test.RetrievalContext]
        };
        var score = await contextualPrecMetric.ScoreAsync(context);

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.Score}");
        _testOutputHelper.WriteLine($"Result: {score.Result}");

        //Assert
        Assert.True(score.Score >= config.Threshold);
        Assert.Equal(MetricScoreResult.Pass, score.Result);
    }

    [Fact]
    public async Task Testing_Summarization_Success()
    {
        var config = new ContextualRecallMetricConfiguration();
        var contextualPrecMetric = new ContextualRecallMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "Summarize the key findings of this report: The report details increased sales, but also highlights concerns over supply chain disruptions.",
            ActualOutput = "The report shows increased sales.",
            ExpectedOutput = "The report indicates that while sales have increased, there are significant concerns about supply chain disruptions that could affect future performance.",
            RetrievalContext = "Report context includes discussions on increased sales as well as detailed analysis of supply chain issues in the latter sections."
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput,
            RetrievalContext = [test.RetrievalContext]
        };
        var score = await contextualPrecMetric.ScoreAsync(context);

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.Score}");
        _testOutputHelper.WriteLine($"Result: {score.Result}");

        //Assert
        Assert.True(score.Score >= config.Threshold);
        Assert.Equal(MetricScoreResult.Pass, score.Result);
    }

    [Fact]
    public async Task Testing_Translation_Success()
    {
        var config = new ContextualRecallMetricConfiguration();
        var contextualPrecMetric = new ContextualRecallMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "Translate 'Break a leg' into French, keeping in mind that it's an idiom used to wish someone good luck in a performance.",
            ActualOutput = "Casse une jambe.",
            ExpectedOutput = "In a theatrical context, 'Break a leg' is best translated to 'Merde' in French, which is commonly used to wish someone good luck on stage.",
            RetrievalContext = "The context notes that 'Break a leg' is an idiom for wishing good luck in a performance, not a literal instruction."
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput,
            RetrievalContext = [test.RetrievalContext]
        };
        var score = await contextualPrecMetric.ScoreAsync(context);

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.Score}");
        _testOutputHelper.WriteLine($"Result: {score.Result}");

        //Assert
        Assert.True(score.Score >= config.Threshold);
        Assert.Equal(MetricScoreResult.Pass, score.Result);
    }

    [Fact]
    public async Task Testing_Tutoring_Success()
    {
        var config = new ContextualRecallMetricConfiguration();
        var contextualPrecMetric = new ContextualRecallMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "Can you explain why plants need sunlight, referring to our discussion on photosynthesis last week?",
            ActualOutput = "Plants need sunlight for growth.",
            ExpectedOutput = "As discussed in our previous lesson on photosynthesis, plants require sunlight to convert carbon dioxide and water into glucose and oxygen, which is vital for their growth and energy production.",
            RetrievalContext = "Previous lesson included a detailed explanation of photosynthesis and the role of sunlight in the process."
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput,
            RetrievalContext = [test.RetrievalContext]
        };
        var score = await contextualPrecMetric.ScoreAsync(context);

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.Score}");
        _testOutputHelper.WriteLine($"Result: {score.Result}");

        //Assert
        Assert.True(score.Score >= config.Threshold);
        Assert.Equal(MetricScoreResult.Pass, score.Result);
    }

    [Fact]
    public async Task Testing_Chatbot_Fail()
    {
        var config = new ContextualRecallMetricConfiguration();
        var contextualPrecMetric = new ContextualRecallMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "I need help with my account; I mentioned earlier that my account number is 12345, but I cannot log in.",
            ActualOutput = "Your account number appears to be 00000. Please try entering that instead.",
            ExpectedOutput = "Since you mentioned account number 12345 earlier and stated you're having trouble logging in, please try resetting your password using the 'Forgot Password' link. If the issue persists, let me escalate your ticket for further assistance.",
            RetrievalContext = new List<string>
            {
                "Previous conversation where the user mentioned account number 12345 and issues with logging in.",
                "Irrelevant context: The system recently updated its login algorithm and now uses account number 00000 for testing."
            }
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput,
            RetrievalContext = test.RetrievalContext
        };
        var score = await contextualPrecMetric.ScoreAsync(context);

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.Score}");
        _testOutputHelper.WriteLine($"Result: {score.Result}");

        //Assert
        Assert.True(score.Score < config.Threshold);
        Assert.Equal(MetricScoreResult.Fail, score.Result);
    }

    [Fact]
    public async Task Testing_Healthcare_Fail()
    {
        var config = new ContextualRecallMetricConfiguration();
        var contextualPrecMetric = new ContextualRecallMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "I've been having a fever and a persistent cough for three days. What should I do?",
            ActualOutput = "Take some painkillers and go jogging to clear your system.",
            ExpectedOutput = "Given that you've had a fever and persistent cough for three days, it's advisable to monitor your symptoms closely. If they worsen or persist beyond a few days, please consult a healthcare professional. In the meantime, rest, stay hydrated, and consider taking appropriate medication.",
            RetrievalContext = new List<string>
            {
                "Earlier messages mentioned that the patient has been experiencing a fever and cough for three days.",
                "Irrelevant context: Recent studies suggest jogging is beneficial for clearing minor illnesses."
            }
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput,
            RetrievalContext = test.RetrievalContext
        };
        var score = await contextualPrecMetric.ScoreAsync(context);

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.Score}");
        _testOutputHelper.WriteLine($"Result: {score.Result}");

        //Assert
        Assert.True(score.Score < config.Threshold);
        Assert.Equal(MetricScoreResult.Fail, score.Result);
    }

    [Fact]
    public async Task Testing_Legal_Fail()
    {
        var config = new ContextualRecallMetricConfiguration();
        var contextualPrecMetric = new ContextualRecallMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "I'm planning to start a business in California. Do I need any special licenses?",
            ActualOutput = "Yes, you should apply for a work visa immediately.",
            ExpectedOutput = "Since you're planning to start a business in California, you'll need to consult state and local regulations. Requirements vary by industry, so it's best to check with the California Business Portal or a legal advisor for your specific needs.",
            RetrievalContext = new List<string>
            {
                "User previously stated they are setting up a business in California, along with industry context.",
                "Irrelevant context: For foreign entrepreneurs, obtaining a work visa is mandatory regardless of local licensing requirements."
            }
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput,
            RetrievalContext = test.RetrievalContext
        };
        var score = await contextualPrecMetric.ScoreAsync(context);

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.Score}");
        _testOutputHelper.WriteLine($"Result: {score.Result}");

        //Assert
        Assert.True(score.Score < config.Threshold);
        Assert.Equal(MetricScoreResult.Fail, score.Result);
    }

    [Fact]
    public async Task Testing_Summarization_Fail()
    {
        var config = new ContextualRecallMetricConfiguration();
        var contextualPrecMetric = new ContextualRecallMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "Summarize the key findings of this report: The report details increased sales, but also highlights concerns over supply chain disruptions.",
            ActualOutput = "The report focuses on marketing strategies and upcoming product launches.",
            ExpectedOutput = "The report indicates that while sales have increased, there are significant concerns about supply chain disruptions that could affect future performance.",
            RetrievalContext = new List<string>
            {
                "Report context includes discussions on increased sales as well as detailed analysis of supply chain issues in the latter sections.",
                "Irrelevant context: Other sections of the report emphasize marketing strategies and product launches."
            }
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput,
            RetrievalContext = test.RetrievalContext
        };
        var score = await contextualPrecMetric.ScoreAsync(context);

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.Score}");
        _testOutputHelper.WriteLine($"Result: {score.Result}");

        //Assert
        Assert.True(score.Score < config.Threshold);
        Assert.Equal(MetricScoreResult.Fail, score.Result);
    }

    [Fact]
    public async Task Testing_Translation_Fail()
    {
        var config = new ContextualRecallMetricConfiguration();
        var contextualPrecMetric = new ContextualRecallMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "Translate 'Break a leg' into French, keeping in mind that it's an idiom used to wish someone good luck in a performance.",
            ActualOutput = "Casser un crayon.",
            ExpectedOutput = "In a theatrical context, 'Break a leg' is best translated to 'Merde' in French, which is commonly used to wish someone good luck on stage.",
            RetrievalContext = new List<string>
            {
                "The context notes that 'Break a leg' is an idiom for wishing good luck in a performance, not a literal instruction.",
                "Irrelevant context: In some literal interpretations, breaking a leg might be humorously referenced as 'Casser un crayon.'"
            }
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput,
            RetrievalContext = test.RetrievalContext
        };
        var score = await contextualPrecMetric.ScoreAsync(context);

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.Score}");
        _testOutputHelper.WriteLine($"Result: {score.Result}");

        //Assert
        Assert.True(score.Score < config.Threshold);
        Assert.Equal(MetricScoreResult.Fail, score.Result);
    }

    [Fact]
    public async Task Testing_Tutoring_Fail()
    {
        var config = new ContextualRecallMetricConfiguration();
        var contextualPrecMetric = new ContextualRecallMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "Can you explain why plants need sunlight, referring to our discussion on photosynthesis last week?",
            ActualOutput = "Plants need sunlight because it helps them grow more flowers.",
            ExpectedOutput = "As discussed in our previous lesson on photosynthesis, plants require sunlight to convert carbon dioxide and water into glucose and oxygen, which is vital for their growth and energy production.",
            RetrievalContext = new List<string>
            {
                "Previous lesson included a detailed explanation of photosynthesis and the role of sunlight in the process.",
                "Irrelevant context: Some sources incorrectly claim that sunlight only influences the blooming of flowers."
            }
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput,
            RetrievalContext = test.RetrievalContext
        };
        var score = await contextualPrecMetric.ScoreAsync(context);

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.Score}");
        _testOutputHelper.WriteLine($"Result: {score.Result}");

        //Assert
        Assert.True(score.Score < config.Threshold);
        Assert.Equal(MetricScoreResult.Fail, score.Result);
    }
}