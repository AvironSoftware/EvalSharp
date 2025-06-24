namespace EvalSharp.Models.Enums;

/// <summary>
/// Represents the possible verdicts for evaluation.
/// </summary>
public enum VerdictEnum
{
    /// <summary>
    /// Default value indicating an unknown verdict.
    /// </summary>
    Idk = 0, // keep as default value

    /// <summary>
    /// Indicates a negative verdict.
    /// </summary>
    No,

    /// <summary>
    /// Indicates a positive verdict.
    /// </summary>
    Yes
}