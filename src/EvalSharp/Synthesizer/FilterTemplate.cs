using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvalSharp.Synthesizer
{
    internal static class FilterTemplate
    {
        public static string EvaluateSyntheticInputs(string query)
        {
            return $$"""
Evaluate the provided synthetic query (which may be a question, task, or instruction) for clarity and answerability, assuming sufficient domain knowledge. Use the following criteria to guide your assessment:

1. **Self-Containment**: Can the query be understood and completed without needing additional context or external references not provided within the query itself? It should be self-sufficient, meaning it doesn't depend on specific documents, tables, or prior knowledge not included in the query.
2. **Clear Objective**: Does the query clearly convey its intent? It should specify what information, action, or response is being requested, allowing for a direct and appropriate answer or execution without ambiguity.

Based on these criteria, assign a score between 0 and 1, where:
- "1" means the query is clear, self-contained, and answerable.
- "0" means the query is vague, relies on external references, or is unclear in its intent.
- Scores between 0 and 1 indicate partial clarity or answerability, where the query meets some but not all of the criteria.

**
IMPORTANT: Please make sure to only return in JSON format, with the 'feedback' and 'score' keys.

Example query: "What technological innovations have changed communication over the last 20 years?"
Example JSON:
{
    "feedback": "The query is somewhat vague as it asks about 'technological innovations' without specifying particular areas of communication (e.g., social media, messaging apps). It could be improved by narrowing the focus to a specific type of innovation or timeframe.",
    "score": 0.5
}

Example query: "Explain the impact of renewable energy policies in Germany on local economies in 2021."
Example JSON:
{
    "feedback": "This query clearly specifies the focus (renewable energy policies), the region (Germany), and the timeframe (2021). It is self-contained and answerable without needing additional context, making it clear and effective.",
    "score": 1.0
}

Example query: "What are the main criticisms of the current education system in the United States?"
Example JSON:
{
    "feedback": "The question is broad and lacks specificity, as 'main criticisms' could refer to various aspects (e.g., funding, curriculum, access). To improve clarity, it could specify which aspect of the education system is being critiqued.",
    "score": 0.4
}

Example query: "Discuss the role of AI in healthcare, particularly in diagnostics, as noted in the last report."
Example JSON:
{
    "feedback": "This question refers to 'the last report' without providing context or details, making it unclear and dependent on external information. It would be clearer if it provided some background on the report or defined what aspects of AI in diagnostics to address.",
    "score": 0.3
}

The `feedback` MUST be a STRING and `score` must be a float from 0 to 1.
**

Query:
{{query}}

JSON:
""";
        }

        public static string EvaluateContext(List<string> context)
        {
            var formattedContext = context.ToFormattedList();
            return $$"""
Given a context, complete the following task and return the result in VALID JSON format: Evaluate the supplied context and assign a numerical score between 0 (Low) and 1 (High) for each of the following criteria in your JSON response:

- **clarity**: Assess how clear and comprehensible the information is. A score of 1 indicates that the context is straightforward and easily understandable, while a score of 0 reflects vagueness or confusion in the information presented.
- **depth**: Evaluate the extent of detailed analysis and the presence of original insights within the context. A high score (1) suggests a thorough and thought-provoking examination, while a low score (0) indicates a shallow overview of the subject.
- **structure**: Review how well the content is organized and whether it follows a logical progression. A score of 1 is given to contexts that are coherently structured and flow well, whereas a score of 0 is for those that lack organization or clarity in their progression.
- **relevance**: Analyze the importance of the content in relation to the main topic, awarding a score of 1 for contexts that stay focused on the subject without unnecessary diversions, and a score of 0 for those that include unrelated or irrelevant information.

**
IMPORTANT: Please make sure to only return in JSON format, with the 'clarity', 'depth', 'structure', abd 'relevance' keys.

Example context: "Artificial intelligence is rapidly changing various sectors, from healthcare to finance, by enhancing efficiency and enabling better decision-making."
Example JSON:
{
    "clarity": 1,
    "depth": 0.8,
    "structure": 0.9,
    "relevance": 1
}

Example context: "Cats are great pets. They like to sleep and play."
Example JSON:
{
    "clarity": 0.5,
    "depth": 0.3,
    "structure": 0.4,
    "relevance": 0.5
}

Example context: "Artificial intelligence is rapidly changing various sectors, from healthcare to finance, by enhancing efficiency and enabling better decision-making."
Example JSON:
{
    "clarity": 1,
    "depth": 0.9,
    "structure": 1,
    "relevance": 1
}

Example context: "Artificial intelligence is rapidly changing various sectors, from healthcare to finance, by enhancing efficiency and enabling better decision-making."
Example JSON:
{
    "clarity": 0.4,
    "depth": 0,
    "structure": 0.3,
    "relevance": 0.2
}

Example context: "The impact of globalization on local cultures is complex, with both positive and negative effects. It can lead to cultural exchange but also to the erosion of local traditions."
Example JSON:
{
    "clarity": 0.9,
    "depth": 0.8,
    "structure": 0.9,
    "relevance": 1
}


`clarity`, `depth`, `structure`, and `relevance` MUST be floats from 0 to 1.
Make sure your JSON response is valid and properly formatted.
**

context:
{{formattedContext}}

JSON:
""";
        }
    }
}
