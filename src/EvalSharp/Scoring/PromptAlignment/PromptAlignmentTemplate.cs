namespace EvalSharp.Scoring.PromptAlignment;
internal static class PromptAlignmentTemplate
{
    //Prompt adapted from deepeval, licensed under Apache 2.0
    //DeepEval Repo: https://github.com/confident-ai/deepeval
    public static string GenerateVerdicts(List<string> promptInstructions, string input, string actualOutput)
    {
        string formattedInstructions = promptInstructions.ToFormattedList();

        return $$"""
                 For the provided list of prompt instructions, determine whether each instruction has been followed in the LLM actual output.
                 Please generate a list of JSON with two keys: `verdict` and `reason`.
                 The 'verdict' key should STRICTLY be either a 'yes' or 'no'. Only answer 'yes' if the instruction COMPLETELY follows the instruction, and 'no' otherwise.
                 You should be EXTRA STRICT AND CAREFUL when giving a 'yes'.
                 The 'reason' is the reason for the verdict.
                 Provide a 'reason' ONLY if the answer is 'no'. 
                 The provided prompt instructions are the instructions to be followed in the prompt, which you have no access to.

                 **
                 IMPORTANT: Please make sure to only return in JSON format, with the 'verdicts' key mapping to a list of JSON objects.
                 Example input: What number is the stars of the sky?
                 Example actual output: HEY THERE! I think what you meant is "What is the number of stars in the sky", but unforunately I don't know the answer to it.
                 Example prompt instructions: ["Answer the input in a well-mannered fashion.", "Do not correct user of any grammatical errors.", "Respond in all upper case"]
                 Example JSON:
                 {
                     "verdicts": [
                         {
                             "verdict": "yes"
                         },
                         {
                             "verdict": "no",
                             "reason": "The LLM corrected the user when the user used the wrong grammar in asking about the number of stars in the sky."
                         },
                         {
                             "verdict": "no",
                             "reason": "The LLM only made 'HEY THERE' uppercase, which does not follow the instruction of making everything uppercase completely."
                         }
                     ]  
                 }

                 Since you are going to generate a verdict for each instruction, the number of 'verdicts' SHOULD BE STRICTLY EQUAL to the number of prompt instructions.
                 **          

                 Prompt Instructions:
                 {{formattedInstructions}}

                 Input:
                 {{input}}

                 LLM Actual Output:
                 {{actualOutput}}

                 JSON:
                 """;
    }

    //Prompt adapted from deepeval, licensed under Apache 2.0
    //DeepEval Repo: https://github.com/confident-ai/deepeval
    public static string GenerateReason(List<string> unalignmentReasons, string input, string actualOutput, double score)
    {
        string formattedReasons = unalignmentReasons.ToFormattedList();

        return $$"""
                 Given the prompt alignment score, the reaons for unalignment found in the LLM actual output, the actual output, and input, provide a CONCISE reason for the score. Explain why it is not higher, but also why it is at its current score.
                 The unalignments represent prompt instructions that are not followed by the LLM in the actual output.
                 If there no unaligments, just say something positive with an upbeat encouraging tone (but don't overdo it otherwise it gets annoying).
                 Don't have to talk about whether the actual output is a good fit for the input, access ENTIRELY based on the unalignment reasons.

                 **
                 IMPORTANT: Please make sure to only return in JSON format, with the 'reason' key providing the reason.
                 Example JSON:
                 {
                     "reason": "The score is <prompt_alignment_score> because <your_reason>."
                 }
                 **

                 Input:
                 {{input}}

                 LLM Actual Output:
                 {{actualOutput}}

                 Prompt Alignment Score:
                 {{score:F2}}

                 Reasons for unalignment:
                 {{formattedReasons}}

                 JSON:
                 """;
    }
}