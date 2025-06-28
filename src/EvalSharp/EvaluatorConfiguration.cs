namespace EvalSharp;

/// <summary>
/// Configuration for an evaluator.
/// </summary>
public class EvaluatorConfiguration
{
    /// <summary>
    /// The system prompt used for all LLM-as-a-judge metrics. Note that this will not override the system prompt set in an individual metric configuration.
    /// </summary>
    public string? SystemPrompt { get; set; }
    
    /// <summary>
    /// The temperature used for all LLM-as-a-judge metrics. Note that this will not override the temperature set in an individual metric configuration.
    /// </summary>
    public float? Temperature { get; set; }

    /// <summary>
    /// Enforces a binary metric score. Setting to true effectively sets the threshold to 1, meaning only a perfect score will pass.
    /// </summary>
    public bool StrictMode { get; set; } = false;
}