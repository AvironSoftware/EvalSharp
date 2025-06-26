namespace EvalSharp.Synthesizer
{
    internal static class SynthesizerTemplate
    {
        public static string GenerateText2SqlInputs(List<string> context, int maxGoldensPerContext)
        {
            var formattedContext = context.ToFormattedList();

            return $$"""
Based on the given context, which is a SQL table schema, please generate a list of JSON objects with `input` keys.
The `input` can either be a question or a statement that can be addressed by the given schema.

**
IMPORTANT: Please make sure to only return in JSON format, with the 'data' key as a list of JSON objects.
You MUST TRY to generate {{maxGoldensPerContext}} data points, unless the `input` is getting repetitive.

Example context: [
    "Table: Customers",
    "Column: CustomerID, Type: INT, Description: Unique identifier for each customer",
    "Column: FirstName, Type: VARCHAR, Description: First name of the customer",
    "Column: LastName, Type: VARCHAR, Description: Last name of the customer",
    "Column: Email, Type: VARCHAR, Description: Email address of the customer",
    "Column: PhoneNumber, Type: VARCHAR, Description: Contact number of the customer",
    "Column: City, Type: VARCHAR, Description: City where the customer resides"
]
Example max goldens per context: 2
Example JSON:
{
    "data": [
        {
            "input": "Show me all the customers who live in New York.",
        },
        {
            "input": "List the first and last names of all customers.",
        }
    ]  
}

You should NOT incorporate any prior knowledge you have and take each context at face value.
You MUST include at least one statement as the input.
`input` MUST be a STRING.
You MUST TRY to generate {{maxGoldensPerContext}} data points, unless the generated `input` is getting repetitive.
**

Max Goldens Per Context:
{{maxGoldensPerContext}}

Context:
{{formattedContext}}

JSON:
""";
        }

        public static string GenerateText2SqlExpectedOutput(string input, List<string> context)
        {
            var formattedContext = context.ToFormattedList();

            return $$""""
Given the input, which may be a question or a statement addressable by the schema provided in the context,
generate a JSON object with a key 'sql'. This key should contain the corresponding SQL statement that accurately and efficiently responds to the input.

**
IMPORTANT: The output must be in JSON format, with the 'sql' key only.

Example Context: [
    "Table: Customers",
    "Column: CustomerID, Type: INT, Description: Unique identifier for each customer",
    "Column: FirstName, Type: VARCHAR, Description: First name of the customer",
    "Column: LastName, Type: VARCHAR, Description: Last name of the customer",
    "Column: Email, Type: VARCHAR, Description: Email address of the customer",
    "Column: PhoneNumber, Type: VARCHAR, Description: Contact number of the customer",
    "Column: City, Type: VARCHAR, Description: City where the customer resides"
]
Example Input: "Show me all the customers who live in New York.",
Example JSON: {
    "sql": "SELECT * FROM Customers WHERE City = 'New York';"
}

Context:
{{formattedContext}}

Input:
{{input}}

JSON:
"""";
        }

        public static string GenerateSyntheticExpectedOutput(string input, List<string> context, string? expectedOutputFormat)
        {
            var formattedContext = context.ToFormattedList();

            var importantSection = !string.IsNullOrWhiteSpace(expectedOutputFormat) ?
                $"IMPORTANT: Please ensure that the generated response strictly adheres to the following format: {expectedOutputFormat}, and make sure it is concise and straight to the point, using supporting information in context." :
                "IMPORTANT: Please make sure to generate a response that is concise and straight to the point, and uses supporting information in context.";

            return $$""""
Given the input, which may or may not be a question, generate a response using information presented in context.

**
{{importantSection}}
**

Context:
{{formattedContext}}

Input:
{{input}}

Generated Response:
"""";
        }

        public static string GenerateSyntheticInputs(List<string> context, int maxGoldensPerContext, string? scenario, string? task, string? inputFormat)
        {
            var formattedContext = context.ToFormattedList();

            var inputFormatSection = !string.IsNullOrWhiteSpace(inputFormat) ?
                $"'input' MUST strictly adhere to the following format: {inputFormat}." :
                "'input' MUST be a STRING.";

            var scenarioSection = !string.IsNullOrWhiteSpace(scenario) ?
                $"'input's MUST be relevant to this specific scenario: '{scenario}' (The scenario describes the circumstances under which the inputs are generated and the user’s intent in eliciting a response)." :
                string.Empty;

            var taskSection = !string.IsNullOrWhiteSpace(task) ?
                $"'input's MUST be framed in a way that evokes a response aligned with the following task: {task} (The task represents the goal or function the entity is expected to achieve when responding)." :
                string.Empty;

            return $$"""
I want you act as a copywriter. Based on the given context, which is list of strings, please generate a list of JSON objects with a 'input' key.
The `input` can either be a question or a statement that can be addressed by the given context.

**
IMPORTANT: Please make sure to only return in JSON format, with the 'data' key as a list of JSON objects.
You MUST TRY to generate {{maxGoldensPerContext}} data points, unless the `input` is getting reptitive.

Example context: ["Einstein won the Nobel Prize for his discovery of penicillin.", "Einstein won the Nobel Prize in 1968."]
Example max goldens per context: 2
Example JSON:
{
    "data": [
        {
            "input": "What was Einstein known for?"
        },
        {
            "input": "Einstein was a smart guy huh"
        }
    ]  
}


You should NOT incorporate any prior knowledge you have and take each context at face value.
You MUST include at least one statement as the input.
{{inputFormatSection}}
{{scenarioSection}}
{{taskSection}}
You MUST TRY to generate {{maxGoldensPerContext}} data points, unless the generated `input` is getting reptitive.
**

Max Goldens Per Context:
{{maxGoldensPerContext}}

Context:
{{context}}

JSON:
""";
        }

        public static string RewriteEvolvedInput(string evolvedInput, string? scenario, string? task, string? inputFormat)
        {
            var scenarioSection = !string.IsNullOrWhiteSpace(scenario) ?
                $"Scenario: \"{scenario}\"" :
                string.Empty;

            var taskSection = !string.IsNullOrWhiteSpace(task) ?
                $"Task: \"{task}\"" :
                string.Empty;

            var inputFormatSection = !string.IsNullOrWhiteSpace(task) ?
                $"Input Format: \"{inputFormat}\"" :
                string.Empty;

            return $$"""
Given the evolved input, which may be a question or a statement, generate a JSON object with a key 'input'. This key should contain a statement or question that fits any provided scenario, aligns with the task's purpose, and matches the required input format (if specified).

**
IMPORTANT: Try to change the evolved input as little as possible. However, if the evolved input does not align with the provided scenario, task, or input format, it must ultimately be adjusted to fit these requirements. The output must be in JSON format, with the 'input' key only. If necessary, the evolved input should be rewritten to ensure it conforms to the scenario, task, and input format.

Example Evolved Input: "Is it okay to joke about someone losing their job in front of their coworkers if I'm just trying to lighten the mood?"
{{{(!string.IsNullOrWhiteSpace(scenario) ? $"Example Scenario: \"{scenario}\"" : "")}}}
{{{(!string.IsNullOrWhiteSpace(task) ? $"Example Task: \"{task}\"" : "")}}}
{{{(!string.IsNullOrWhiteSpace(inputFormat) ? $"Example Input Format: \"{inputFormat}\"" : "")}}}
Example JSON: {
    "input": "How can I joke about someone losing their job without making the situation worse? Is it possible to use humor here without hurting anyone's feelings?"
}

Evolved Input:
{{evolvedInput}}

{{scenarioSection}}
{{taskSection}}
{{inputFormatSection}}

JSON:
""";
        }

        public static string RewriteSyntheticInputs(List<string> context, string originalQuery, string feedback)
        {
            var formattedContext = context.ToFormattedList();

            return $$"""
I want you to act as a query rewriter. Based on the provided context, original query, and feedback, generate a rewritten query that improves its clarity and answerability based on the feedback provided.

**
IMPORTANT: Please make sure to only return in JSON format, with the 'rewritten_input' key.

Example context: "The Golden Gate Bridge, located in San Francisco, was completed in 1937 and is known for its Art Deco design. It connects the city of San Francisco to Marin County and spans the Golden Gate Strait."
Example query: "When was the bridge completed?"
Example feedback: "The question asks about the completion of 'the bridge' but does not specify which bridge it refers to. There are many famous bridges, and without specifying the name, the question is too vague. To improve clarity, include the bridge's name."
Example JSON:
{
    "rewritten_input": "When was the Golden Gate Bridge completed?"
}

Example context: "The paper 'Advancements in Quantum Computing' by Dr. Alice Thompson discusses breakthroughs in quantum algorithms and was published in 2022. It explores the potential applications of quantum computing in cryptography and drug discovery."
Example query: "What applications of quantum computing are discussed in the paper?"
Example feedback: "The query is asking about applications of quantum computing but doesn't specify which paper is being referenced. Since many papers may discuss quantum computing, it would help to specify the title or author of the paper to improve clarity."
Example JSON:
{
    "rewritten_input": "What applications of quantum computing are discussed in the paper 'Advancements in Quantum Computing' by Dr. Alice Thompson?"
}

You should NOT incorporate any prior knowledge and should base the rewritten query only on the context and feedback provided.
The `rewritten_input` MUST be a STRING.
**

Context:
{{formattedContext}}

Query:
{{originalQuery}}

Feedback:
{{feedback}}

JSON:
""";
        }

    }
}
