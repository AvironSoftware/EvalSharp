using EvalSharp.Scoring;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace EvalSharp.Tests;

public class GEvalTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    public GEvalTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task Testing_Accuracy_Success()
    {
        var config = new GEvalMetricConfiguration
        {
            Criteria = "Does the summary misrepresent key facts, figures, or events mentioned in the article?",
        };

        var gEvalMetric = new GEvalMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "The United Nations has warned that the global economy is facing severe challenges, with many countries experiencing deep recessions due to the ongoing pandemic. In a new report, the UN highlights the increasing unemployment rates, widespread poverty, and disruptions to supply chains. While some countries are beginning to show signs of recovery, the overall situation remains uncertain. Governments are urged to prioritize fiscal support and sustainable development policies to avoid long-term economic stagnation.",
            ActualOutput = "The UN has warned about global economic challenges caused by the pandemic, highlighting issues such as unemployment, poverty, and supply chain disruptions. Some countries are recovering, but uncertainty remains, with an emphasis on fiscal support and sustainable development.",
            ExpectedOutput = "The United Nations has warned that the global economy is facing challenges due to the pandemic, with rising unemployment, poverty, and supply chain disruptions. While some nations are recovering, the global outlook remains uncertain, urging governments to prioritize fiscal support and sustainable development.",
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput
        };
        var score = await gEvalMetric.ScoreAsync(context);

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.Score}");
        _testOutputHelper.WriteLine($"Result: {score.Result}");

        //Assert
        Assert.True(score.Score >= config.Threshold);
        Assert.Equal(MetricScoreResult.Pass, score.Result);
    }

    [Fact]
    public async Task Testing_Accuracy_Fail()
    {
        var config = new GEvalMetricConfiguration
        {
            Criteria = "Does the summary misrepresent key facts, figures, or events mentioned in the article?",
        };

        var gEvalMetric = new GEvalMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "The United Nations has warned that the global economy is facing severe challenges, with many countries experiencing deep recessions due to the ongoing pandemic. In a new report, the UN highlights the increasing unemployment rates, widespread poverty, and disruptions to supply chains. While some countries are beginning to show signs of recovery, the overall situation remains uncertain. Governments are urged to prioritize fiscal support and sustainable development policies to avoid long-term economic stagnation.",
            ActualOutput = "The United Nations has declared that the global economy is in perfect shape, with all countries experiencing record-breaking growth due to the pandemic. Their latest report celebrates the lowest unemployment rates in history, universal wealth, and flawlessly functioning supply chains. Every country has fully recovered, and there are no economic uncertainties whatsoever. Governments are advised to relax and avoid any economic policies, as everything is running smoothly on its own.",
            ExpectedOutput = "The United Nations has warned that the global economy is facing challenges due to the pandemic, with rising unemployment, poverty, and supply chain disruptions. While some nations are recovering, the global outlook remains uncertain, urging governments to prioritize fiscal support and sustainable development.",
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput
        };
        var score = await gEvalMetric.ScoreAsync(context);

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.Score}");
        _testOutputHelper.WriteLine($"Result: {score.Result}");
        // Assert
        Assert.True(score.Score < config.Threshold);
        Assert.Equal(MetricScoreResult.Fail, score.Result);
    }

    [Fact]
    public async Task Testing_Coverage_Success()
    {
        var config = new GEvalMetricConfiguration
        {
            Criteria = "Are the most important aspects of the article captured in the summary?"
        };

        var gEvalMetric = new GEvalMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "The United Nations has warned that the global economy is facing severe challenges, with many countries experiencing deep recessions due to the ongoing pandemic. In a new report, the UN highlights the increasing unemployment rates, widespread poverty, and disruptions to supply chains. While some countries are beginning to show signs of recovery, the overall situation remains uncertain. Governments are urged to prioritize fiscal support and sustainable development policies to avoid long-term economic stagnation.",
            ActualOutput = "The UN has warned about global economic challenges caused by the pandemic, highlighting issues such as unemployment, poverty, and supply chain disruptions. Some countries are recovering, but uncertainty remains, with an emphasis on fiscal support and sustainable development.",
            ExpectedOutput = "The United Nations has warned that the global economy is facing challenges due to the pandemic, with rising unemployment, poverty, and supply chain disruptions. While some nations are recovering, the global outlook remains uncertain, urging governments to prioritize fiscal support and sustainable development.",
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput
        };
        var score = await gEvalMetric.ScoreAsync(context);

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.Score}");
        _testOutputHelper.WriteLine($"Result: {score.Result}");
        // Assert
        Assert.True(score.Score >= config.Threshold);
        Assert.Equal(MetricScoreResult.Pass, score.Result);
    }

    [Fact]
    public async Task Testing_Coverage_Fail()
    {
        var config = new GEvalMetricConfiguration
        {
            Criteria = "Are the most important aspects of the article captured in the summary?"
        };

        var gEvalMetric = new GEvalMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "The United Nations has warned that the global economy is facing severe challenges, with many countries experiencing deep recessions due to the ongoing pandemic. In a new report, the UN highlights the increasing unemployment rates, widespread poverty, and disruptions to supply chains. While some countries are beginning to show signs of recovery, the overall situation remains uncertain. Governments are urged to prioritize fiscal support and sustainable development policies to avoid long-term economic stagnation.",
            ActualOutput = "The United Nations mentioned something about the economy in a report. Some places are doing okay, while others are not. There were suggestions for governments to take action, but the details weren�t too clear.",
            ExpectedOutput = "The United Nations has warned that the global economy is facing challenges due to the pandemic, with rising unemployment, poverty, and supply chain disruptions. While some nations are recovering, the global outlook remains uncertain, urging governments to prioritize fiscal support and sustainable development.",
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput
        };
        var score = await gEvalMetric.ScoreAsync(context);


        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.Score}");
        _testOutputHelper.WriteLine($"Result: {score.Result}");
        // Assert
        Assert.True(score.Score < config.Threshold);
        Assert.Equal(MetricScoreResult.Fail, score.Result);
    }

    [Fact]
    public async Task Testing_Conciseness_Success()
    {
        var config = new GEvalMetricConfiguration
        {
            Criteria = "Is the summary shorter than the original article without omitting key details?",
            Threshold = 0.8
        };

        var gEvalMetric = new GEvalMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "The United Nations has warned that the global economy is facing severe challenges, with many countries experiencing deep recessions due to the ongoing pandemic. In a new report, the UN highlights the increasing unemployment rates, widespread poverty, and disruptions to supply chains. While some countries are beginning to show signs of recovery, the overall situation remains uncertain. Governments are urged to prioritize fiscal support and sustainable development policies to avoid long-term economic stagnation.",
            ActualOutput = "The UN has warned about global economic challenges caused by the pandemic, highlighting issues such as unemployment, poverty, and supply chain disruptions. Some countries are recovering, but uncertainty remains, with an emphasis on fiscal support and sustainable development.",
            ExpectedOutput = "The United Nations has warned that the global economy is facing challenges due to the pandemic, with rising unemployment, poverty, and supply chain disruptions. While some nations are recovering, the global outlook remains uncertain, urging governments to prioritize fiscal support and sustainable development.",
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput= test.ActualOutput,
            ExpectedOutput= test.ExpectedOutput
        };
        var score = await gEvalMetric.ScoreAsync(context);



        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.Score}");
        _testOutputHelper.WriteLine($"Result: {score.Result}");
        // Assert
        Assert.True(score.Score >= config.Threshold);
        Assert.Equal(MetricScoreResult.Pass, score.Result);
    }

    [Fact]
    public async Task Testing_Conciseness_Fail()
    {
        var config = new GEvalMetricConfiguration
        {
            Criteria = "Is the summary shorter than the original article without omitting key details?",
            Threshold = 0.8
        };

        var gEvalMetric = new GEvalMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "The United Nations has warned that the global economy is facing severe challenges, with many countries experiencing deep recessions due to the ongoing pandemic. In a new report, the UN highlights the increasing unemployment rates, widespread poverty, and disruptions to supply chains. While some countries are beginning to show signs of recovery, the overall situation remains uncertain. Governments are urged to prioritize fiscal support and sustainable development policies to avoid long-term economic stagnation.",
            ActualOutput = "The United Nations has issued a warning that the global economy is facing significant challenges, with numerous countries experiencing some very bad things. In a newly released report, the UN emphasizes the rising unemployment rates, increasing levels of poverty, and major disruptions to supply chains that have affected economies worldwide. While certain countries have started to show early signs of recovery, the overall economic outlook remains highly uncertain. The report urges governments to take proactive measures, prioritizing fiscal support and implementing sustainable development policies to prevent long-term economic stagnation and ensure a more stable future.",
            ExpectedOutput = "The United Nations has warned that the global economy is facing challenges due to the pandemic, with many things that make it difficult for these nations. While some nations are recovering, the global outlook remains uncertain, urging governments to prioritize fiscal support and sustainable development.",
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput
        };
        var score = await gEvalMetric.ScoreAsync(context);


        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.Score}");
        _testOutputHelper.WriteLine($"Result: {score.Result}");
        // Assert
        Assert.True(score.Score < config.Threshold);
        Assert.Equal(MetricScoreResult.Fail, score.Result);
    }

    [Fact]
    public async Task Testing_EvalSteps_Success()
    {
        var steps = new List<string>
        {
            "Compare the summary with the original article to ensure that no facts have been misrepresented or omitted.",
            "Identify the key elements in the original article (e.g., wildfire threat to homes, evacuations, fire spread, dry conditions, high winds) and check if they are included in the summary.",
            "Measure the length of the summary against the original article. Ensure that the summary captures the most essential information without being overly long or excessively short."
        };
        var config = new GEvalMetricConfiguration
        {
            EvaluationSteps = steps
        };

        var gEvalMetric = new GEvalMetric(ChatClient.GetInstance(), config);
        var test = new
        {
            InitialInput = "The United Nations has warned that the global economy is facing severe challenges, with many countries experiencing deep recessions due to the ongoing pandemic. In a new report, the UN highlights the increasing unemployment rates, widespread poverty, and disruptions to supply chains. While some countries are beginning to show signs of recovery, the overall situation remains uncertain. Governments are urged to prioritize fiscal support and sustainable development policies to avoid long-term economic stagnation.",
            ActualOutput = "The UN has warned about global economic challenges caused by the pandemic, highlighting issues such as unemployment, poverty, and supply chain disruptions. Some countries are recovering, but uncertainty remains, with an emphasis on fiscal support and sustainable development.",
            ExpectedOutput = "The United Nations has warned that the global economy is facing challenges due to the pandemic, with rising unemployment, poverty, and supply chain disruptions. While some nations are recovering, the global outlook remains uncertain, urging governments to prioritize fiscal support and sustainable development.",
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput= test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput
        };
        var score = await gEvalMetric.ScoreAsync(context);


        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.Score}");
        _testOutputHelper.WriteLine($"Result: {score.Result}");
        // Assert
        Assert.True(score.Score >= config.Threshold);
        Assert.Equal(MetricScoreResult.Pass, score.Result);
    }

    [Fact]
    public async Task Testing_Relevance_Success()
    {
        var config = new GEvalMetricConfiguration
        {
            Criteria = "Does the generated text stay on topic and directly answer the question or follow the instructions given in the prompt?",
        };

        var gEvalMetric = new GEvalMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "How do plants make their food?",
            ActualOutput = "Plants make their food through a process called photosynthesis. Using sunlight, water, and carbon dioxide, plants create glucose, which is a type of sugar that provides energy. The process occurs in the chloroplasts of plant cells, which contain a green pigment called chlorophyll. Chlorophyll absorbs sunlight, allowing the plant to convert energy and produce food.",
            ExpectedOutput = "Plants produce their food through photosynthesis, a process that occurs in the chloroplasts of their cells. Using sunlight, water, and carbon dioxide, plants convert these elements into glucose, a sugar that provides energy. Chlorophyll, the green pigment in plants, absorbs sunlight, enabling the plant to carry out this process.",
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput
        };
        var score = await gEvalMetric.ScoreAsync(context);

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.Score}");
        _testOutputHelper.WriteLine($"Result: {score.Result}");
        // Assert
        Assert.True(score.Score >= config.Threshold);
        Assert.Equal(MetricScoreResult.Pass, score.Result);
    }

    [Fact]
    public async Task Testing_Relevance_Fail()
    {
        var config = new GEvalMetricConfiguration
        {
            Criteria = "Does the generated text stay on topic and directly answer the question or follow the instructions given in the prompt?"
        };

        var gEvalMetric = new GEvalMetric(ChatClient.GetInstance(), config);

        var test = new
        {
            InitialInput = "How do plants make their food?",
            ActualOutput = "You know, it's fascinating how nature works in so many intricate ways. Have you ever thought about how different living organisms have evolved unique strategies to sustain themselves? Take deep-sea creatures, for instance�they thrive in complete darkness, relying on chemosynthesis instead of sunlight. It's incredible how life finds a way in even the most extreme environments. Speaking of fascinating processes, have you ever seen a time-lapse of a plant growing? It's almost like watching a slow-motion dance of life, reaching toward the light.",
            ExpectedOutput = "Plants produce their food through photosynthesis, a process that occurs in the chloroplasts of their cells. Using sunlight, water, and carbon dioxide, plants convert these elements into glucose, a sugar that provides energy. Chlorophyll, the green pigment in plants, absorbs sunlight, enabling the plant to carry out this process.",
        };

        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ExpectedOutput = test.ExpectedOutput
        };
        var score = await gEvalMetric.ScoreAsync(context);


        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.Score}");
        _testOutputHelper.WriteLine($"Result: {score.Result}");
        // Assert
        Assert.True(score.Score < config.Threshold);
        Assert.Equal(MetricScoreResult.Fail, score.Result);
    }
}