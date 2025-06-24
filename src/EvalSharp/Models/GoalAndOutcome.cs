using System.Text.Json.Serialization;

namespace EvalSharp.Models;

internal class GoalAndOutcome
{
    [JsonPropertyName("user_goal")]
    public string UserGoal { get; set; } = string.Empty;

    [JsonPropertyName("task_outcome")]
    public string TaskOutcome { get; set; } = string.Empty;
}