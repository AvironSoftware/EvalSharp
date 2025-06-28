using Microsoft.Extensions.AI;

namespace EvalSharp.Scoring;

/// <summary>
/// Represents an evaluation metric that uses an LLM to make its judgment.
/// </summary>
/// <typeparam name="TConfiguration">The type of the metric configuration.</typeparam>
public abstract class LLMAsAJudgeMetric<TConfiguration> : Metric<TConfiguration>
    where TConfiguration : LLMAsAJudgeMetricConfiguration
{
    /// <summary>
    /// Gets the chat client used for interacting with the LLM.
    /// </summary>
    public IChatClient ChatClient { get; }

    /// <summary>
    /// Base constructor for the LLM-as-a-judge metric.
    /// </summary>
    /// <param name="configuration">The metric configuration</param>
    /// <param name="chatClient">The chat client used to execute the metric</param>
    public LLMAsAJudgeMetric(TConfiguration configuration, IChatClient chatClient) : base(configuration)
    {
        ChatClient = chatClient;
    }
    
    internal async Task<TResponse> GetStructuredResponseFromLLM<TResponse>(
        string prompt
    )
        where TResponse : class
    {
        return await ChatClient.GetStructuredResponseFromLLM<TResponse>(prompt, Configuration);
    }
}

/// <summary>
/// Base class for LLM-as-a-judge metrics.
/// </summary>
public record LLMAsAJudgeMetricConfiguration : MetricConfiguration
{
    /// <summary>
    /// The system prompt to use for the LLM interactions.
    /// </summary>
    public string? SystemPrompt { get; set; }
    
    /// <summary>
    /// The temperature to use for the LLM interactions.
    /// </summary>
    public float? Temperature { get; set; }

    /// <summary>
    /// A boolean that, when set to True, makes an extra LLM call to provide a reason for the metric score. Default is True.
    /// </summary>
    public bool IncludeReason { get; set; } = true;
}

/// <summary>
/// Represents a metric with a specific configuration.
/// </summary>
/// <typeparam name="TConfiguration">The type of the metric configuration.</typeparam>
public abstract class Metric<TConfiguration> : Metric, IConfigurableMetric
    where TConfiguration : MetricConfiguration
{
    /// <summary>
    /// Base constructor for the Metric object
    /// </summary>
    /// <param name="configuration"></param>
    public Metric(TConfiguration configuration)
    {
        Configuration = configuration;
    }
    
    /// <summary>
    /// Gets the configuration settings for the metric.
    /// </summary>
    public TConfiguration Configuration { get; }
    MetricConfiguration IConfigurableMetric.Configuration => Configuration;
}

/// <summary>
/// Represents a conversational (e.g. multi-message) metric with a specific configuration.
/// </summary>
public abstract class ConversationalMetric
{
    /// <summary>
    /// Scores the provided test data asynchronously.
    /// </summary>
    /// <param name="testData">The test data containing expected and actual outputs, context, and tool calls.</param>
    /// <returns>A task that represents the asynchronous operation, containing the metric score.</returns>
    public abstract Task<MetricScore> ScoreAsync(EvaluatorTestData testData);
    
    /// <summary>
    /// Gets the name of the metric, derived from the type name by removing "Metric".
    /// </summary>
    public virtual string Name => this.GetType().Name.Replace("Metric", string.Empty);
}

/// <summary>
/// Represents a metric with a specific configuration.
/// </summary>
public abstract class Metric
{
    /// <summary>
    /// Scores the provided test data asynchronously.
    /// </summary>
    /// <param name="testData">The test data containing expected and actual outputs, context, and tool calls.</param>
    /// <returns>A task that represents the asynchronous operation, containing the metric score.</returns>
    public abstract Task<MetricScore> ScoreAsync(EvaluatorTestData testData);
    /// <summary>
    /// Gets the name of the metric, derived from the type name by removing "Metric".
    /// </summary>
    public virtual string Name => this.GetType().Name.Replace("Metric", string.Empty);
}

/// <summary>
/// The base class for all metric configurations.
/// </summary>
public record MetricConfiguration
{
    /// <summary>
    /// A float representing the minimum passing score, defaulting to 0.5.
    /// </summary>
    public double Threshold { get; set; } = 0.5;

    /// <summary>
    /// Enforces a binary metric score. Setting to true effectively sets the threshold to 1, meaning only a perfect score will pass.
    /// </summary>
    public bool? StrictMode { get; set; }
}

/// <summary>
/// Represents a metric that can be configured with specific settings.
/// </summary>
public interface IConfigurableMetric
{
    /// <summary>
    /// Gets the configuration settings for the metric.
    /// </summary>
    MetricConfiguration Configuration { get; }
}

/// <summary>
/// Represents a metric that interacts with a chat client.
/// </summary>
public interface IChatClientMetric
{
    /// <summary>
    /// Gets the chat client associated with the metric.
    /// </summary>
    IChatClient ChatClient { get; }
}

