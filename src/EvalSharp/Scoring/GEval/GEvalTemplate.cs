using System.Text;

namespace EvalSharp.Scoring.GEval
{
    internal static class GEvalTemplate
    {
        private const string Parameters = "Initial Input, Actual Output, Expected Output";
        public static string GenerateEvaluation(string initialInput, string actualOutput, string? expectedOutput, List<string>? evaluationStepList)
        {
            var evaluationSteps = string.Join("\n", evaluationStepList ?? []);
            // 2. Build the evaluation text from the context.
            var evaluationTextBuilder = new StringBuilder();
            evaluationTextBuilder.AppendLine($"Initial Input: {initialInput}");
            evaluationTextBuilder.AppendLine();
            evaluationTextBuilder.AppendLine($"Actual Output: {actualOutput}");
            evaluationTextBuilder.AppendLine();
            evaluationTextBuilder.AppendLine($"Expected Output: {expectedOutput}");
            evaluationTextBuilder.AppendLine();
            string evaluationText = evaluationTextBuilder.ToString();

            //Prompt adapted from deepeval, licensed under Apache 2.0
            //DeepEval Repo: https://github.com/confident-ai/deepeval

            // 3. Build the evaluation prompt asking for a score and reason.
            return $@"Given the evaluation steps, return a JSON with two keys: 
1) a ""score"" key ranging from 0 - 10 (with 10 indicating perfect adherence to the criteria, and 0 indicating complete failure), 
and 2) a ""reason"" key that provides a concise explanation for the score. 
Mention specific details from {Parameters} in your reason.

Evaluation Steps:
{evaluationSteps}

{evaluationText}

**
IMPORTANT: Please return only valid JSON containing the ""score"" and ""reason"" keys.
Example JSON:
{{
    ""score"": 0,
    ""reason"": ""The response does not follow the evaluation steps provided.""
}}
**

JSON:";

        }

        public static string GenerateEvaluationSteps(string criteria)
        {
            //Prompt adapted from deepeval, licensed under Apache 2.0
            //DeepEval Repo: https://github.com/confident-ai/deepeval

            return $$"""
Given an evaluation criteria which outlines how you should judge the {{Parameters}}, generate 3-4 concise evaluation steps based on the criteria below. You MUST make it clear how to evaluate {{Parameters}} in relation to one another.

Evaluation Criteria:
{{criteria}}

**
IMPORTANT: Please make sure to only return in JSON format, with the "steps" key as a list of strings. No extra words are needed.
Example JSON:
{
    "steps": ["Step 1 description", "Step 2 description", "Step 3 description"]
}
**

JSON:
""";
        }
    
    }
}
