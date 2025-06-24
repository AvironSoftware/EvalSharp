using EvalSharp.Models.Enums;

namespace EvalSharp.Models;

internal class SummarizationCoverageVerdict
{
    public required string Question { get; set; }
    public required VerdictEnum OriginalVerdict { get; set; }
    public VerdictEnum? SummaryVerdict { get; set; }
}