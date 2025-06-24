namespace EvalSharp.Models.Enums;

/// <summary>  
/// Enum representing the parameters for a tool call.
/// Instructs ToolCorrectness metric which components of the Tool to evaluate.
/// </summary>  
public enum ToolCallParamsEnum
{
    /// <summary>  
    /// Represents the name of the tool being called.  
    /// </summary>  
    TOOL,

    /// <summary>  
    /// Represents the input parameters for the tool call.  
    /// </summary>  
    INPUT_PARAMETERS,

    /// <summary>  
    /// Represents the output of the tool call.  
    /// </summary>  
    OUTPUT
}