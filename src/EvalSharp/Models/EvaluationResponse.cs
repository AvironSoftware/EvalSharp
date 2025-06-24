namespace EvalSharp.Models
{
    internal class EvaluationResponse
    {
        public required int Score { get; set; }
        public required string Reason { get; set; }
    }
}
