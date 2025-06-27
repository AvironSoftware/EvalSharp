# EvalSharp 🧠

**LLM Evaluation for .NET Developers — No Python Required**

EvalSharp brings the power of reliable LLM evaluation directly to your C# projects. Inspired by [DeepEval](https://github.com/confident-ai/deepeval), but designed for the .NET ecosystem, EvalSharp lets you measure LLM outputs with confidence using familiar C# tools and patterns.

---

## 🔥 Key Features

- **Fully Native .NET API** — Designed for C# developers; no Python dependencies.
- **Out-of-the-box Metrics** — Evaluate Answer Relevancy, Contextual Recall, GEval, and more.
- **LLM-as-a-Judge** — Supports OpenAI, Azure OpenAI, and custom chat clients.
- **Easy Customization** — Build your own metrics tailored to your use case.

---

## ⚡ Quick Start

1. **Install EvalSharp**

```bash
dotnet add package EvalSharp
```

2. **Create an Evaluator**

```csharp
var cases = new[]
{
    new TType
    {
        UserInput    = "Please summarize the article on climate change impacts.",
        LLMOutput   = "The article talks about how technology is advancing rapidly.",
    }
};

var evaluator = Evaluator.FromData(
    ChatClient.GetInstance(),
    cases,
    c => new MetricEvaluationContext
    {
        InitialInput    = c.UserInput,
        ActualOutput    = c.LLMOutput
    }
);
```

3. **Add Metrics**

```csharp
evaluator.AddAnswerRelevancy(includeReason: true);
```

4. **Evaluate Your LLM Output**

```csharp
var result = await evaluator.RunAsync();
```

---

## ✅ Unit Testing with EvalTest.AssertAsync

In addition to evaluating datasets with the `Evaluator`, EvalSharp makes it easy to include LLM evaluation in your unit tests. The `EvalTest.AssertAsync` method allows you to assert results for a single test with one or more metrics.

### Example: Asserting Multiple Metrics in a Unit Test

```csharp
using EvalSharp.Models;
using EvalSharp.Scoring;
using Xunit.Abstractions;

public class MyEvalTests
{
    public MyEvalTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task SingleTest_MultipleMetrics()
    {
        var testData = new EvaluatorTestData
        {
            InitialInput = "Summarize the meeting.",
            ActualOutput = "The meeting summary is provided below...",
        };

        var rel_config = new AnswerRelevancyMetricConfiguration
        {
            IncludeReason = true,
            Threshold = 0.9
        };

        var geval_config = new GEvalMetricConfiguration
        {
            Threshold = 0.5,
            Criteria = "Does the output correctly explain concepts, events, or processes based on the input prompt?"
        };

        var metrics = new List<Metric>
        {
            new AnswerRelevancyMetric(ChatClient.GetInstance(), rel_config),
            new GEvalMetric(ChatClient.GetInstance(), geval_config)
        };

        await EvalTest.AssertAsync(testData, metrics, _testOutputHelper.WriteLine);
    }
}
```

✅ Supports multiple metrics in a single call  
✅ Output results to your preferred sink (e.g., Console, Xunit test output)  
✅ Ideal for lightweight, targeted LLM evaluation in CI/CD pipelines

---

## 🛠 Metrics Included

✅ **[Answer Relevancy](/src/EvalSharp/Scoring/AnswerRelevancy/README.md)** — Is the LLM's response relevant to the input?  
✅ **[Bias](/src/EvalSharp/Scoring/Bias/README.md)** — Checks for content biases.  
✅ **[Contextual Precision](/src/EvalSharp/Scoring/ContextualPrecision/README.md)** — Measures if output precisely reflects provided context.  
✅ **[Contextual Recall](/src/EvalSharp/Scoring/ContextualRecall/README.md)** — Assesses how much of the relevant context was included in the output.  
✅ **[Faithfulness](/src/EvalSharp/Scoring/Faithfulness/README.md)** — Evaluates factual correctness and grounding of the output.  
✅ **[GEval](/src/EvalSharp/Scoring/GEval/README.md)** — Enforces structure, logical flow, and coverage expectations.  
✅ **[Hallucination](/src/EvalSharp/Scoring/Hallucination/README.md)** — Detects whether the LLM generated unsupported or fabricated content.  
✅ **[Match](/src/EvalSharp/Scoring/Match/README.md)** — Compares expected and actual output for equality or similarity.  
✅ **[Prompt Alignment](/src/EvalSharp/Scoring/PromptAlignment/README.md)** — Ensures output follows the intent and structure of the prompt.  
✅ **[Summarization](/src/EvalSharp/Scoring/Summarization/README.md)** — Scores the quality and accuracy of generated summaries.  
✅ **[Task Completion](/src/EvalSharp/Scoring/TaskCompletion/README.md)** — Measures whether the LLM's output fulfills the requested task.  
✅ **[Tool Correctness](/src/EvalSharp/Scoring/ToolCorrectness/README.md)** — Evaluates whether tool-augmented LLM responses are correct.

---

## 💡 Why EvalSharp?

- No need to switch to Python for LLM evaluation
- Designed with .NET 8 in mind
- Beautiful, easy to digest outputs
- Ideal for both RAG and general LLM application testing
- Easy to extend with your own custom metrics

---

## 🚧 Future Roadmap

We're just getting started. Here's what's coming soon to EvalSharp:

- [ ] Additional Built-in Metrics (e.g., DAG, RAGAS, Contextual Relevancy, Toxicity, JSON Correctness)
- [ ] Data Synthesizer
- [ ] Token Usage/ Cost Calculation
- [ ] Additional Scorers (Rouge, Truth Identification, etc.)
- [ ] Expanded Examples and Tutorials
- [ ] Conversational Metrics

---

## 📄 License

This project is licensed under the MIT License. See the [LICENSE](./LICENSE) file for details.

Portions of this project include content adapted from deepeval, which is licensed under the Apache License 2.0. See the [NOTICE](./NOTICE) file for attribution.

---

## Acknowledgements

Aviron Software would like to give a special thanks to the team at [DeepEval](https://github.com/confident-ai/deepeval). Their original metrics and prompts are the catalysts for this project.
