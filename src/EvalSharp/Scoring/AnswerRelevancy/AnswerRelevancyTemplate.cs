﻿namespace EvalSharp.Scoring.AnswerRelevancy
{
    internal static class AnswerRelevancyTemplate
    {
        //Prompt adapted from deepeval, licensed under Apache 2.0
        //DeepEval Repo: https://github.com/confident-ai/deepeval
        public static string GenerateStatements(string actualOutput)
        {
            return $$"""
Given the text, breakdown and generate a list of statements presented. Ambiguous statements and single words can also be considered as statements.

Example:
Example text: 
Our new laptop model features a high-resolution Retina display for crystal-clear visuals. It also includes a fast-charging battery, giving you up to 12 hours of usage on a single charge. For security, we've added fingerprint authentication and an encrypted SSD. Plus, every purchase comes with a one-year warranty and 24/7 customer support.

{
    "statements": [
        "The new laptop model has a high-resolution Retina display.",
        "It includes a fast-charging battery with up to 12 hours of usage.",
        "Security features include fingerprint authentication and an encrypted SSD.",
        "Every purchase comes with a one-year warranty.",
        "24/7 customer support is included."
    ]
}
===== END OF EXAMPLE ======
        
**
IMPORTANT: Please make sure to only return in JSON format, with the "statements" key mapping to a list of strings. No words or explanation is needed.
**

Text:
{{actualOutput}}

JSON:
""";
        }

        //Prompt adapted from deepeval, licensed under Apache 2.0
        //DeepEval Repo: https://github.com/confident-ai/deepeval
        public static string GenerateVerdicts(string input, string statementsJson)
        {
            return $$"""
For the provided list of statements, determine whether each statement is relevant to address the input.
Please generate a list of JSON with two keys: `verdict` and `reason`.
The 'verdict' key should STRICTLY be either a 'yes', 'idk' or 'no'. Answer 'yes' if the statement is relevant to addressing the original input, 'no' if the statement is irrelevant, and 'idk' if it is ambiguous (e.g., not directly relevant but could be used as a supporting point to address the input).
The 'reason' is the reason for the verdict.
Provide a 'reason' ONLY if the answer is 'no'. 
The provided statements are statements made in the actual output.

**
IMPORTANT: Please make sure to only return in JSON format, with the 'verdicts' key mapping to a list of JSON objects.
Example input: 
What features does the new laptop have?

Example statements: 
[
    "The new laptop model has a high-resolution Retina display.",
    "It includes a fast-charging battery with up to 12 hours of usage.",
    "Security features include fingerprint authentication and an encrypted SSD.",
    "Every purchase comes with a one-year warranty.",
    "24/7 customer support is included.",
    "Pineapples taste great on pizza."
]

Example JSON:
{
    "verdicts": [
        {
            "verdict": "yes"
        },
        {
            "verdict": "yes"
        },
        {
            "verdict": "yes"
        },
        {
            "verdict": "no",
            "reason": "A one-year warranty is a purchase benefit, not a feature of the laptop itself."
        },
        {
            "verdict": "no",
            "reason": "Customer support is a service, not a feature of the laptop."
        },
        {
            "verdict": "no",
            "reason": "The statement about pineapples on pizza is completely irrelevant to the input, which asks about laptop features."
        }
    ]
}

Since you are going to generate a verdict for each statement, the number of 'verdicts' SHOULD BE STRICTLY EQUAL to the number of `statements`.

Input:
{{input}}

Statements:
{{statementsJson}}

JSON:
""";
        }

        //Prompt adapted from deepeval, licensed under Apache 2.0
        //DeepEval Repo: https://github.com/confident-ai/deepeval
        public static string GenerateReason(List<string> irrelevantReasons, string input, string score)
        {
            string reasonsText = string.Join(", ", irrelevantReasons);
            return $$"""
Given the answer relevancy score, the list of reasons of irrelevant statements made in the actual output, and the input, provide a CONCISE reason for the score. Explain why it is not higher, but also why it is at its current score.
The irrelevant statements represent things in the actual output that are irrelevant to addressing whatever is asked/talked about in the input.
If there is nothing irrelevant, just say something positive with an upbeat encouraging tone (but don't overdo it otherwise it gets annoying).

**
IMPORTANT: Please make sure to only return in JSON format, with the 'reason' key providing the reason.
Example JSON:
{
    "reason": "The score is <answer_relevancy_score> because <your_reason>."
}
**

Answer Relevancy Score:
{{score}}

Reasons why the score can't be higher based on irrelevant statements in the actual output:
{{reasonsText}}

Input:
{{input}}

JSON:
""";
        }
    }
}
