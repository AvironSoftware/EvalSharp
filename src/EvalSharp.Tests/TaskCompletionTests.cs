using EvalSharp.Models;
using EvalSharp.Scoring;
using Xunit.Abstractions;

namespace EvalSharp.Tests;

public class TaskCompletionTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly TaskCompletionMetricConfiguration _config;
    private readonly TaskCompletionMetric _taskCompletionMetric;

    public TaskCompletionTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _config = new TaskCompletionMetricConfiguration { IncludeReason = true, Threshold = 0.5 };
        _taskCompletionMetric = new TaskCompletionMetric(ChatClient.GetInstance(), _config);
    }

    private async Task RunTestAsync(
        string testName,
        string initialInput,
        string actualOutput,
        List<ToolCall> toolsCalled,
        List<string> context,
        bool shouldPass)
    {
        var test = new
        {
            InitialInput = initialInput,
            ActualOutput = actualOutput,
            ToolsCalled = toolsCalled
        };
        var evalContext = new EvaluatorTestData
        {
            InitialInput = test.InitialInput,
            ActualOutput = test.ActualOutput,
            ToolsCalled = test.ToolsCalled
        };

        var score = await _taskCompletionMetric.ScoreAsync(evalContext);

        _testOutputHelper.WriteLine($"Score: {score.Score}");
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

    // Successful Cases
    [Fact]
    public async Task TaskCompletion_CustomerSupport_Success() =>
        await RunTestAsync(
            "TaskCompletion_CustomerSupport_Success",
            "I need to reset my password.",
            "I’ve sent a password reset link to your registered email.",
            [
                new ToolCall
                {
                    Name = "Reset Password",
                    Description = "Resets a user's password and sends a reset link.",
                    InputParameters = new Dictionary<string, object?> { { "user_request", "reset password" } },
                    Output = "Password reset link sent."
                }
            ],
            ["System logs confirm that a password reset link was sent."],
            true);

    [Fact]
    public async Task TaskCompletion_CodeGeneration_Success() =>
        await RunTestAsync(
            "TaskCompletion_CodeGeneration_Success",
            "Write a Python function to reverse a string.",
            "def reverse_string(s): return s[::-1]",
            [
                new ToolCall
                {
                    Name = "Generate Code",
                    Description = "Generates code based on user request.",
                    InputParameters = new Dictionary<string, object?>
                    {
                        { "language", "Python" },
                        { "task", "reverse a string" }
                    },
                    Output = "def reverse_string(s): return s[::-1]"
                }
            ],
            ["Correct Python syntax for reversing a string."],
            true);

    [Fact]
    public async Task TaskCompletion_SQLQuery_Success() =>
        await RunTestAsync(
            "TaskCompletion_SQLQuery_Success",
            "Write a SQL query to get the top 5 highest-paid employees from the 'employees' table.",
            "SELECT * FROM employees ORDER BY salary DESC LIMIT 5;",
            [
                new ToolCall
                {
                    Name = "Execute SQL Query",
                    Description = "Executes a SQL query to retrieve data.",
                    InputParameters = new Dictionary<string, object?>
                    {
                        { "query", "SELECT * FROM employees ORDER BY salary DESC LIMIT 5;" }
                    },
                    Output = "Query executed successfully with correct results."
                }
            ],
            ["Correct SQL syntax for retrieving highest-paid employees."],
            true);

    [Fact]
    public async Task TaskCompletion_EmailDrafting_Success() =>
        await RunTestAsync(
            "TaskCompletion_EmailDrafting_Success",
            "Write a professional email apologizing for a late shipment and offering a discount.",
            "Dear Customer, we apologize for the delay in your shipment. As compensation, we are offering a 10% discount on your next purchase.",
            [
                new ToolCall
                {
                    Name = "Send Email",
                    Description = "Drafts and sends an email based on input parameters.",
                    InputParameters = new Dictionary<string, object?> { { "subject", "Apology for late shipment" }, { "discount", "10%" } },
                    Output = "Email sent with apology and discount offer."
                }
            ],
            ["Apology and discount offer are included."],
            true);

    [Fact]
    public async Task TaskCompletion_VirtualAssistant_Success() =>
        await RunTestAsync(
            "TaskCompletion_VirtualAssistant_Success",
            "Schedule a meeting with John for tomorrow at 2 PM and send an invite.",
            "Meeting scheduled and invite sent.",
            [
                new ToolCall
                {
                    Name = "Schedule Meeting",
                    Description = "Schedules a meeting in the calendar.",
                    InputParameters = new Dictionary<string, object?> { { "participant", "John" }, { "time", "Tomorrow at 2 PM" } },
                    Output = "Meeting scheduled."
                },
                new ToolCall
                {
                    Name = "Send Calendar Invite",
                    Description = "Sends an invite to a meeting participant.",
                    InputParameters = new Dictionary<string, object?> { { "recipient", "John" }, { "meeting_time", "Tomorrow at 2 PM" } },
                    Output = "Calendar invite sent."
                }
            ],
            ["Confirmed meeting and invite in calendar system."],
            true);

    [Fact]
    public async Task TaskCompletion_Itinerary_Success() =>
        await RunTestAsync(
            "TaskCompletion_Itinerary_Success",
            "Plan a 3-day itinerary for Paris with cultural landmarks and local cuisine.",
            "Day 1: Eiffel Tower, dinner at Le Jules Verne. Day 2: Louvre Museum, lunch at Angelina Paris. Day 3: Montmartre, evening at a wine bar.",
            [
                new ToolCall
                {
                    Name = "Itinerary Generator",
                    Description = "Creates travel plans based on destination and duration.",
                    InputParameters = new Dictionary<string, object?> { { "destination", "Paris" }, { "days", 3 } },
                    Output = new List<string> { 
                        "Day 1: Eiffel Tower, Le Jules Verne.",
                        "Day 2: Louvre Museum, Angelina Paris.",
                        "Day 3: Montmartre, wine bar." 
                    }
                },
                new ToolCall
                {
                    Name = "Restaurant Finder",
                    Description = "Finds top restaurants in a city.",
                    InputParameters = new Dictionary<string, object?> { { "city", "Paris" } },
                    Output = new List<string> { "Le Jules Verne", "Angelina Paris", "local wine bars" }
                }
            ],
            [],
            true);

    // Failing Cases


    [Fact]
    public async Task TaskCompletion_CustomerSupport_Fail() =>
        await RunTestAsync(
            "TaskCompletion_CustomerSupport_Fail",
            "I need to reset my password.",
            "Here’s a link to our help center.",
            [], // No tool called
            ["No confirmation of a reset link being sent."],
            false);

    [Fact]
    public async Task TaskCompletion_CodeGeneration_Fail() =>
        await RunTestAsync(
            "TaskCompletion_CodeGeneration_Fail",
            "Write a Python function to reverse a string.",
            "def reverse_string(s): return s.reverse()",
            [
                new ToolCall
                {
                    Name = "Generate Code",
                    Description = "Generates code based on user request.",
                    InputParameters = new Dictionary<string, object?>
                    {
                        { "language", "Python" },
                        { "task", "reverse a string" }
                    },
                    Output = "def reverse_string(s): return s.reverse()"
                }
            ],
            ["Incorrect syntax; `.reverse()` does not exist for strings."],
            false);


    [Fact]
    public async Task TaskCompletion_SQLQuery_Fail() =>
        await RunTestAsync(
            "TaskCompletion_SQLQuery_Fail",
            "Write a SQL query to get the top 5 highest-paid employees from the 'employees' table.",
            "SELECT * FROM employees ORDER BY name DESC;",
            [
                new ToolCall
                {
                    Name = "Execute SQL Query",
                    Description = "Executes a SQL query to retrieve data.",
                    InputParameters = new Dictionary<string, object?> { { "query", "SELECT * FROM employees ORDER BY name DESC;" } },
                    Output = "Query executed but incorrect sorting applied."
                }
            ],
            ["Query does not sort by salary and does not limit results."],
            false);

    [Fact]
    public async Task TaskCompletion_EmailDrafting_Fail() =>
        await RunTestAsync(
            "TaskCompletion_EmailDrafting_Fail",
            "Write a professional email apologizing for a late shipment and offering a discount.",
            "Dear Customer, we apologize for the delay in your shipment.",
            [
                new ToolCall
                {
                    Name = "Send Email",
                    Description = "Drafts and sends an email based on input parameters.",
                    InputParameters = new Dictionary<string, object?> { { "subject", "Apology for late shipment" }, { "discount", null } },
                    Output = "Email sent, but missing discount offer."
                }
            ],
            ["Did not include the discount offer."],
            false);

    [Fact]
    public async Task TaskCompletion_VirtualAssistant_Fail() =>
        await RunTestAsync(
            "TaskCompletion_VirtualAssistant_Fail",
            "Schedule a meeting with John for tomorrow at 2 PM and send an invite.",
            "Meeting scheduled.",
            [
                new ToolCall
                {
                    Name = "Schedule Meeting",
                    Description = "Schedules a meeting in the calendar.",
                    InputParameters = new Dictionary<string, object?> { { "participant", "John" }, { "time", "Tomorrow at 2 PM" } },
                    Output = "Meeting scheduled."
                }
            ],
            ["Did not send an invite."],
            false);
}