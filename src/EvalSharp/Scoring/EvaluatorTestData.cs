using EvalSharp.Models;

namespace EvalSharp.Scoring;

/// <summary>
/// Represents the test data used for evaluating the performance of the LLM.
/// </summary>
public class EvaluatorTestData
{
    /// <summary>
    /// The expected output of the test case.
    /// </summary>
    public string? ExpectedOutput { get; init; }

    /// <summary>
    /// The actual output of the test case from the LLM.
    /// </summary>
    public string? ActualOutput { get; init; }

    /// <summary>
    /// The initial input is the user interaction with the LLM.
    /// </summary>
    public string? InitialInput { get; init; }

    /// <summary>
    /// A list of background information strings that your app actually found when answering. 
    /// Use this to compare what was retrieved against the ideal <see cref="Context"/>.
    /// </summary> 
    public List<string>? RetrievalContext { get; set; }

    /// <summary>
    /// A list of ideal background information strings that best answer the question. 
    /// Think of this as the "correct" facts you want the system to use.
    /// </summary>
    public List<string>? Context { get; set; }

    /// <summary>
    /// Tools your LLM actually invoked during execution.
    /// </summary>
    public List<ToolCall>? ToolsCalled { get; set; }

    /// <summary>
    /// The expected tools your LLM should have invoked during execution.
    /// </summary>
    public List<ToolCall>? ExpectedTools { get; set; }
}