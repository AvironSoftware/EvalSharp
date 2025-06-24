using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json;
using EvalSharp.JsonConverters;
using EvalSharp.Models;
using EvalSharp.Models.Enums;
using EvalSharp.Scoring;

#pragma warning disable SKEXP0010

namespace EvalSharp;

internal static class Extensions
{
    private readonly static JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        PropertyNameCaseInsensitive = true,
        Converters = { new VerdictEnumConverter() }
    };
    
    public static async Task<TResponse> GetStructuredResponseFromLLM<TResponse>(this IChatClient chatCompletionService, string prompt)
        where TResponse : class
    {
        var responseAsync = (await chatCompletionService.GetResponseAsync<TResponse>(prompt, _serializerOptions));
        return responseAsync.Result;
    }
    
    public static async Task<(TResponse Response, ChatResponse<TResponse> ChatResponse)> GetStructuredResponseFromLLMWithOriginalResponse<TResponse>(
        this IChatClient chatCompletionService, 
        string prompt,
        Action<ChatOptions>? configureSettings = null
    )
        where TResponse : class
    {
        var chatOptions = new ChatOptions();
        configureSettings?.Invoke(chatOptions);
        var resp = await chatCompletionService.GetResponseAsync<TResponse>(
            prompt,
            _serializerOptions,
            chatOptions
        );

        return (resp.Result, resp);
    }

    /// <summary>
    /// Retrieves metadata information for the specified <see cref="Metric"/> instance.
    /// </summary>
    /// <param name="metric">The <see cref="Metric"/> for which metadata is to be retrieved. Cannot be null.</param>
    /// <returns>A <see cref="MetricMetadata"/> object containing metadata derived from the provided <see cref="Metric"/>. If the
    /// metric implements <see cref="IConfigurableMetric"/>, configuration details such as strict mode and threshold are
    /// included. If the metric implements <see cref="IChatClientMetric"/>, the default model ID is included.</returns>
    public static MetricMetadata Meta(this Metric metric)
    {
        var meta = new MetricMetadata(metric.Name);
        try
        {
            if (metric is IConfigurableMetric configurableMetric)
            {
                meta.StrictMode = configurableMetric?.Configuration?.StrictMode;
                meta.Threshold = configurableMetric?.Configuration?.Threshold;
            }

            if (metric is IChatClientMetric chatClientMetric)
            {
                meta.Model = chatClientMetric.ChatClient.GetService(typeof(ChatClientMetadata)) is ChatClientMetadata metadata ? metadata?.DefaultModelId ?? "" : string.Empty;
            }
        }
        catch { }

        return meta;
    }


    internal static bool TryGetLogprobs(this ChatResponse chatMessageContent, out IReadOnlyList<ChatTokenLogProbabilityDetails>? logprobs)
    {
        if (chatMessageContent.RawRepresentation is ChatCompletion chatCompletion)
        {
            logprobs = chatCompletion.ContentTokenLogProbabilities;
            return logprobs?.Count > 0;
        }

        logprobs = null;
        return false;
    }

    internal static string ToFormattedList(this IEnumerable<string>? strings)
    {
        // this matches output of lists in Python when using string interpolation.
        if (strings == null) return "None";
        return "[" + string.Join(",", (strings).Select(s => $"'{s}'")) + "]";
    }

    internal static List<string> GetReasons(this VerdictModel[] verdicts, VerdictEnum verdictEnum)
    {
        return verdicts.Where(v =>
                v.Verdict == verdictEnum
                && !string.IsNullOrEmpty(v.Reason))
                .Select(v => v.Reason!)
                .ToList();
    }

    internal static List<string> GetReasons(this List<SummarizationCoverageVerdict> verdicts)
    {
        return verdicts
            .Where(v =>
                v.OriginalVerdict == VerdictEnum.Yes &&
                v.SummaryVerdict == VerdictEnum.No)
            .Select(v => v.Question)
            .Where(q => !string.IsNullOrWhiteSpace(q))
            .ToList();
    }

    internal static double ScoreYesIdk(this VerdictModel[] verdicts)
    {
        if (verdicts.Length == 0) return 1.0;
        int verdictCount = verdicts.Count(v => v.Verdict != VerdictEnum.No);
        return (double)verdictCount / verdicts.Length;
    }

    internal static double ScoreYes(this VerdictModel[] verdicts)
    {
        if (verdicts.Length == 0) return 0.0;
        int verdictCount = verdicts.Count(v => v.Verdict == VerdictEnum.Yes);
        return (double)verdictCount / verdicts.Length;
    }

    internal static double ScoreNo(this VerdictModel[] verdicts)
    {
        if (verdicts.Length == 0) return 0.0;
        int verdictCount = verdicts.Count(v => v.Verdict == VerdictEnum.No);
        return (double)verdictCount / verdicts.Length;
    }

    internal static double ScoreYes(this List<SummarizationCoverageVerdict> verdicts)
    {
        if (verdicts.Count == 0) return 1;

        int countYes = verdicts.Count(v =>
            v.OriginalVerdict == VerdictEnum.Yes &&
            v.SummaryVerdict == VerdictEnum.Yes);

        int total = verdicts.Count(v =>
            v.OriginalVerdict == VerdictEnum.Yes);

        return total == 0 ? 0 : (double)countYes / total;
    }
}