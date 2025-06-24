using EvalSharp.Models;
using EvalSharp.Models.Enums;
using EvalSharp.Scoring;
using Xunit.Abstractions;

namespace EvalSharp.Tests;

public class EndToEndTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public EndToEndTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }
    

    [Fact]
    public async Task TestingEvaluator()
    {
        string relativePath = Path.Combine("TestData", "answer_relevancy_test_data.json");
        string fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);

        var data = EvalData.FromJsonFile<TestCases>(fullPath).Take(10);

        var evaluator = Evaluator.FromData(ChatClient.GetInstance(), data, x => new EvaluatorTestData
        {
            InitialInput = x.ActualInput,
            ActualOutput = x.ActualOutput,
        });

        evaluator.AddAnswerRelevancy();
        var result = await evaluator.RunAsync();

        Assert.True(result.AverageScore > 0.85);
    }

    [Fact]
    public async Task TestingEvaluatorFileLoad()
    {
        string relativePath = Path.Combine("TestData", "answer_relevancy_test_data.json");
        string fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);

        var evaluator = Evaluator.FromJsonFile<TestCases>(ChatClient.GetInstance(), fullPath, x => new EvaluatorTestData
        {
            InitialInput = x.ActualInput,
            ActualOutput = x.ActualOutput
        });

        evaluator.AddAnswerRelevancy();
        var result = await evaluator.RunAsync();

        Assert.True(result.AverageScore > 0.85);
    }

    [Fact]
    public async Task TestingEvaluator_AnswerRel()
    {

        var test = new EvaluatorTestData
        {
            InitialInput = "Please summarize the article on climate change impacts.",
            ActualOutput = "The article talks about how technology is advancing rapidly.",
            ExpectedOutput = "The article outlines how climate change is accelerating global temperature rise, leading to severe weather events and long-term environmental impacts, and stresses the urgency for collective action."
        };

        var evaluator = Evaluator.FromData(ChatClient.GetInstance(), [test], x => x);
        evaluator.AddAnswerRelevancy(includeReason: true);
        var score = await evaluator.RunAsync();

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.AverageScore}");
        _testOutputHelper.WriteLine($"Result: {score.Tests.First().Results.First().Score.Result}");

        //Assert
        Assert.Equal(MetricScoreResult.Fail, score.Tests.First().Results.First().Score.Result);
    }

    [Fact]
    public async Task TestingEvaluator_Bias()
    {

        var test = new EvaluatorTestData
        {
            InitialInput = "Who is the best leader in the tech industry?",
            ActualOutput = "There are many great leaders in the tech industry, such as Satya Nadella, Tim Cook, and Susan Wojcicki.",
            ExpectedOutput = "A variety of tech leaders, including individuals like Satya Nadella, Tim Cook, and Susan Wojcicki, have significantly influenced the industry."
        };

        var evaluator = Evaluator.FromData(ChatClient.GetInstance(), [test], x => x);
        evaluator.AddBias(includeReason: true);
        var score = await evaluator.RunAsync();

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.AverageScore}");
        _testOutputHelper.WriteLine($"Result: {score.Tests.First().Results.First().Score.Result}");

        //Assert
        Assert.Equal(MetricScoreResult.Pass, score.Tests.First().Results.First().Score.Result);
    }

    [Fact]
    public async Task TestingEvaluator_ContextualPrecision()
    {

        var test = new EvaluatorTestData
        {
            InitialInput = "I need help with my account; I mentioned earlier that my account number is 12345, but I cannot log in.",
            ActualOutput = "Please reset your password using the forgot password link.",
            ExpectedOutput = "Since you mentioned account number 12345 earlier and stated you're having trouble logging in, please try resetting your password using the 'Forgot Password' link. If the issue persists, let me escalate your ticket for further assistance.",
            RetrievalContext = ["Previous conversation where the user mentioned account number 12345 and issues with logging in."]
        };

        var evaluator = Evaluator.FromData(ChatClient.GetInstance(), [test], x => x);
        evaluator.AddContextualPrecision(includeReason: true);
        var score = await evaluator.RunAsync();
        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.AverageScore}");
        _testOutputHelper.WriteLine($"Result: {score.Tests.First().Results.First().Score.Result}");

        //Assert
        Assert.Equal(MetricScoreResult.Pass, score.Tests.First().Results.First().Score.Result);
    }

    [Fact]
    public async Task TestingEvaluator_ContextualRecall()
    {

        var test = new EvaluatorTestData
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

        var evaluator = Evaluator.FromData(ChatClient.GetInstance(), [test], x => x);
        evaluator.AddContextualRecall();
        var score = await evaluator.RunAsync();

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.AverageScore}");
        _testOutputHelper.WriteLine($"Result: {score.Tests.First().Results.First().Score.Result}");

        //Assert
        Assert.Equal(MetricScoreResult.Pass, score.Tests.First().Results.First().Score.Result);
    }

    [Fact]
    public async Task TestingEvaluator_Faithfulness()
    {

        var test = new EvaluatorTestData
        {
            InitialInput = "I need help with my account; I mentioned earlier that my account number is 12345, but I cannot log in.",
            ActualOutput = "Your account number is 12345. Try resetting your password using the 'Forgot Password' link.",
            ExpectedOutput = "Since you mentioned account number 12345 earlier and stated you're having trouble logging in, please try resetting your password using the 'Forgot Password' link. If the issue persists, let me escalate your ticket for further assistance.",
            RetrievalContext =
            [
                "Previous conversation where the user mentioned account number 12345 and issues with logging in."
            ]
        };

        var evaluator = Evaluator.FromData(ChatClient.GetInstance(), [test], x => x);
        evaluator.AddFaithfulness(includeReason: true);
        var score = await evaluator.RunAsync();

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.AverageScore}");
        _testOutputHelper.WriteLine($"Result: {score.Tests.First().Results.First().Score.Result}");

        //Assert
        Assert.Equal(MetricScoreResult.Pass, score.Tests.First().Results.First().Score.Result);
    }

    [Fact]
    public async Task TestingEvaluator_GEval_Criteria()
    {

        var test = new EvaluatorTestData
        {
            InitialInput = "The United Nations has warned that the global economy is facing severe challenges, with many countries experiencing deep recessions due to the ongoing pandemic. In a new report, the UN highlights the increasing unemployment rates, widespread poverty, and disruptions to supply chains. While some countries are beginning to show signs of recovery, the overall situation remains uncertain. Governments are urged to prioritize fiscal support and sustainable development policies to avoid long-term economic stagnation.",
            ActualOutput = "The United Nations has issued a warning that the global economy is facing significant challenges, with numerous countries experiencing some very bad things. In a newly released report, the UN emphasizes the rising unemployment rates, increasing levels of poverty, and major disruptions to supply chains that have affected economies worldwide. While certain countries have started to show early signs of recovery, the overall economic outlook remains highly uncertain. The report urges governments to take proactive measures, prioritizing fiscal support and implementing sustainable development policies to prevent long-term economic stagnation and ensure a more stable future.",
            ExpectedOutput = "The United Nations has warned that the global economy is facing challenges due to the pandemic, with many things that make it difficult for these nations. While some nations are recovering, the global outlook remains uncertain, urging governments to prioritize fiscal support and sustainable development.",
        };

        var evaluator = Evaluator.FromData(ChatClient.GetInstance(), [test], x => x);
        evaluator.AddGEval("Is the summary shorter than the original article without omitting key details?");
        var score = await evaluator.RunAsync();

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.AverageScore}");
        _testOutputHelper.WriteLine($"Result: {score.Tests.First().Results.First().Score.Result}");

        //Assert
        Assert.Equal(MetricScoreResult.Fail, score.Tests.First().Results.First().Score.Result);
    }

    [Fact]
    public async Task TestingEvaluator_GEval_EvalSteps()
    {

        var test = new EvaluatorTestData
        {
            InitialInput = "The United Nations has warned that the global economy is facing severe challenges, with many countries experiencing deep recessions due to the ongoing pandemic. In a new report, the UN highlights the increasing unemployment rates, widespread poverty, and disruptions to supply chains. While some countries are beginning to show signs of recovery, the overall situation remains uncertain. Governments are urged to prioritize fiscal support and sustainable development policies to avoid long-term economic stagnation.",
            ActualOutput = "The UN has warned about global economic challenges caused by the pandemic, highlighting issues such as unemployment, poverty, and supply chain disruptions. Some countries are recovering, but uncertainty remains, with an emphasis on fiscal support and sustainable development.",
            ExpectedOutput = "The United Nations has warned that the global economy is facing challenges due to the pandemic, with rising unemployment, poverty, and supply chain disruptions. While some nations are recovering, the global outlook remains uncertain, urging governments to prioritize fiscal support and sustainable development.",
        };
        var steps = new List<string>
        {
            "Compare the summary with the original article to ensure that no facts have been misrepresented or omitted.",
            "Identify the key elements in the original article (e.g., wildfire threat to homes, evacuations, fire spread, dry conditions, high winds) and check if they are included in the summary.",
            "Measure the length of the summary against the original article. Ensure that the summary captures the most essential information without being overly long or excessively short."
        };

        var evaluator = Evaluator.FromData(ChatClient.GetInstance(), [test], x => x);
        evaluator.AddGEval(steps);
        var score = await evaluator.RunAsync();

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.AverageScore}");
        _testOutputHelper.WriteLine($"Result: {score.Tests.First().Results.First().Score.Result}");

        //Assert
        Assert.Equal(MetricScoreResult.Pass, score.Tests.First().Results.First().Score.Result);
    }

    [Fact]
    public async Task TestingEvaluator_Hallucination()
    {

        var test = new EvaluatorTestData
        {
            InitialInput = "Who was the first president of the United States?",
            ActualOutput = "The first president of the United States was George Washington.",
            Context = ["Verified historical records state George Washington was the first U.S. president."],
        };

        var evaluator = Evaluator.FromData(ChatClient.GetInstance(), [test], x => x);
        evaluator.AddHallucination();
        var score = await evaluator.RunAsync();

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.AverageScore}");
        _testOutputHelper.WriteLine($"Result: {score.Tests.First().Results.First().Score.Result}");

        //Assert
        Assert.Equal(MetricScoreResult.Pass, score.Tests.First().Results.First().Score.Result);
    }

    [Fact]
    public async Task TestingEvaluator_ExactMatch()
    {

        var test = new EvaluatorTestData
        {
            InitialInput = "",
            ExpectedOutput = "Hello",
            ActualOutput = "Hello"
        };

        var evaluator = Evaluator.FromData(null!, [test], x => x);
        evaluator.AddExactMatchMetric();
        var score = await evaluator.RunAsync();

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.AverageScore}");
        _testOutputHelper.WriteLine($"Result: {score.Tests.First().Results.First().Score.Result}");

        //Assert
        Assert.Equal(MetricScoreResult.Pass, score.Tests.First().Results.First().Score.Result);
    }

    [Fact]
    public async Task TestingEvaluator_RegexMatch()
    {

        var test = new EvaluatorTestData
        {
            InitialInput = "",
            ExpectedOutput = "Hello",
            ActualOutput = "Well, hello there!"
        };

        var evaluator = Evaluator.FromData(null!, [test], x => x);
        evaluator.AddRegexMatchMetric(@"\bhello\b", StringComparison.InvariantCultureIgnoreCase);
        var score = await evaluator.RunAsync();

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.AverageScore}");
        _testOutputHelper.WriteLine($"Result: {score.Tests.First().Results.First().Score.Result}");

        //Assert
        Assert.Equal(MetricScoreResult.Pass, score.Tests.First().Results.First().Score.Result);
    }

    [Fact]
    public async Task TestingEvaluator_PromptAlignment()
    {

        var test = new EvaluatorTestData
        {
            InitialInput = "Summarize this medical report for a patient in plain English. Do not include any medical jargon.",
            ActualOutput = "The report says your heart is healthy and there are no signs of serious problems. Your blood pressure and cholesterol levels are normal.",
            ExpectedOutput = "The report says your heart is healthy and there are no signs of serious problems. Your blood pressure and cholesterol levels are normal."
        };

        var evaluator = Evaluator.FromData(ChatClient.GetInstance(), [test], x => x);
        evaluator.AddPromptAlignment(promptInstructions: ["Summarize for a patient", "Use plain English", "Avoid medical jargon"], includeReason: true);
        var score = await evaluator.RunAsync();

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.AverageScore}");
        _testOutputHelper.WriteLine($"Result: {score.Tests.First().Results.First().Score.Result}");

        //Assert
        Assert.Equal(MetricScoreResult.Pass, score.Tests.First().Results.First().Score.Result);
    }

    [Fact]
    public async Task TestingEvaluator_Summarization_Default()
    {

        var test = new EvaluatorTestData
        {
            InitialInput = "The president announced a new climate policy today aimed at reducing carbon emissions by 30% over the next decade. Experts say this could significantly affect global warming trends.",
            ActualOutput = "The president introduced a climate policy to cut carbon emissions by 30% in 10 years, which experts believe will impact global warming."
        };

        var evaluator = Evaluator.FromData(ChatClient.GetInstance(), [test], x => x);
        evaluator.AddSummarization();
        var score = await evaluator.RunAsync();

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.AverageScore}");
        _testOutputHelper.WriteLine($"Result: {score.Tests.First().Results.First().Score.Result}");

        //Assert
        Assert.Equal(MetricScoreResult.Pass, score.Tests.First().Results.First().Score.Result);
    }

    [Fact]
    public async Task TestingEvaluator_Summarization_WithQuestions()
    {

        var test = new EvaluatorTestData
        {
            InitialInput = "A study on sleep patterns found that adults who sleep 7-8 hours a night perform better on cognitive tests and report better mental health. The study analyzed 10,000 adults across the US.",
            ActualOutput = "A US study of 10,000 adults found that sleeping 7-8 hours improves mental health and cognitive performance.",
        };

        var evaluator = Evaluator.FromData(ChatClient.GetInstance(), [test], x => x);
        evaluator.AddSummarization(assessmentQuestions: [
                "Does the summary mention that the study included 10,000 adults?",
                "Does the summary correctly identify 7-8 hours of sleep as the optimal range?",
                "Does the summary mention improved cognitive performance as a result?",
                "Does the summary include improved mental health as a finding?",
                "Does the summary indicate that the study was conducted in the US?"
            ]);
        var score = await evaluator.RunAsync();

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.AverageScore}");
        _testOutputHelper.WriteLine($"Result: {score.Tests.First().Results.First().Score.Result}");

        //Assert
        Assert.Equal(MetricScoreResult.Pass, score.Tests.First().Results.First().Score.Result);
    }

    [Fact]
    public async Task TestingEvaluator_TaskCompletion()
    {

        var test = new EvaluatorTestData
        {
            InitialInput = "I need to reset my password.",
            ActualOutput = "I�ve sent a password reset link to your registered email.",
            ToolsCalled = [
                new ToolCall
                {
                    Name = "Reset Password",
                    Description = "Resets a user's password and sends a reset link.",
                    InputParameters = new Dictionary<string, object?> { { "user_request", "reset password" } },
                    Output = "Password reset link sent."
                }
            ],
            Context = ["System logs confirm that a password reset link was sent."]
        };

        var evaluator = Evaluator.FromData(ChatClient.GetInstance(), [test], x => x);
        evaluator.AddTaskCompletion();
        var score = await evaluator.RunAsync();

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.AverageScore}");
        _testOutputHelper.WriteLine($"Result: {score.Tests.First().Results.First().Score.Result}");

        //Assert
        Assert.Equal(MetricScoreResult.Pass, score.Tests.First().Results.First().Score.Result);
    }

    [Fact]
    public async Task TestingEvaluator_ToolCorrectness()
    {

        var test = new EvaluatorTestData
        {
            InitialInput = "Look up product 101 and add it to my cart.",
            ActualOutput = """{"tool": "product_lookup"} -> {"tool": "add_to_cart"}""",
            ToolsCalled =
            [
                new ToolCall { Name = "product_lookup", InputParameters = new Dictionary<string, object?> { { "product_id", 101 } } },
                new ToolCall
                {
                    Name = "add_to_cart",
                    InputParameters = new Dictionary<string, object?> { { "product_id", 101 }, { "quantity", 1 } }
                }
            ],
            ExpectedTools =
            [
                new ToolCall { Name = "product_lookup", InputParameters = new Dictionary<string, object?> { { "product_id", 101 } } },
                new ToolCall {
                    Name = "add_to_cart",
                    InputParameters = new Dictionary<string, object?> { { "product_id", 101 }, { "quantity", 1 } }
                }
            ]
        };

        var evaluator = Evaluator.FromData(null!, [test], x => x);
        evaluator.AddToolCorrectness(shouldConsiderOrdering: true, evaluationParams: [ToolCallParamsEnum.INPUT_PARAMETERS, ToolCallParamsEnum.TOOL]);
        var score = await evaluator.RunAsync();

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.AverageScore}");
        _testOutputHelper.WriteLine($"Result: {score.Tests.First().Results.First().Score.Result}");

        //Assert
        Assert.Equal(MetricScoreResult.Pass, score.Tests.First().Results.First().Score.Result);
    }

    [Fact]
    public async Task TestingEvaluator_ToolCorrectness_Exact()
    {

        var test = new EvaluatorTestData
        {
            InitialInput = "Look up product 101 and add it to my cart.",
            ActualOutput = """{"tool": "product_lookup"} -> {"tool": "add_to_cart"}""",
            ToolsCalled =
            [
                new ToolCall { Name = "product_lookup", InputParameters = new Dictionary<string, object?> { { "product_id", 101 } } },
                new ToolCall
                {
                    Name = "add_to_cart",
                    InputParameters = new Dictionary<string, object?> { { "product_id", 101 }, { "quantity", 1 } }
                }
            ],
            ExpectedTools =
            [
                new ToolCall { Name = "product_lookup", InputParameters = new Dictionary<string, object?> { { "product_id", 101 } } },
                new ToolCall {
                    Name = "add_to_cart",
                    InputParameters = new Dictionary<string, object?> { { "product_id", 101 }, { "quantity", 1 } }
                }
            ]
        };

        var evaluator = Evaluator.FromData(null!, [test], x => x);
        evaluator.AddToolCorrectnessExactMatch(evaluationParams: [ToolCallParamsEnum.INPUT_PARAMETERS, ToolCallParamsEnum.TOOL]);
        var score = await evaluator.RunAsync();

        // Ouput
        _testOutputHelper.WriteLine(string.Empty);
        _testOutputHelper.WriteLine($"Total score: {score.AverageScore}");
        _testOutputHelper.WriteLine($"Result: {score.Tests.First().Results.First().Score.Result}");

        //Assert
        Assert.Equal(MetricScoreResult.Pass, score.Tests.First().Results.First().Score.Result);
    }

    public class TestCases
    {
        public string ActualInput { get; set; } = "";
        public string ActualOutput { get; set; }  = "";
    }
}
