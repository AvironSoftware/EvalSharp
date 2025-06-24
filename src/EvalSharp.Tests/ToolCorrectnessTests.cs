using EvalSharp.Models;
using EvalSharp.Models.Enums;
using EvalSharp.Scoring;
using Xunit.Abstractions;

namespace EvalSharp.Tests;

public class ToolCorrectnessTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ToolCorrectnessTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    private async Task RunTestAsync(string input, string actualOutput, List<ToolCall> toolsCalled, List<ToolCall> expectedTools, bool shouldPass, List<ToolCallParamsEnum> evalParams, bool considerOrdering = false)
    {
        var config = new ToolCorrectnessMetricConfiguration
        {
            ShouldExactMatch = true,
            ShouldConsiderOrdering = considerOrdering,
            EvaluationParams = evalParams,
            Threshold = 0.6,
        };
        var metric = new ToolCorrectnessMetric(config);

        var test = new
        {
            InitialInput = input,
            ActualOutput = actualOutput,
            ToolsCalled = toolsCalled,
            ExpectedTools = expectedTools
        };
        var context = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ToolsCalled = test.ToolsCalled,
            ExpectedTools = test.ExpectedTools
        };

        var score = await metric.ScoreAsync(context);

        _testOutputHelper.WriteLine($"Total score: {score.Score}");
        _testOutputHelper.WriteLine($"Result: {score.Result}");

        if (shouldPass)
        {
            Assert.True(score.Score >= 0.6);
            Assert.Equal(MetricScoreResult.Pass, score.Result);
        }
        else
        {
            Assert.True(score.Score < 0.6);
            Assert.Equal(MetricScoreResult.Fail, score.Result);
        }
    }

    [Fact]
    public async Task ToolCorrectness_Ordering_Success() =>
        await RunTestAsync("Look up product 101 and add it to my cart.",
            """{"tool": "product_lookup"} -> {"tool": "add_to_cart"}""",
            new List<ToolCall>
            {
                new ToolCall { Name = "product_lookup", InputParameters = new Dictionary<string, object?> { { "product_id", 101 } } },
                new ToolCall
                {
                    Name = "add_to_cart",
                    InputParameters = new Dictionary<string, object?> { { "product_id", 101 }, { "quantity", 1 } }
                }
            },
            new List<ToolCall>
            {
                new ToolCall { Name = "product_lookup", InputParameters = new Dictionary<string, object?> { { "product_id", 101 } } },
                new ToolCall { 
                    Name = "add_to_cart", 
                    InputParameters = new Dictionary<string, object?> { { "product_id", 101 }, { "quantity", 1 } } 
                } 
            },
            true, [ToolCallParamsEnum.TOOL], true);

    [Fact]
    public async Task ToolCorrectness_Ordering_Missing_Success() =>
        await RunTestAsync("Look up product 101 and add it to my cart.",
            """{"tool": "add_to_cart"}""",
            new List<ToolCall>
            {
                new ToolCall { Name = "product_lookup", InputParameters = new Dictionary<string, object?> { { "product_id", 101 } } },
                new ToolCall { Name = "add_to_cart", InputParameters = new Dictionary<string, object?> { { "product_id", 101 }, { "quantity", 1 } } }
            },
            new List<ToolCall>
            {
                new ToolCall { Name = "product_lookup", InputParameters = new Dictionary<string, object?> { { "product_id", 101 } } },
                new ToolCall { Name = "add_to_cart", InputParameters = new Dictionary<string, object?> { { "product_id", 101 }, { "quantity", 1 } } }
            },
            true, [ToolCallParamsEnum.TOOL], true);

    [Fact]
    public async Task ToolCorrectness_Ordering_Missing_Success_Two() =>
        await RunTestAsync("Look up product 101, add it to my cart, and checkout.",
            """{"tool": "add_to_cart"}""",
            new List<ToolCall>
            {
                new ToolCall { Name = "product_lookup", InputParameters = new Dictionary<string, object?> { { "product_id", 101 } } },
                new ToolCall { Name = "add_to_cart", InputParameters = new Dictionary<string, object?> { { "product_id", 101 }, { "quantity", 1 } } },
                new ToolCall { Name = "checkout", InputParameters = new Dictionary<string, object?> { { "product_id", 101 }, { "total", 100.00 } } }
            },
            new List<ToolCall>
            {
                new ToolCall { Name = "product_lookup", InputParameters = new Dictionary<string, object?> { { "product_id", 101 } } },
                new ToolCall { Name = "add_to_cart", InputParameters = new Dictionary<string, object?> { { "product_id", 101 }, { "quantity", 1 } } },
                new ToolCall { Name = "checkout", InputParameters = new Dictionary<string, object?> { { "product_id", 101 }, { "total", 100.00 } } }
            },
            true, [ToolCallParamsEnum.TOOL], true);

    [Fact]
    public async Task ToolCorrectness_Ordering_Output_Success() =>
        await RunTestAsync("Look up product 101 and add it to my cart.",
            """{"tool": "product_lookup"} -> {"tool": "add_to_cart"}""",
            new List<ToolCall>
            {
                new ToolCall
                {
                    Name = "product_lookup",
                    InputParameters = new Dictionary<string, object?> { { "product_id", 101 } },
                    Output = new { x = true }
                },
                new ToolCall {
                    Name = "add_to_cart",
                    InputParameters = new Dictionary<string, object?> { { "product_id", 101 }, { "quantity", 1 } },
                    Output = new { x = false }
                }
            },
            new List<ToolCall>
            {
                new ToolCall
                {
                    Name = "product_lookup",
                    InputParameters = new Dictionary<string, object?> { { "product_id", 101 } },
                    Output = new { x = true }
                },
                new ToolCall {
                    Name = "add_to_cart",
                    InputParameters = new Dictionary<string, object?> { { "product_id", 101 }, { "quantity", 1 } },
                    Output = new { x = false }
                }
            },
            true, [ToolCallParamsEnum.TOOL, ToolCallParamsEnum.OUTPUT], true);


    [Fact]
    public async Task ToolCorrectness_Ordering_Missing_Fail() =>
        await RunTestAsync("Look up product 101 and add it to my cart.",
            """{"tool": "add_to_cart"}""",
            new List<ToolCall> { new ToolCall { Name = "add_to_cart", InputParameters = new Dictionary<string, object?> { { "product_id", 101 }, { "quantity", 1 } } } },
            new List<ToolCall>
            {
                new ToolCall { Name = "product_lookup", InputParameters = new Dictionary<string, object?> { { "product_id", 101 } } },
                new ToolCall { Name = "add_to_cart", InputParameters = new Dictionary<string, object?> { { "product_id", 101 }, { "quantity", 1 } } }
            },
            false, [ToolCallParamsEnum.TOOL], true);

    [Fact]
    public async Task ToolCorrectness_Ordering_Missing_Fail_Two() =>
        await RunTestAsync("Look up product 101, add it to my cart, and checkout.",
            """{"tool": "add_to_cart"}""",
            new List<ToolCall> { new ToolCall { Name = "add_to_cart", InputParameters = new Dictionary<string, object?> { { "product_id", 101 }, { "quantity", 1 } } } },
            new List<ToolCall>
            {
                new ToolCall { Name = "product_lookup", InputParameters = new Dictionary<string, object?> { { "product_id", 101 } } },
                new ToolCall { Name = "add_to_cart", InputParameters = new Dictionary<string, object?> { { "product_id", 101 }, { "quantity", 1 } } },
                new ToolCall { Name = "checkout", InputParameters = new Dictionary<string, object?> { { "product_id", 101 }, { "total", 100.00 } } }
            },
            false, [ToolCallParamsEnum.TOOL], true);

    [Fact]
    public async Task ToolCorrectness_Ordering_Fail() =>
        await RunTestAsync("Look up product 101 and add it to my cart.",
            """{"tool": "add_to_cart"}""",
            new List<ToolCall> 
            { 
                new ToolCall { Name = "add_to_cart", InputParameters = new Dictionary<string, object?> { { "product_id", 101 }, { "quantity", 1 } } },
                new ToolCall { Name = "product_lookup", InputParameters = new Dictionary<string, object?> { { "product_id", 101 } } },
            },
            new List<ToolCall>
            {
                new ToolCall { Name = "product_lookup", InputParameters = new Dictionary<string, object?> { { "product_id", 101 } } },
                new ToolCall { Name = "add_to_cart", InputParameters = new Dictionary<string, object?> { { "product_id", 101 }, { "quantity", 1 } } }
            },
            false, [ToolCallParamsEnum.TOOL], true);

    [Fact]
    public async Task ToolCorrectness_Ordering_Output_Fail() =>
        await RunTestAsync("Look up product 101 and add it to my cart.",
            """{"tool": "product_lookup"} -> {"tool": "add_to_cart"}""",
            new List<ToolCall>
            {
                new ToolCall 
                { 
                    Name = "product_lookup", 
                    InputParameters = new Dictionary<string, object?> { { "product_id", 101 } } ,
                    Output = new { x = true }
                },
                new ToolCall
                {
                    Name = "add_to_cart",
                    InputParameters = new Dictionary<string, object?> { { "product_id", 101 }, { "quantity", 1 } },
                    Output = new { x = true }
                }
            },
            new List<ToolCall>
            {
                new ToolCall 
                { 
                    Name = "product_lookup", 
                    InputParameters = new Dictionary<string, object?> { { "product_id", 101 } }, 
                    Output = new { x = true }
                },
                new ToolCall {
                    Name = "add_to_cart",
                    InputParameters = new Dictionary<string, object?> { { "product_id", 101 }, { "quantity", 1 } },
                    Output = new { x = false }
                }
            },
            false, [ToolCallParamsEnum.TOOL, ToolCallParamsEnum.OUTPUT], true);

    [Fact]
    public async Task ToolCorrectness_Output_Fail() =>
        await RunTestAsync("Look up product 101 and add it to my cart.",
            """{"tool": "product_lookup"} -> {"tool": "add_to_cart"}""",
            new List<ToolCall>
            {
                new ToolCall
                {
                    Name = "product_lookup",
                    InputParameters = new Dictionary<string, object?> { { "product_id", 101 } } ,
                    Output = new { x = true }
                },
                new ToolCall
                {
                    Name = "add_to_cart",
                    InputParameters = new Dictionary<string, object?> { { "product_id", 101 }, { "quantity", 1 } },
                    Output = new { x = true }
                }
            },
            new List<ToolCall>
            {
                new ToolCall
                {
                    Name = "product_lookup",
                    InputParameters = new Dictionary<string, object?> { { "product_id", 101 } },
                    Output = new { x = true }
                },
                new ToolCall {
                    Name = "add_to_cart",
                    InputParameters = new Dictionary<string, object?> { { "product_id", 101 }, { "quantity", 1 } },
                    Output = new { x = false }
                }
            },
            false, [ToolCallParamsEnum.TOOL, ToolCallParamsEnum.OUTPUT], false);

    [Fact]
    public async Task ToolCorrectness_Calculator_Success() =>
        await RunTestAsync("What is 3 times (5 + 2)?",
            """{"tool": "calculator", "expression": "3 * (5 + 2)"}""",
            new List<ToolCall> { new ToolCall { Name = "calculator", InputParameters = new Dictionary<string, object?> { { "expression", "3 * (5 + 2)" } } } },
            new List<ToolCall> { new ToolCall { Name = "calculator", InputParameters = new Dictionary<string, object?> { { "expression", "3 * (5 + 2)" } } } },
            true, [ToolCallParamsEnum.TOOL], true);

    [Fact]
    public async Task ToolCorrectness_Calculator_Fail() =>
        await RunTestAsync("What is 3 times (5 + 2)?",
            """{"tool": "calculator", "expression": "3 * 5 + )2("}""",
            new List<ToolCall> { new ToolCall { Name = "calculator", InputParameters = new Dictionary<string, object?> { { "bmi", "healthy" } } } },
            new List<ToolCall> { new ToolCall { Name = "calculator", InputParameters = new Dictionary<string, object?> { { "expression", "3 * (5 + 2)" } } } },
            false, [ToolCallParamsEnum.TOOL]);

    [Fact]
    public async Task ToolCorrectness_Function_Call_Success() =>
        await RunTestAsync("Retrieve the user profile for ID 123.",
            "get_user_profile(user_id=123)",
            new List<ToolCall> { new ToolCall { Name = "get_user_profile", InputParameters = new Dictionary<string, object?> { { "user_id", 123 } } } },
            new List<ToolCall> { new ToolCall { Name = "get_user_profile", InputParameters = new Dictionary<string, object?> { { "user_id", 123 } } } },
            true, [ToolCallParamsEnum.TOOL]);

    [Fact]
    public async Task ToolCorrectness_Function_Call_Fail() =>
        await RunTestAsync("Retrieve the user profile for ID 123.",
            """getUserProfile(1234, "extra_arg")""",
            new List<ToolCall> { new ToolCall { Name = "getUserProfile", InputParameters = new Dictionary<string, object?>() } },
            new List<ToolCall> { new ToolCall { Name = "get_user_profile", InputParameters = new Dictionary<string, object?> { { "user_id", 123 } } } },
            false, [ToolCallParamsEnum.TOOL]);

}