using Microsoft.Extensions.AI;

namespace EvalSharp.Scoring;

/// <summary>
/// Represents a metric with a specific configuration.
/// </summary>
/// <typeparam name="TConfiguration">The type of the metric configuration.</typeparam>
public abstract class Metric<TConfiguration>(TConfiguration configuration) : Metric, IConfigurableMetric
    where TConfiguration : MetricConfiguration
{
    /// <summary>
    /// Gets the configuration settings for the metric.
    /// </summary>
    public TConfiguration Configuration { get; } = configuration;
    MetricConfiguration IConfigurableMetric.Configuration => Configuration;
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
public class MetricConfiguration
{
    /// <summary>
    ///A float representing the minimum passing score, defaulting to 0.5.
    /// </summary>
    public double Threshold { get; set; } = 0.5;

    /// <summary>
    /// Enforces a binary metric score�1 for perfect relevance, 0 otherwise�setting the threshold to 1. Default is False.
    /// </summary>
    public bool StrictMode { get; set; } = false;
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

