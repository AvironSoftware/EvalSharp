using EvalSharp.Scoring;
using Xunit.Abstractions;

namespace EvalSharp.Tests;

public class AnswerRelevancyTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    public AnswerRelevancyTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }


    [Fact]
    public async Task Testing_Chatbot_Success()
    {
        var config = new AnswerRelevancyMetricConfiguration
        {
            IncludeReason = true,
        };
        var answerRelMetric = new AnswerRelevancyMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "My internet connection is down. How can I reset my router?",
            ActualOutput = "Please try unplugging your router for 30 seconds and then plug it back in.",
            ExpectedOutput = "Unplug your router for 30 seconds, plug it back in, and check if the connection is restored. If not, contact technical support."
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput
        };
        var score = await answerRelMetric.ScoreAsync(context);

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
        var config = new AnswerRelevancyMetricConfiguration
        {
            IncludeReason = true
        };
        var answerRelMetric = new AnswerRelevancyMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "I have a slight fever and cough. What should I do right now?",
            ActualOutput = "Drink water and get some rest.",
            ExpectedOutput = "Monitor your symptoms closely, stay hydrated, and rest. If your fever or cough worsens, seek medical advice promptly."
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput
        };
        var score = await answerRelMetric.ScoreAsync(context);

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.Score}");
        _testOutputHelper.WriteLine($"Result: {score.Result}");

        //Assert
        Assert.True(score.Score >= config.Threshold);
        Assert.Equal(MetricScoreResult.Pass, score.Result);
    }

    [Fact]
    public async Task Testing_Education_Success()
    {
        var config = new AnswerRelevancyMetricConfiguration
        {
            IncludeReason = true
        };
        var answerRelMetric = new AnswerRelevancyMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "Can you explain the Pythagorean theorem to me?",
            ActualOutput = "In a right triangle, a² + b² = c².",
            ExpectedOutput = "The Pythagorean theorem states that in a right-angled triangle, the sum of the squares of the two shorter sides (a and b) equals the square of the hypotenuse (c): a² + b² = c²."
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput
        };
        var score = await answerRelMetric.ScoreAsync(context);

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
        var config = new AnswerRelevancyMetricConfiguration
        {
            IncludeReason = true
        };
        var answerRelMetric = new AnswerRelevancyMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "What are the steps to file a small claims court case?",
            ActualOutput = "Fill out the required forms and pay the filing fee.",
            ExpectedOutput = "Generally, filing a small claims case involves completing the necessary forms, paying a filing fee, and serving the defendant with notice. Check your local court’s guidelines for specifics."
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput
        };
        var score = await answerRelMetric.ScoreAsync(context);

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.Score}");
        _testOutputHelper.WriteLine($"Result: {score.Result}");

        //Assert
        Assert.True(score.Score >= config.Threshold);
        Assert.Equal(MetricScoreResult.Pass, score.Result);
    }

    [Fact]
    public async Task Testing_SearchEngine_Success()
    {
        var config = new AnswerRelevancyMetricConfiguration
        {
            IncludeReason = true
        };
        var answerRelMetric = new AnswerRelevancyMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "Which is the tallest mountain in the world?",
            ActualOutput = "Mount Everest is the tallest mountain in the world.",
            ExpectedOutput = "Mount Everest, standing at about 8,848 meters (29,029 feet), is recognized as the tallest mountain on Earth."
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput
        };
        var score = await answerRelMetric.ScoreAsync(context);

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
        var config = new AnswerRelevancyMetricConfiguration
        {
            IncludeReason = true
        };
        var answerRelMetric = new AnswerRelevancyMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "Please summarize the article on climate change impacts.",
            ActualOutput = "The article discusses climate change and its effects.",
            ExpectedOutput = "The article outlines how climate change is accelerating global temperature rise, leading to severe weather events and long-term environmental impacts, and stresses the urgency for collective action."
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput
        };
        var score = await answerRelMetric.ScoreAsync(context);

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
        var config = new AnswerRelevancyMetricConfiguration
        {
            IncludeReason = true
        };
        var answerRelMetric = new AnswerRelevancyMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "My internet connection is down. How can I reset my router?",
            ActualOutput = "Try turning off your television to see if that helps.",
            ExpectedOutput = "Unplug your router for 30 seconds, plug it back in, and check if the connection is restored. If not, contact technical support."
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput
        };
        var score = await answerRelMetric.ScoreAsync(context);

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
        var config = new AnswerRelevancyMetricConfiguration
        {
            IncludeReason = true
        };
        var answerRelMetric = new AnswerRelevancyMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "I have a slight fever and cough. What should I do right now?",
            ActualOutput = "You should run a marathon immediately.",
            ExpectedOutput = "Monitor your symptoms closely, stay hydrated, and rest. If your fever or cough worsens, seek medical advice promptly."
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput
        };
        var score = await answerRelMetric.ScoreAsync(context);

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.Score}");
        _testOutputHelper.WriteLine($"Result: {score.Result}");

        //Assert
        Assert.True(score.Score < config.Threshold);
        Assert.Equal(MetricScoreResult.Fail, score.Result);
    }

    [Fact]
    public async Task Testing_Education_Fail()
    {
        var config = new AnswerRelevancyMetricConfiguration
        {
            IncludeReason = true
        };
        var answerRelMetric = new AnswerRelevancyMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "Can you explain the Pythagorean theorem to me?",
            ActualOutput = "It's a theorem that describes how rain falls.",
            ExpectedOutput = "The Pythagorean theorem states that in a right-angled triangle, the sum of the squares of the two shorter sides (a and b) equals the square of the hypotenuse (c): a² + b² = c²."
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput
        };
        var score = await answerRelMetric.ScoreAsync(context);

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
        var config = new AnswerRelevancyMetricConfiguration
        {
            IncludeReason = true
        };
        var answerRelMetric = new AnswerRelevancyMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "What are the steps to file a small claims court case?",
            ActualOutput = "You should immediately hire a large law firm and file a civil lawsuit.",
            ExpectedOutput = "Generally, filing a small claims case involves completing the necessary forms, paying a filing fee, and serving the defendant with notice. Check your local court’s guidelines for specifics."
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput
        };
        var score = await answerRelMetric.ScoreAsync(context);

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.Score}");
        _testOutputHelper.WriteLine($"Result: {score.Result}");

        //Assert
        Assert.True(score.Score < config.Threshold);
        Assert.Equal(MetricScoreResult.Fail, score.Result);
    }

    [Fact]
    public async Task Testing_SearchEngine_Fail()
    {
        var config = new AnswerRelevancyMetricConfiguration
        {
            IncludeReason = true
        };
        var answerRelMetric = new AnswerRelevancyMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "Which is the tallest mountain in the world?",
            ActualOutput = "Mount Kilimanjaro is the tallest mountain in the world.",
            ExpectedOutput = "Mount Everest, standing at about 8,848 meters (29,029 feet), is recognized as the tallest mountain on Earth."
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput
        };
        var score = await answerRelMetric.ScoreAsync(context);

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
        var config = new AnswerRelevancyMetricConfiguration
        {
            IncludeReason = true
        };
        var answerRelMetric = new AnswerRelevancyMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "Please summarize the article on climate change impacts.",
            ActualOutput = "The article talks about how technology is advancing rapidly.",
            ExpectedOutput = "The article outlines how climate change is accelerating global temperature rise, leading to severe weather events and long-term environmental impacts, and stresses the urgency for collective action."
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput
        };
        var score = await answerRelMetric.ScoreAsync(context);

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.Score}");
        _testOutputHelper.WriteLine($"Result: {score.Result}");

        //Assert
        Assert.True(score.Score < config.Threshold);
        Assert.Equal(MetricScoreResult.Fail, score.Result);
    }
}