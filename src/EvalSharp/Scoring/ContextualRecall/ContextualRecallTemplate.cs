﻿using System.Text.Json;
using EvalSharp.Models;
using EvalSharp.Models.Enums;

namespace EvalSharp.Scoring.ContextualRecall
{
    internal static class ContextualRecallTemplate
    {
        //Prompt adapted from deepeval, licensed under Apache 2.0
        //DeepEval Repo: https://github.com/confident-ai/deepeval
        public static string GenerateReason(string expectedOutput, VerdictModel[] verdicts, double score)
        {
            var supportiveReasons = verdicts.GetReasons(VerdictEnum.Yes);
            var unsupportiveReasons = verdicts.GetReasons(VerdictEnum.No);

            return $@"Given the original expected output, a list of supportive reasons, and a list of unsupportive reasons (which are deduced directly from the 'expected output'), and a contextual recall score (closer to 1 the better), summarize a CONCISE reason for the score.
A supportive reason is the reason why a certain sentence in the original expected output can be attributed to the node in the retrieval context.
An unsupportive reason is the reason why a certain sentence in the original expected output cannot be attributed to anything in the retrieval context.
In your reason, you should relate supportive/unsupportive reasons to the sentence number in expected output, and include info regarding the node number in retrieval context to support your final reason. The first mention of ""node(s)"" should specify ""node(s) in retrieval context"".

**
IMPORTANT: Please make sure to only return in JSON format, with the 'reason' key providing the reason.
Example JSON:
{{
    ""reason"": ""The score is <contextual_recall_score> because <your_reason>.""
}}

DO NOT mention 'supportive reasons' and 'unsupportive reasons' in your reason, these terms are just here for you to understand the broader scope of things.
If the score is 1, keep it short and say something positive with an upbeat encouraging tone (but don't overdo it, otherwise it gets annoying).
**

Contextual Recall Score:
{score:0.00}

Expected Output:
{expectedOutput}

Supportive Reasons:
{JsonSerializer.Serialize(supportiveReasons)}

Unsupportive Reasons:
{JsonSerializer.Serialize(unsupportiveReasons)}

JSON:";
        }

        //Prompt adapted from deepeval, licensed under Apache 2.0
        //DeepEval Repo: https://github.com/confident-ai/deepeval
        public static string GenerateVerdicts(string expectedOutput, List<string> retrievalContext)
        {
            var retrievalContextFormatted = retrievalContext.ToFormattedList();

            return $@"For EACH sentence in the given expected output below, determine whether the sentence can be attributed to the nodes of retrieval contexts. Please generate a list of JSON with two keys: `verdict` and `reason`.
The `verdict` key should STRICTLY be either a 'yes' or 'no'. Answer 'yes' if the sentence can be attributed to any parts of the retrieval context, else answer 'no'.
The `reason` key should provide a reason why to the verdict. In the reason, you should aim to include the node(s) count in the retrieval context (eg., 1st node, and 2nd node in the retrieval context) that is attributed to said sentence. You should also aim to quote the specific part of the retrieval context to justify your verdict, but keep it extremely concise and cut short the quote with an ellipsis if possible.

**
IMPORTANT: Please make sure to only return in JSON format, with the 'verdicts' key as a list of JSON objects, each with two keys: `verdict` and `reason`.

{{
    ""verdicts"": [
        {{
            ""verdict"": ""yes"",
            ""reason"": ""...""
        }},
        ...
    ]  
}}

Since you are going to generate a verdict for each sentence, the number of 'verdicts' SHOULD BE STRICTLY EQUAL to the number of sentences in `expected output`.
**

Expected Output:
{expectedOutput}

Retrieval Context:
{retrievalContextFormatted}

JSON:";
        }

    }
}
