namespace EvalSharp.Models;

internal class ScoredVerdictModel
{
    public required double Verdict { get; set; }
    public string? Reason { get; set; }
}