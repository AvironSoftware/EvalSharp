using CsvHelper.Configuration;
using Microsoft.Extensions.AI;
using System.Collections.Concurrent;
using System.Text.Json;
using EvalSharp.Helpers;
using EvalSharp.Models.Enums;
using EvalSharp.Scoring;
using EvalSharp.Exceptions;

namespace EvalSharp;

/// <summary>
/// Used to create Evaluator instances that can run metrics against a dataset.
/// </summary>
public static class Evaluator
{
    /// <summary>
    /// Initializes an evaluator that will run any added metrics against the provided test data.
    /// </summary>
    /// <param name="chatClient">LLM chat client</param>
    /// <param name="data">Dataset to run metrics against</param>
    public static Evaluator<EvaluatorTestData> FromData(IChatClient chatClient, IEnumerable<EvaluatorTestData> data)
    {
        return new Evaluator<EvaluatorTestData>(chatClient, data, c => c);
    }
    
    /// <summary>
    /// Initializes an evaluator that will run any added metrics against the provided data.
    /// </summary>
    /// <param name="chatClient">LLM chat client</param>
    /// <param name="data">Dataset to run metrics against</param>
    /// <param name="map">Mapping from the provided dataset <typeparamref name="T"/> to EvaluatorTestData</param>
    public static Evaluator<T> FromData<T>(IChatClient chatClient, IEnumerable<T> data, Func<T, EvaluatorTestData> map)
    {
        return new Evaluator<T>(chatClient, data, map);
    }

    /// <summary>
    /// Initializes an evaluator that will run any added metrics against the provided data. 
    /// Instantiates the data from a valid JSON string.
    /// </summary>
    /// <param name="chatClient">LLM chat client</param>
    /// <param name="json">Valid JSON string</param>
    /// <param name="map">Mapping from the provided dataset<typeparamref name="T"/> to EvaluatorTestData</param>
    /// <param name="jsonOptions">optional JSON options</param>
    public static Evaluator<T> FromJson<T>(IChatClient chatClient, string json, Func<T, EvaluatorTestData> map, JsonSerializerOptions? jsonOptions = null)
    {
        var data = JsonDataLoader.LoadJson<T>(json, jsonOptions);
        return new Evaluator<T>(chatClient, data, map);
    }

    /// <summary>
    /// Initializes an evaluator that will run any added metrics against the provided data. 
    /// Instantiates the data from a list of valid JSON lines.
    /// </summary>
    /// <param name="chatClient">LLM chat client</param>
    /// <param name="jsonLine">List of valid JSON lines</param>
    /// <param name="map">Mapping from the provided dataset<typeparamref name="T"/> to EvaluatorTestData</param>
    /// <param name="jsonOptions">optional JSON options</param>
    public static Evaluator<T> FromJsonLines<T>(IChatClient chatClient, IEnumerable<string> jsonLine, Func<T, EvaluatorTestData> map, JsonSerializerOptions? jsonOptions = null)
    {
        var data = JsonDataLoader.LoadJsonLines<T>(jsonLine, jsonOptions);
        return new Evaluator<T>(chatClient, data, map);
    }

    /// <summary>
    /// Initializes an evaluator that will run any added metrics against the provided data. 
    /// Instantiates the data from a JSON file.
    /// </summary>
    /// <param name="chatClient">LLM chat client</param>
    /// <param name="filePath">File path to a JSON file</param>
    /// <param name="map">Mapping from the provided dataset<typeparamref name="T"/> to EvaluatorTestData</param>
    /// <param name="jsonOptions">optional JSON options</param>
    public static Evaluator<T> FromJsonFile<T>(IChatClient chatClient, string filePath, Func<T, EvaluatorTestData> map, JsonSerializerOptions? jsonOptions = null)
    {
        var data = JsonDataLoader.LoadJsonFile<T>(filePath, jsonOptions);
        return new Evaluator<T>(chatClient, data, map);
    }

    /// <summary>
    /// Initializes an evaluator that will run any added metrics against the provided data. 
    /// Instantiates the data from a CSV string.
    /// </summary>
    /// <param name="chatClient">LLM chat client</param>
    /// <param name="csvText">Valid CSV string</param>
    /// <param name="map">Mapping from the provided dataset<typeparamref name="T"/> to EvaluatorTestData</param>
    /// <param name="config">optional CSV configurations</param>
    public static Evaluator<T> FromCsv<T>(IChatClient chatClient, string csvText, Func<T, EvaluatorTestData> map, CsvConfiguration? config = null)
    {
        var data = CsvDataLoader.LoadCsv<T>(csvText, config);
        return new Evaluator<T>(chatClient, data, map);
    }

    /// <summary>
    /// Initializes an evaluator that will run any added metrics against the provided data. 
    /// Instantiates the data from a CSV file.
    /// </summary>
    /// <param name="chatClient">LLM chat client</param>
    /// <param name="filePath">File path to CSV file</param>
    /// <param name="map">Mapping from the provided dataset<typeparamref name="T"/> to EvaluatorTestData</param>
    /// <param name="config">optional CSV configurations</param>
    public static Evaluator<T> FromCsvFile<T>(IChatClient chatClient, string filePath, Func<T, EvaluatorTestData> map, CsvConfiguration? config = null)
    {
        var data = CsvDataLoader.LoadCsv<T>(filePath, config);
        return new Evaluator<T>(chatClient, data, map);
    }
}

/// <summary>
/// Evaluator is used to run metrics against a dataset.
/// </summary>
/// <typeparam name="T">The type of the data being tested.</typeparam>
public class Evaluator<T>
{
    /// <summary>
    /// Configuration for all metrics for this evaluator. Settings specified here will not override metric-specific configurations.
    /// </summary>
    public EvaluatorConfiguration Configuration { get; }
    private IChatClient? ChatClient { get; }
    private IList<EvaluatorTestData> Data { get; }
    
    private readonly List<Metric> _metrics = new();
    
    /// <summary>
    /// The metrics that will be run against the provided data.
    /// </summary>
    public IReadOnlyList<Metric> Metrics =>_metrics.AsReadOnly();

    /// <summary>
    /// Initializes an evaluator that will run any added metrics against the provided data. 
    /// Use this if you need an LLM-as-a-judge for your metrics.
    /// If you do NOT need LLM-as-a-judge metrics, use the other constructor that does not require an IChatClient.
    /// </summary>
    /// <param name="chatClient">LLM chat client</param>
    /// <param name="data">Dataset to run metrics against</param>
    /// <param name="map">Mapping from the provided dataset <typeparamref name="T"/> to EvaluatorTestData</param>
    public Evaluator(IChatClient chatClient, IEnumerable<T> data, Func<T, EvaluatorTestData> map)
    {
        Configuration = new();
        ChatClient = chatClient;
        Data = [..data.Select(map)];
    }

    /// <summary>
    /// Adds the metric to the evaluator. 
    /// This can be a custom metric that enables you to evaluate your tailored evaluation criteria.
    /// </summary>
    /// <param name="metric">Metric that inherits the Metric class</param>
    public void AddMetric(Metric metric)
    {
        _metrics.Add(metric);
    }

    /// <summary>
    /// Creates the Answer Relevancy metric and adds it to the evaluator. 
    /// Answer Relevancy is a metric that uses an LLM-as-a-judge to evaluate the relevancy of a model's answers based on the input.
    /// </summary>
    /// <param name="config">Configuration used to tune strict mode, threshold, and more.</param>
    public void AddAnswerRelevancy(AnswerRelevancyMetricConfiguration config)
    {
        CheckThatChatClientIsSet();
        EnsureFieldsAreSetForAnswerRelevancy();
        config = CreateConfig(config);
        var metric = new AnswerRelevancyMetric(ChatClient!, config);
        AddMetric(metric);
    }

    /// <summary>
    /// Creates the Answer Relevancy metric and adds it to the evaluator. 
    /// Answer Relevancy is a metric that uses an LLM-as-a-judge to evaluate the relevancy of a model's answers based on the input.
    /// </summary>
    /// <param name="includeReason">Includes a reason that the LLM made its determination. Note that this will call an LLM a second time.</param>
    /// <param name="strictMode">Enforces a binary metric score. Either perfect score of 1 or a failure of anything less. Defaults to false.</param>
    /// <param name="threshold">Threshold for what is considered a pass or failure. Defaults to 0.5.</param>
    public void AddAnswerRelevancy(bool? includeReason = null, bool? strictMode = null, double? threshold = null)
    {
        CheckThatChatClientIsSet();
        EnsureFieldsAreSetForAnswerRelevancy();
        var config = CreateConfig<AnswerRelevancyMetricConfiguration>(includeReason, strictMode, threshold);
        var metric = new AnswerRelevancyMetric(ChatClient!, config);
        AddMetric(metric);
    }

    /// <summary>
    /// Creates the Bias metric and adds it to the evaluator. 
    /// Bias metric is a metric that uses an LLM-as-a-judge to evaluate the output of a model to determine if there is any bias.
    /// Bias is considered anything that's not fact or claim. IncludeReason parameter is helpful with identifying the LLMs reasoning.
    /// </summary>
    /// <param name="config">Configuration used to tune strict mode, threshold, and more.</param>
    public void AddBias(BiasMetricConfiguration config)
    {
        CheckThatChatClientIsSet();
        EnsureFieldsAreSetForBias();
        config = CreateConfig(config);
        var metric = new BiasMetric(ChatClient!, config);
        AddMetric(metric);
    }

    /// <summary>
    /// Creates the Bias metric and adds it to the evaluator. 
    /// Bias metric is a metric that uses an LLM-as-a-judge to evaluate the output of a model to determine if there is any bias.
    /// Bias is considered anything that's not fact or claim. IncludeReason parameter is helpful with identifying the LLMs reasoning.
    /// </summary>
    /// <param name="includeReason">Includes a reason that the LLM made its determination. Note that this will call an LLM a second time.</param>
    /// <param name="strictMode">Enforces a binary metric score. Either perfect score of 1 or a failure of anything less. Defaults to false.</param>
    /// <param name="threshold">Threshold for what is considered a pass or failure. Defaults to 0.5.</param>
    public void AddBias(bool? includeReason = null, bool? strictMode = null, double? threshold = null)
    {
        CheckThatChatClientIsSet();
        EnsureFieldsAreSetForBias();
        var config = CreateConfig<BiasMetricConfiguration>(includeReason, strictMode, threshold);
        var metric = new BiasMetric(ChatClient!, config);
        AddMetric(metric);
    }

    /// <summary>
    /// Creates the Contextual Precision metric and adds it to the evaluator. 
    /// Contextual Precision metric is a metric that uses an LLM-as-a-judge to evaluate that the nodes in your retrieval context are relevant to the input.
    /// </summary>
    /// <param name="config">Configuration used to tune strict mode, threshold, and more.</param>
    public void AddContextualPrecision(ContextualPrecisionMetricConfiguration config)
    {
        CheckThatChatClientIsSet();
        EnsureFieldsAreSetForContextualPrecision();
        config = CreateConfig(config);
        var metric = new ContextualPrecisionMetric(ChatClient!, config);
        AddMetric(metric);
    }

    /// <summary>
    /// Creates the Contextual Precision metric and adds it to the evaluator. 
    /// Contextual Precision metric is a metric that uses an LLM-as-a-judge to evaluate that the nodes in your retrieval context are relevant to the input.
    /// </summary>
    /// <param name="includeReason">Includes a reason that the LLM made its determination. Note that this will call an LLM a second time.</param>
    /// <param name="strictMode">Enforces a binary metric score. Either perfect score of 1 or a failure of anything less. Defaults to false.</param>
    /// <param name="threshold">Threshold for what is considered a pass or failure. Defaults to 0.5.</param>
    public void AddContextualPrecision(bool? includeReason = null, bool? strictMode = null, double? threshold = null)
    {
        CheckThatChatClientIsSet();
        EnsureFieldsAreSetForContextualPrecision();
        var config = CreateConfig<ContextualPrecisionMetricConfiguration>(includeReason, strictMode, threshold);
        var metric = new ContextualPrecisionMetric(ChatClient!, config);
        AddMetric(metric);
    }

    /// <summary>
    /// Creates the Contextual Recall metric and adds it to the evaluator. 
    /// Contextual Recall metric is a metric that uses an LLM-as-a-judge to measure what percentage of the key facts in your expected output are actually found in the documents your system retrieved.
    /// It works by splitting the expected output into individual statements to see which of those statements appear in your retrieved context.
    /// A higher score means your retrieval pipeline is doing a better job of finding all the relevant information.
    /// </summary>
    /// <param name="config">Configuration used to tune strict mode, threshold, and more.</param>
    public void AddContextualRecall(ContextualRecallMetricConfiguration config)
    {
        CheckThatChatClientIsSet();
        EnsureFieldsAreSetForContextualRecall();
        config = CreateConfig(config);
        var metric = new ContextualRecallMetric(ChatClient!, config);
        AddMetric(metric);
    }

    /// <summary>
    /// Creates the Contextual Recall metric and adds it to the evaluator. 
    /// Contextual Recall metric is a metric that uses an LLM-as-a-judge to measure what percentage of the key facts in your expected output are actually found in the documents your system retrieved.
    /// It works by splitting the expected output into individual statements to see which of those statements appear in your retrieved context.
    /// A higher score means your retrieval pipeline is doing a better job of finding all the relevant information.
    /// </summary>
    /// <param name="strictMode">Enforces a binary metric score. Either perfect score of 1 or a failure of anything less. Defaults to false.</param>
    /// <param name="threshold">Threshold for what is considered a pass or failure. Defaults to 0.5.</param>
    public void AddContextualRecall(bool? strictMode = null, double? threshold = null)
    {
        CheckThatChatClientIsSet();
        EnsureFieldsAreSetForContextualRecall();
        var config = CreateConfig<ContextualRecallMetricConfiguration>(null, strictMode, threshold);
        var metric = new ContextualRecallMetric(ChatClient!, config);
        AddMetric(metric);
    }

    /// <summary>
    /// Creates the Faithfulness metric and adds it to the evaluator. 
    /// Faithfulness metric is a metric that uses an LLM-as-a-judge to factually evaluate the actual output is aligned with the retrieval context.
    /// This metric will extract claims from the actual output and check if there are any contradictions to those claims in the retrieval context.
    /// You can specify how many truths are in the actual output by providing a truthsExtractionLimit or simply let the LLM decide by leaving it null.
    /// </summary>
    /// <param name="config">Configuration used to tune strict mode, threshold, and more.</param>
    public void AddFaithfulness(FaithfulnessMetricConfiguration config)
    {
        CheckThatChatClientIsSet();
        EnsureFieldsAreSetForFaithfulness();
        config = CreateConfig(config);
        var metric = new FaithfulnessMetric(ChatClient!, config);
        AddMetric(metric);
    }


    /// <summary>
    /// Creates the Faithfulness metric and adds it to the evaluator. 
    /// Faithfulness metric is a metric that uses an LLM-as-a-judge to factually evaluate the actual output is aligned with the retrieval context.
    /// This metric will extract claims from the actual output and check if there are any contradictions to those claims in the retrieval context.
    /// You can specify how many truths are in the actual output by providing a truthsExtractionLimit or simply let the LLM decide by leaving it null.
    /// </summary>
    /// <param name="truthsExtractionLimit">Number of truths to extract from the actual output. Leaving this NULL will let the LLM decide</param>
    /// <param name="includeReason">Includes a reason that the LLM made its determination. Note that this will call an LLM a second time.</param>
    /// <param name="strictMode">Enforces a binary metric score. Either perfect score of 1 or a failure of anything less. Defaults to false.</param>
    /// <param name="threshold">Threshold for what is considered a pass or failure. Defaults to 0.5.</param>
    public void AddFaithfulness(int? truthsExtractionLimit = null, bool? includeReason = null, bool? strictMode = null, double? threshold = null)
    {
        CheckThatChatClientIsSet();
        EnsureFieldsAreSetForFaithfulness();
        var config = CreateConfig<FaithfulnessMetricConfiguration>(includeReason, strictMode, threshold) with
        {
            TruthsExtractionLimit = truthsExtractionLimit
        };
        var metric = new FaithfulnessMetric(ChatClient!, config);
        AddMetric(metric);
    }


    /// <summary>
    /// Creates the GEval metric and adds it to the evaluator. 
    /// GEval is a metric that uses an LLM-as-a-judge to evaluate the quality of a model's output based on the specified criteria.
    /// The criteria that you specify will be given to an LLM and turned into a set of evaluation steps that the LLM will use to evaluate the model's output.
    /// If you want to avoid the extra LLM call and/or want to tightly control the evaluation steps, you can use the overload that takes a list of evaluation steps.
    /// </summary>
    /// <param name="criteria">Description used to generate the evaluation steps.</param>
    /// <param name="includeReason">Includes a reason that the LLM made its determination. Note that this will call an LLM a second time.</param>
    /// <param name="strictMode">Enforces a binary metric score. Either perfect score of 1 or a failure of anything less. Defaults to false.</param>
    /// <param name="threshold">Threshold for what is considered a pass or failure. Defaults to 0.5.</param>
    public void AddGEval(string criteria, bool? includeReason = null, bool? strictMode = null, double? threshold = null)
    {
        CheckThatChatClientIsSet();
        EnsureFieldsAreSetForGEval();
        var config = CreateConfig<GEvalMetricConfiguration>(includeReason, strictMode, threshold) with
        {
            Criteria = criteria
        };
        var metric = new GEvalMetric(ChatClient!, config);
        AddMetric(metric);
    }


    /// <summary>
    /// Creates the GEval metric and adds it to the evaluator. 
    /// GEval is a metric that uses an LLM-as-a-judge to evaluate the quality of a model's output based on the evaluation steps.
    /// The evaluation steps are steps that the LLM will use to evaluate the model's output.
    /// If you're unsure of the evaluation steps and are less concerned with the reliability of the metric score, you can use the overload that takes a single text of the criteria.
    /// </summary>
    /// <param name="evaluationSteps">List of strings that represent each step the LLM should use to evaluate</param>
    /// <param name="includeReason">Includes a reason that the LLM made its determination. Note that this will call an LLM a second time.</param>
    /// <param name="strictMode">Enforces a binary metric score. Either perfect score of 1 or a failure of anything less. Defaults to false.</param>
    /// <param name="threshold">Threshold for what is considered a pass or failure. Defaults to 0.5.</param>
    public void AddGEval(IEnumerable<string> evaluationSteps, bool? includeReason = null, bool? strictMode = null, double? threshold = null)
    {
        CheckThatChatClientIsSet();
        EnsureFieldsAreSetForGEval();
        var config = CreateConfig<GEvalMetricConfiguration>(includeReason, strictMode, threshold) with
        {
            EvaluationSteps = [.. evaluationSteps]
        };
        var metric = new GEvalMetric(ChatClient!, config);
        AddMetric(metric);
    }

    /// <summary>
    /// Creates the GEval metric and adds it to the evaluator. 
    /// GEval is a metric that uses an LLM-as-a-judge to evaluate the quality of a model's output based on the evaluation steps or criteria.
    /// If the evaluation steps are provided, criteria will be ignored and the evaluation steps will be used instead.
    /// </summary>
    /// <param name="config">Configuration used to tune strict mode, threshold, and more.</param>
    public void AddGEval(GEvalMetricConfiguration config)
    {
        CheckThatChatClientIsSet();
        EnsureFieldsAreSetForGEval();
        config = CreateConfig(config);
        var metric = new GEvalMetric(ChatClient!, config);
        AddMetric(metric);
    }

    /// <summary>
    /// Creates the Hallucination metric and adds it to the evaluator. 
    /// Hallucination metric is a metric that uses an LLM-as-a-judge to evaluate LLM hallucinations by analyzing the actual output and the context.
    /// Hallucination metric uses the context as the source of truth and calculates how much the actual output disagrees with the context. From this calculation, a score is derived.
    /// </summary>
    /// <param name="config">Configuration used to tune strict mode, threshold, and more.</param>
    public void AddHallucination(HallucinationMetricConfiguration config)
    {
        CheckThatChatClientIsSet();
        EnsureFieldsAreSetForHallucination();
        config = CreateConfig(config);
        var metric = new HallucinationMetric(ChatClient!, config);
        AddMetric(metric);
    }

    /// <summary>
    /// Creates the Hallucination metric and adds it to the evaluator. 
    /// Hallucination metric is a metric that uses an LLM-as-a-judge to evaluate LLM hallucinations by analyzing the actual output and the context.
    /// Hallucination metric uses the context as the source of truth and calculates how much the actual output disagrees with the context. From this calculation, a score is derived.
    /// </summary>
    /// <param name="includeReason">Includes a reason that the LLM made its determination. Note that this will call an LLM a second time.</param>
    /// <param name="strictMode">Enforces a binary metric score. Either perfect score of 1 or a failure of anything less. Defaults to false.</param>
    /// <param name="threshold">Threshold for what is considered a pass or failure. Defaults to 0.5.</param>
    public void AddHallucination(bool? includeReason = null, bool? strictMode = null, double? threshold = null)
    {
        CheckThatChatClientIsSet();
        EnsureFieldsAreSetForHallucination();
        var config = CreateConfig<HallucinationMetricConfiguration>(includeReason, strictMode, threshold);
        var metric = new HallucinationMetric(ChatClient!, config);
        AddMetric(metric);
    }

    /// <summary>
    /// Creates the Prompt Alignment metric and adds it to the evaluator. 
    /// Prompt Alignment metric is a metric that uses an LLM-as-a-judge to evaluate whether the model's output aligns with the specified prompt instructions.
    /// </summary>
    /// <param name="config">Configuration used to tune strict mode, threshold, and more.</param>
    public void AddPromptAlignment(PromptAlignmentMetricConfiguration config)
    {
        CheckThatChatClientIsSet();
        EnsureFieldsAreSetForPromptAlignment();
        config = CreateConfig(config);
        var metric = new PromptAlignmentMetric(ChatClient!, config);
        AddMetric(metric);
    }

    /// <summary>
    /// Creates the Prompt Alignment metric and adds it to the evaluator. 
    /// Prompt Alignment metric is a metric that uses an LLM-as-a-judge to evaluate whether the model's output aligns with the specified prompt instructions.
    /// </summary>
    /// <param name="promptInstructions">List of instructions to validate against the model's response.</param>
    /// <param name="includeReason">Includes a reason that the LLM made its determination. Note that this will call an LLM a second time.</param>
    /// <param name="strictMode">Enforces a binary metric score. Either perfect score of 1 or a failure of anything less. Defaults to false.</param>
    /// <param name="threshold">Threshold for what is considered a pass or failure. Defaults to 0.5.</param>
    public void AddPromptAlignment(IEnumerable<string> promptInstructions, bool? includeReason = null, bool? strictMode = null, double? threshold = null)
    {
        CheckThatChatClientIsSet();
        EnsureFieldsAreSetForPromptAlignment();
        var config = CreateConfig<PromptAlignmentMetricConfiguration>(includeReason, strictMode, threshold) with
        {
            PromptInstructions = [.. promptInstructions]
        };
        var metric = new PromptAlignmentMetric(ChatClient!, config);
        AddMetric(metric);
    }

    /// <summary>
    /// Creates the Summarization metric and add it to the evaluator using the given configuration. 
    /// The Summarization metric uses an LLM-as-a-judge to evaluate the quality and factual accuracy of a summary against source content (input).
    /// </summary>
    /// <param name="config">Configuration used to tune strict mode, threshold, and more.</param>
    public void AddSummarization(SummarizationMetricConfiguration config)
    {
        CheckThatChatClientIsSet();
        EnsureFieldsAreSetForSummarization();
        config = CreateConfig(config);
        var metric = new SummarizationMetric(ChatClient!, config);
        AddMetric(metric);
    }

    /// <summary>
    /// Creates the Summarization metric and adds it to the evaluator.
    /// This metric uses an LLM-as-a-judge to evaluate the quality and factual accuracy of a summary against source content (input).
    /// Assessment questions are generated by the LLM, and you can specify how many questions to generate with numQuestions, otherwise a default of 5 is used.
    /// If you want to avoid the extra LLM call and/or want to tightly control the assessment questions, you can use the overload that takes a list of assessment questions.
    /// </summary>
    /// <param name="numQuestions">Number of assessment questions to generate when none are provided. Defaults to 5.</param>
    /// <param name="truthsExtractionLimit">Maximum number of factual claims to extract from the summary; null lets the LLM choose.</param>
    /// <param name="includeReason">Includes a reason that the LLM made its determination. Note that this will call an LLM a second time.</param>
    /// <param name="strictMode">Enforces a binary metric score. Either perfect score of 1 or a failure of anything less. Defaults to false.</param>
    /// <param name="threshold">Threshold for what is considered a pass or failure. Defaults to 0.5.</param>
    public void AddSummarization(int numQuestions = 5, int? truthsExtractionLimit = null, bool? includeReason = null, bool? strictMode = null, double? threshold = null)
    {
        CheckThatChatClientIsSet();
        EnsureFieldsAreSetForSummarization();
        var config = CreateConfig<SummarizationMetricConfiguration>(includeReason, strictMode, threshold) with
        {
            TruthsExtractionLimit = truthsExtractionLimit,
            NumQuestions = numQuestions
        };
        var metric = new SummarizationMetric(ChatClient!, config);
        AddMetric(metric);
    }

    /// <summary>
    /// Creates the Summarization metric and adds it to the evaluator.
    /// This metric uses an LLM-as-a-judge to evaluate the quality and factual accuracy of a summary against source content (input).
    /// Assessment questions are generated by the LLM, and you can specify how many questions to generate with numQuestions, otherwise a default of 5 is used.
    /// If you're unsure of the assessment questions and are less concerned with the reliability of the metric score, you can use the overload that takes an optional number of assessment questions to generate.
    /// </summary>
    /// <param name="assessmentQuestions">List of close-ended questions to assess summary quality that can be answered with yes or no.</param>
    /// <param name="truthsExtractionLimit">Maximum number of factual claims to extract from the original text (input); null lets the LLM choose.</param>
    /// <param name="includeReason">Includes a reason that the LLM made its determination. Note that this will call an LLM a second time.</param>
    /// <param name="strictMode">Enforces a binary metric score. Either perfect score of 1 or a failure of anything less. Defaults to false.</param>
    /// <param name="threshold">Threshold for what is considered a pass or failure. Defaults to 0.5.</param>
    public void AddSummarization(List<string> assessmentQuestions, int? truthsExtractionLimit = null, bool? includeReason = null, bool? strictMode = null, double? threshold = null)
    {
        CheckThatChatClientIsSet();
        EnsureFieldsAreSetForSummarization();
        var config = CreateConfig<SummarizationMetricConfiguration>(includeReason, strictMode, threshold) with
        {
            TruthsExtractionLimit = truthsExtractionLimit,
            AssessmentQuestions = assessmentQuestions
        };
        var metric = new SummarizationMetric(ChatClient!, config);
        AddMetric(metric);
    }

    /// <summary>
    /// Creates the Task Completion metric and adds it to the evaluator.
    /// This metric uses an LLM-as-a-judge to measure how well the LLM fulfills explicit task instructions stated in the input based on the tools called and the actual output.
    /// </summary>
    /// <param name="config">Configuration used to tune strict mode, threshold, and more.</param>
    public void AddTaskCompletion(TaskCompletionMetricConfiguration config)
    {
        CheckThatChatClientIsSet();
        EnsureFieldsAreSetForTaskCompletion();
        config = CreateConfig(config);
        var metric = new TaskCompletionMetric(ChatClient!, config);
        AddMetric(metric);
    }

    /// <summary>
    /// Creates the Task Completion metric and adds it to the evaluator.
    /// This metric uses an LLM-as-a-judge to measure how well the LLM fulfills explicit task instructions stated in the input based on the tools called and the actual output.
    /// </summary>
    /// <param name="includeReason">Includes a reason that the LLM made its determination. Note that this will call an LLM a second time.</param>
    /// <param name="strictMode">Enforces a binary metric score. Either perfect score of 1 or a failure of anything less. Defaults to false.</param>
    /// <param name="threshold">Threshold for what is considered a pass or failure. Defaults to 0.5.</param>
    public void AddTaskCompletion(bool? includeReason = null, bool? strictMode = null, double? threshold = null)
    {
        CheckThatChatClientIsSet();
        EnsureFieldsAreSetForTaskCompletion();
        var config = CreateConfig<TaskCompletionMetricConfiguration>(includeReason, strictMode, threshold);
        var metric = new TaskCompletionMetric(ChatClient!, config);
        AddMetric(metric);
    }

    /// <summary>
    /// Creates the Task Completion metric and adds it to the evaluator. This metric does NOT use LLM-as-a-judge.
    /// This metric evaluates whether tool calls match the expected tool calls.
    /// If ShouldExactMatch is set to True, it will trump ShouldConsiderOrdering.
    /// </summary>
    /// <param name="config">Configuration used to tune strict mode, threshold, and more.</param>
    public void AddToolCorrectness(ToolCorrectnessMetricConfiguration config)
    {
        EnsureFieldsAreSetForToolCorrectness();
        var metric = new ToolCorrectnessMetric(config);
        AddMetric(metric);
    }

    /// <summary>
    /// Creates the Task Completion metric and adds it to the evaluator. This metric does NOT use LLM-as-a-judge.
    /// This metric evaluates whether tool calls match the expected tool calls.
    /// If you would like to evaluate the order of tool calls, set shouldConsiderOrdering to true.
    /// A match is based on the evaluation parameters. Tool names are always checked even if ToolCallParamsEnum.TOOL is not specified in the evaluation parameters.
    /// </summary>
    /// <param name="shouldConsiderOrdering">Indicates if order should be considered a part of the metric score</param>
    /// <param name="threshold">Threshold for what is considered a pass or failure. Defaults to 0.5.</param>
    /// <param name="evaluationParams">List of tool call parameters to include in scoring; defaults to TOOL.</param>
    public void AddToolCorrectness(bool shouldConsiderOrdering = false, double threshold = 0.5, List<ToolCallParamsEnum>? evaluationParams = null)
    {
        EnsureFieldsAreSetForToolCorrectness();
        var config = new ToolCorrectnessMetricConfiguration
        {
            ShouldConsiderOrdering = shouldConsiderOrdering,
            Threshold = threshold,
            EvaluationParams = evaluationParams ?? [ToolCallParamsEnum.TOOL]
        };
        var metric = new ToolCorrectnessMetric(config);
        AddMetric(metric);
    }

    /// <summary>
    /// Creates the Task Completion metric and adds it to the evaluator. This metric does NOT use LLM-as-a-judge.
    /// This metric evaluates whether tool calls EXACTLY match the expected tool calls and their respective order.
    /// An exact match is based on the evaluation parameters. Tool names are always checked even if ToolCallParamsEnum.TOOL is not specified in the evaluation parameters.
    /// </summary>
    /// <param name="threshold">Threshold for what is considered a pass or failure. Defaults to 0.5.</param>
    /// <param name="evaluationParams">List of tool call parameters to include in scoring; defaults to TOOL.</param>
    public void AddToolCorrectnessExactMatch(double threshold = 0.5, List<ToolCallParamsEnum>? evaluationParams = null)
    {
        EnsureFieldsAreSetForToolCorrectness();
        var config = new ToolCorrectnessMetricConfiguration
        {
            ShouldExactMatch = true,
            Threshold = threshold,
            EvaluationParams = evaluationParams ?? [ToolCallParamsEnum.TOOL]
        };
        var metric = new ToolCorrectnessMetric(config);
        AddMetric(metric);
    }

    /// <summary>
    /// Creates the Match metric and adds it to the evaluator. This metric does NOT use LLM-as-a-judge.
    /// Checks if the actual output EXACTLY matches the expected output.
    /// </summary>
    public void AddExactMatchMetric()
    {
        EnsureFieldsAreSetForMatch();
        var metric = MatchMetric.Exact();
        AddMetric(metric);
    }

    /// <summary>
    /// Creates the Match metric and adds it to the evaluator. This metric does NOT use LLM-as-a-judge.
    /// Extracts desired text from expected output and matches against actual output. 
    /// If there is more than one regex match in the actual output, the test case fails.
    /// If the extracted output does not match the expected output, the test case fails.
    /// </summary>
    /// <param name="matchRegexString">Regex to extract text from the actual output.</param>
    /// <param name="stringComparisonForAnswer">Optional string comparison for matching the extracted text from actual output against the expected output</param>
    public void AddRegexMatchMetric(string matchRegexString, StringComparison? stringComparisonForAnswer = null)
    {
        EnsureFieldsAreSetForMatch();
        var metric = MatchMetric.Regex(matchRegexString, stringComparisonForAnswer);
        AddMetric(metric);
    }

    /// <summary>
    /// Creates the Match metric and adds it to the evaluator. This metric does NOT use LLM-as-a-judge.
    /// Matches after the occurrence of the given string. Example: if your LLM is giving its answer after printing "Answer:",
    /// you can use this to match the text after "Answer:" is returned by the LLM.
    ///
    /// If the string occurrence is not found, the test case fails.
    /// If the string occurs multiple times, the test case fails.
    /// After the answer is found, the test ignores whitespace.
    /// </summary>
    /// <param name="searchString">String to match on prefix of actual output.</param>
    public void AddAfterStringMatchMetric(string searchString)
    {
        EnsureFieldsAreSetForMatch();
        var metric = MatchMetric.AfterString(searchString);
        AddMetric(metric);
    }

    /// <summary>
    /// Starts the evaluation process by running all added metrics against the provided data.
    /// MetricResults are printed to the console and returned as an EvaluatorResult object.
    /// </summary>
    public async Task<EvalResult> RunAsync()
    {
        if (!Metrics.Any())
        {
            throw new InvalidOperationException("No metrics have been added to the evaluator.");
        }

        PrintMetrics();

        var scores = new EvalResult();

        foreach (var test in Data)
        {
            var results = new ConcurrentBag<(Metric, MetricScore)>();

            await Parallel.ForEachAsync(Metrics, async (metric, _) =>
            {
                var metricScore = await metric.ScoreAsync(test);
                results.Add((metric, metricScore));
            });
            var metricResultCollection = new MetricResultCollection(test, [.. results]);
            scores.Tests.Add(metricResultCollection);
        }

        var printer = new EvalResultPrinter(scores);
        printer.PrintReport();
        return scores;
    }

    private void CheckThatChatClientIsSet()
    {
        if (ChatClient is null)
        {
            throw new InvalidOperationException("An instance of IChatClient is required to use this metric.");
        }
    }
    
    private TConfiguration CreateConfig<TConfiguration>(TConfiguration originalConfiguration)
        where TConfiguration : LLMAsAJudgeMetricConfiguration
    {
        return originalConfiguration with
        {
            SystemPrompt = originalConfiguration.SystemPrompt ?? Configuration.SystemPrompt,
            Temperature = originalConfiguration.Temperature ?? Configuration.Temperature,
            StrictMode = originalConfiguration.StrictMode ?? Configuration.StrictMode,
            Threshold = originalConfiguration.Threshold,
            IncludeReason = originalConfiguration.IncludeReason
        };
    }
    
    private TConfiguration CreateConfig<TConfiguration>(bool? includeReason, bool? strictMode, double? threshold)
        where TConfiguration : LLMAsAJudgeMetricConfiguration, new()
    {
        return new TConfiguration
        {
            SystemPrompt = Configuration.SystemPrompt,
            Temperature = Configuration.Temperature,
            StrictMode = strictMode ?? Configuration.StrictMode,
            Threshold = threshold ?? 0.5,
            IncludeReason = includeReason ?? true,
        };
    }

    private void PrintMetrics()
    {
        foreach (var metric in Metrics)
        {
            var meta = metric.Meta();
            var strictMode = meta.StrictMode ?? false ? "True" : "False";
            Console.WriteLine($"You're running EvalSharps's {meta.Name} Metric! (using {meta.Model}, strict = {strictMode})");
        }
        Console.WriteLine($"Evaluating {Data.Count()} test case(s) in parallel:");
    }


    #region NULL checks for metric fields
    private void EnsureFieldsAreSetForGEval()
    {
        var errors = new List<string>();
        foreach (var obj in Data)
        {
            if (string.IsNullOrWhiteSpace(obj.InitialInput))
            {
                errors.Add($"Object at position {Data.IndexOf(obj)}: InitialInput is required for GEval metric.");
            }
            if (string.IsNullOrWhiteSpace(obj.ActualOutput))
            {
                errors.Add($"Object at position {Data.IndexOf(obj)}: ActualOutput is required for GEval metric.");
            }
        }

        if (errors.Count != 0)
        {
            throw new EvalConfigurationException("GEval metric requires the following fields to be set in the EvaluatorTestData: InitialInput and ActualOutput. " +
                "Please ensure that these fields are set for all objects in the dataset.\n\n" + string.Join(Environment.NewLine, errors));
        }
    }

    private void EnsureFieldsAreSetForAnswerRelevancy()
    {
        var errors = new List<string>();
        foreach (var obj in Data)
        {
            if (string.IsNullOrWhiteSpace(obj.InitialInput))
            {
                errors.Add($"Object at position {Data.IndexOf(obj)}: InitialInput is required for Answer Relevancy metric.");
            }
            if (string.IsNullOrWhiteSpace(obj.ActualOutput))
            {
                errors.Add($"Object at position {Data.IndexOf(obj)}: ActualOutput is required for Answer Relevancy metric.");
            }
        }

        if (errors.Count != 0)
        {
            throw new EvalConfigurationException("AnswerRelevancy metric requires the following fields to be set in the EvaluatorTestData: InitialInput and ActualOutput. " +
                "Please ensure that these fields are set for all objects in the dataset.\n\n" + string.Join(Environment.NewLine, errors));
        }
    }

    private void EnsureFieldsAreSetForFaithfulness()
    {
        var errors = new List<string>();
        foreach (var obj in Data)
        {
            if (string.IsNullOrWhiteSpace(obj.ActualOutput))
            {
                errors.Add($"Object at position {Data.IndexOf(obj)}: ActualOutput is required for Faithfulness metric.");
            }
            if (obj.RetrievalContext == null)
            {
                errors.Add($"Object at position {Data.IndexOf(obj)}: RetrievalContext is required for Faithfulness metric.");
            }
        }

        if (errors.Count != 0)
        {
            throw new EvalConfigurationException("Faithfulness metric requires the following fields to be set in the EvaluatorTestData: ActualOutput and RetrievalContext. " +
                "Please ensure that these fields are set for all objects in the dataset.\n\n" + string.Join(Environment.NewLine, errors));
        }
    }

    private void EnsureFieldsAreSetForContextualPrecision()
    {
        var errors = new List<string>();
        foreach (var obj in Data)
        {
            if (string.IsNullOrWhiteSpace(obj.InitialInput))
            {
                errors.Add($"Object at position {Data.IndexOf(obj)}: InitialInput is required for ContextualPrecision metric.");
            }
            if (string.IsNullOrWhiteSpace(obj.ExpectedOutput))
            {
                errors.Add($"Object at position {Data.IndexOf(obj)}: ExpectedOutput is required for ContextualPrecision metric.");
            }
            if (obj.RetrievalContext == null)
            {
                errors.Add($"Object at position {Data.IndexOf(obj)}: RetrievalContext is required for ContextualPrecision metric.");
            }
        }

        if (errors.Count != 0)
        {
            throw new EvalConfigurationException("ContextualPrecision metric requires the following fields to be set in the EvaluatorTestData: InitialInput, ExpectedOutput, and RetrievalContext. " +
                "Please ensure that these fields are set for all objects in the dataset.\n\n" + string.Join(Environment.NewLine, errors));
        }
    }

    private void EnsureFieldsAreSetForContextualRecall()
    {
        var errors = new List<string>();
        foreach (var obj in Data)
        {
            if (string.IsNullOrWhiteSpace(obj.ExpectedOutput))
            {
                errors.Add($"Object at position {Data.IndexOf(obj)}: ExpectedOutput is required for ContextualRecall metric.");
            }
            if (obj.RetrievalContext == null || obj.RetrievalContext.Count == 0)
            {
                errors.Add($"Object at position {Data.IndexOf(obj)}: RetrievalContext is required for ContextualRecall metric.");
            }
        }

        if (errors.Count != 0)
        {
            throw new EvalConfigurationException("ContextualRecall metric requires the following fields to be set in the EvaluatorTestData: ExpectedOutput and RetrievalContext. " +
                "Please ensure that these fields are set for all objects in the dataset.\n\n" + string.Join(Environment.NewLine, errors));
        }
    }

    private void EnsureFieldsAreSetForBias()
    {
        var errors = new List<string>();
        foreach (var obj in Data)
        {
            if (string.IsNullOrWhiteSpace(obj.ActualOutput))
            {
                errors.Add($"Object at position {Data.IndexOf(obj)}: ActualOutput is required for Bias metric.");
            }
        }

        if (errors.Count != 0)
        {
            throw new EvalConfigurationException("Bias metric requires the following fields to be set in the EvaluatorTestData: ActualOutput. " +
                "Please ensure that these fields are set for all objects in the dataset.\n\n" + string.Join(Environment.NewLine, errors));
        }
    }

    private void EnsureFieldsAreSetForSummarization()
    {
        var errors = new List<string>();
        foreach (var obj in Data)
        {
            if (string.IsNullOrWhiteSpace(obj.InitialInput))
            {
                errors.Add($"Object at position {Data.IndexOf(obj)}: InitialInput is required for Summarization metric.");
            }
            if (string.IsNullOrWhiteSpace(obj.ActualOutput))
            {
                errors.Add($"Object at position {Data.IndexOf(obj)}: ActualOutput is required for Summarization metric.");
            }
        }

        if (errors.Count != 0)
        {
            throw new EvalConfigurationException("Summarization metric requires the following fields to be set in the EvaluatorTestData: InitialInput and ActualOutput. " +
                "Please ensure that these fields are set for all objects in the dataset.\n\n" + string.Join(Environment.NewLine, errors));
        }
    }


    private void EnsureFieldsAreSetForHallucination()
    {
        var errors = new List<string>();
        foreach (var obj in Data)
        {
            if (string.IsNullOrWhiteSpace(obj.ActualOutput))
            {
                errors.Add($"Object at position {Data.IndexOf(obj)}: ActualOutput is required for Hallucination metric.");
            }
            if (obj.Context == null || obj.Context.Count == 0)
            {
                errors.Add($"Object at position {Data.IndexOf(obj)}: Context is required for Hallucination metric.");
            }
        }

        if (errors.Count != 0)
        {
            throw new EvalConfigurationException("Hallucination metric requires the following fields to be set in the EvaluatorTestData: ActualOutput and Context. " +
                "Please ensure that these fields are set for all objects in the dataset.\n\n" + string.Join(Environment.NewLine, errors));
        }
    }

    private void EnsureFieldsAreSetForPromptAlignment()
    {
        var errors = new List<string>();
        foreach (var obj in Data)
        {
            if (string.IsNullOrWhiteSpace(obj.InitialInput))
            {
                errors.Add($"Object at position {Data.IndexOf(obj)}: InitialInput is required for PromptAlignment metric.");
            }
            if (string.IsNullOrWhiteSpace(obj.ActualOutput))
            {
                errors.Add($"Object at position {Data.IndexOf(obj)}: ActualOutput is required for PromptAlignment metric.");
            }
        }

        if (errors.Count != 0)
        {
            throw new EvalConfigurationException("PromptAlignment metric requires the following fields to be set in the EvaluatorTestData: InitialInput and ActualOutput. " +
                "Please ensure that these fields are set for all objects in the dataset.\n\n" + string.Join(Environment.NewLine, errors));
        }
    }

    private void EnsureFieldsAreSetForTaskCompletion()
    {
        var errors = new List<string>();
        foreach (var obj in Data)
        {
            if (string.IsNullOrWhiteSpace(obj.InitialInput))
            {
                errors.Add($"Object at position {Data.IndexOf(obj)}: InitialInput is required for TaskCompletion metric.");
            }
            if (string.IsNullOrWhiteSpace(obj.ActualOutput))
            {
                errors.Add($"Object at position {Data.IndexOf(obj)}: ActualOutput is required for TaskCompletion metric.");
            }
            if (obj.ToolsCalled == null)
            {
                errors.Add($"Object at position {Data.IndexOf(obj)}: ToolsCalled is required for TaskCompletion metric.");
            }
        }

        if (errors.Count != 0)
        {
            throw new EvalConfigurationException("TaskCompletion metric requires the following fields to be set in the EvaluatorTestData: InitialInput, ActualOutput, and ToolsCalled. " +
                "Please ensure that these fields are set for all objects in the dataset.\n\n" + string.Join(Environment.NewLine, errors));
        }
    }

    private void EnsureFieldsAreSetForToolCorrectness()
    {
        var errors = new List<string>();
        foreach (var obj in Data)
        {
            if (obj.ToolsCalled == null)
            {
                errors.Add($"Object at position {Data.IndexOf(obj)}: ToolsCalled is required for ToolCorrectness metric.");
            }
            if (obj.ExpectedTools == null)
            {
                errors.Add($"Object at position {Data.IndexOf(obj)}: ExpectedTools is required for ToolCorrectness metric.");
            }
        }

        if (errors.Count != 0)
        {
            throw new EvalConfigurationException("ToolCorrectness metric requires the following fields to be set in the EvaluatorTestData: ToolsCalled and ExpectedTools. " +
                "Please ensure that these fields are set for all objects in the dataset.\n\n" + string.Join(Environment.NewLine, errors));
        }
    }

    private void EnsureFieldsAreSetForMatch()
    {
        var errors = new List<string>();
        foreach (var obj in Data)
        {
            if (string.IsNullOrWhiteSpace(obj.ActualOutput))
            {
                errors.Add($"Object at position {Data.IndexOf(obj)}: ActualOutput is required for Match metric.");
            }
            if (string.IsNullOrWhiteSpace(obj.ExpectedOutput))
            {
                errors.Add($"Object at position {Data.IndexOf(obj)}: ExpectedOutput is required for Match metric.");
            }
        }

        if (errors.Count != 0)
        {
            throw new EvalConfigurationException("Match metric requires the following fields to be set in the EvaluatorTestData: ActualOutput and ExpectedOutput. " +
                "Please ensure that these fields are set for all objects in the dataset.\n\n" + string.Join(Environment.NewLine, errors));
        }
    }
    #endregion NULL checks for metric fields
}


