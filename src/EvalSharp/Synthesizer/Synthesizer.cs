using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace EvalSharp.Synthesizer
{
    public class DataSynthesizer
    {
        private readonly ILLMModel _model;
        private readonly IEmbeddingModel _embedder;
        private readonly FiltrationConfig _filtrationConfig;
        private readonly EvolutionConfig _evolutionConfig;
        private readonly StylingConfig _stylingConfig;

        /// <summary>
        /// 
        /// </summary>
        public List<Golden> SyntheticGoldens { get; } = new();

        public DataSynthesizer(
            IChatClient model,
            IEmbeddingModel? embedder = null,
            FiltrationConfig? filtrationConfig = null,
            EvolutionConfig? evolutionConfig = null,
            StylingConfig? stylingConfig = null)
        {
            model.Get
            _model = model;
            _embedder = embedder ?? new OpenAIEmbeddingModel();
            _filtrationConfig = filtrationConfig ?? new FiltrationConfig(model);
            _evolutionConfig = evolutionConfig ?? new EvolutionConfig();
            _stylingConfig = stylingConfig ?? new StylingConfig();
        }

        public List<Golden> GenerateGoldensFromDocs(
            List<string> documentPaths,
            bool includeExpectedOutput = true,
            int maxGoldensPerContext = 2,
            ContextConstructionConfig? contextConfig = null)
        {
            contextConfig ??= new ContextConstructionConfig(_embedder, _model);
            var ctxGen = new ContextGenerator(
                embedder: _embedder,
                model: contextConfig.CriticModel,
                chunkSize: contextConfig.ChunkSize,
                chunkOverlap: contextConfig.ChunkOverlap,
                filterThreshold: contextConfig.ContextQualityThreshold,
                similarityThreshold: contextConfig.ContextSimilarityThreshold,
                maxRetries: contextConfig.MaxRetries);

            ctxGen.LoadDocs(documentPaths);
            var (contexts, sourceFiles, contextScores) = ctxGen.GenerateContexts(
                contextConfig.MaxContextsPerDocument,
                contextConfig.MaxContextLength);

            return GenerateGoldensFromContexts(
                contexts,
                includeExpectedOutput,
                maxGoldensPerContext,
                sourceFiles,
                contextScores);
        }

        public List<Golden> GenerateGoldensFromContexts(
            List<List<string>> contexts,
            bool includeExpectedOutput = true,
            int maxGoldensPerContext = 2,
            List<string>? sourceFiles = null,
            List<double>? contextScores = null)
        {
            var goldens = new List<Golden>();

            foreach (var (context, idx) in contexts.Select((c, i) => (c, i)))
            {
                // 1. Generate inputs
                var prompt = $$"""
                    {SynthesizerTemplate.GenerateSyntheticInputs(context, maxGoldensPerContext, _stylingConfig.Scenario, _stylingConfig.Task, _stylingConfig.InputFormat)}
                """;
                var syntheticInputs = GenerateInputs(prompt);

                // 2. Qualify inputs
                var (qualifiedInputs, scores) = RewriteInputs(context, syntheticInputs);

                for (int j = 0; j < qualifiedInputs.Count; j++)
                {
                    var data = qualifiedInputs[j];

                    // 3. Evolve input
                    var (evolvedInput, evolutionsUsed) = EvolveInput(
                        data.Input,
                        context,
                        _evolutionConfig.NumEvolutions,
                        _evolutionConfig.Evolutions);

                    // 4. Optional styling rewrite
                    if (!string.IsNullOrEmpty(_stylingConfig.InputFormat)
                        || !string.IsNullOrEmpty(_stylingConfig.Scenario)
                        || !string.IsNullOrEmpty(_stylingConfig.Task))
                    {
                        var rewritePrompt = $$"""
                            {SynthesizerTemplate.RewriteEvolvedInput(evolvedInput, _stylingConfig.Scenario, _stylingConfig.Task, _stylingConfig.InputFormat)}
                        """;
                        var res = GenerateSchema<SyntheticData>(rewritePrompt);
                        evolvedInput = res.Input;
                    }

                    // 5. Build Golden
                    var golden = new Golden(
                        input: evolvedInput,
                        context: context,
                        sourceFile: sourceFiles?[idx],
                        additionalMetadata: new Dictionary<string, object?>
                        {
                            ["evolutions"] = evolutionsUsed,
                            ["synthetic_input_quality"] = scores[j],
                            ["context_quality"] = contextScores?.ElementAtOrDefault(idx)
                        });

                    // 6. Optional expected output
                    if (includeExpectedOutput)
                    {
                        var outputPrompt = $$"""
                            {SynthesizerTemplate.GenerateSyntheticExpectedOutput(golden.Input, string.Join("\n", golden.Context), _stylingConfig.ExpectedOutputFormat)}
                        """;
                        golden.ExpectedOutput = Generate(outputPrompt);
                    }

                    goldens.Add(golden);
                }
            }

            SyntheticGoldens.AddRange(goldens);
            return goldens;
        }

        public List<Golden> GenerateGoldensFromScratch(int numGoldens)
        {
            if (string.IsNullOrEmpty(_stylingConfig.Scenario)
                || string.IsNullOrEmpty(_stylingConfig.Task)
                || string.IsNullOrEmpty(_stylingConfig.InputFormat))
            {
                throw new InvalidOperationException(
                    "Scenario, Task, and InputFormat must be set for scratch generation.");
            }

            var transformed = TransformDistribution(_evolutionConfig.Evolutions);
            var prompt = PromptSynthesizerTemplate.GenerateSyntheticPrompts(
                _stylingConfig.Scenario!,
                _stylingConfig.Task!,
                _stylingConfig.InputFormat!,
                numGoldens);

            var syntheticData = GenerateInputs(prompt);
            var goldens = new List<Golden>();

            foreach (var data in syntheticData)
            {
                var (evolvedInput, evolutionsUsed) = EvolveInput(
                    data.Input,
                    null,
                    _evolutionConfig.NumEvolutions,
                    transformed);

                var golden = new Golden(
                    input: evolvedInput,
                    additionalMetadata: new Dictionary<string, object?>
                    {
                        ["evolutions"] = evolutionsUsed
                    });
                goldens.Add(golden);
            }

            SyntheticGoldens.AddRange(goldens);
            return goldens;
        }

        // JSON (de)serializer options
        private static readonly JsonSerializerOptions _jsonOptions =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // 1) Generic JSON→T parser
        private T GenerateSchema<T>(string prompt)
        {
            var json = _model.Generate(prompt);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions)!;
        }

        // 2) Produce a list of SyntheticData from a prompt
        private List<SyntheticData> GenerateInputs(string prompt)
        {
            var wrapper = GenerateSchema<SyntheticDataList>(prompt);
            return wrapper.Data;
        }

        // 3) Score & (re)write low-quality inputs
        private (List<SyntheticData> Inputs, List<double> Scores)
            RewriteInputs(List<string> context, List<SyntheticData> inputs)
        {
            var outputs = new List<SyntheticData>();
            var scores = new List<double>();

            foreach (var item in inputs)
            {
                var current = item.Input;
                double lastScore = 0;

                // retry until above threshold (or exhaust retries)
                for (int attempt = 0; attempt < _filtrationConfig.MaxQualityRetries; attempt++)
                {
                    // 3a) evaluate clarity/answerability
                    var evalPrompt = FilterTemplate.EvaluateSyntheticInputs(current);
                    var fb = GenerateSchema<InputFeedback>(evalPrompt);
                    lastScore = fb.Score;
                    if (lastScore >= _filtrationConfig.SyntheticInputQualityThreshold)
                        break;

                    // 3b) rewrite based on feedback
                    var rewritePrompt = EvolutionTemplate
                        .RewriteSyntheticInputs(string.Join("\n", context), current, fb.Feedback);
                    var rewritten = GenerateSchema<RewrittenInput>(rewritePrompt);
                    current = rewritten.RewrittenInput;
                }

                outputs.Add(new SyntheticData { Input = current });
                scores.Add(lastScore);
            }

            return (outputs, scores);
        }

        // 4) Randomly apply N “evolutions” to an input
        private (string EvolvedInput, List<string> EvolutionsUsed)
            EvolveInput(
                string input,
                List<string>? context,
                int numEvolutions,
                Dictionary<Evolution, double> evolutions)
        {
            var evolved = input;
            var used = new List<string>();
            var rng = new Random();
            var keys = evolutions.Keys.ToList();
            var weights = evolutions.Values.ToList();

            for (int i = 0; i < numEvolutions; i++)
            {
                // pick one evolution strategy by weight
                double total = weights.Sum();
                double pick = rng.NextDouble() * total;
                double acc = 0;
                Evolution choice = keys.Last();
                for (int j = 0; j < keys.Count; j++)
                {
                    acc += weights[j];
                    if (pick <= acc)
                    {
                        choice = keys[j];
                        break;
                    }
                }

                // build the right prompt
                string prompt = choice switch
                {
                    Evolution.Reasoning => PromptEvolutionTemplate.ReasoningEvolution(evolved),
                    Evolution.MultiContext => PromptEvolutionTemplate.MultiContextEvolution(evolved, context),
                    Evolution.Concretizing => PromptEvolutionTemplate.ConcretizingEvolution(evolved),
                    Evolution.Constrained => PromptEvolutionTemplate.ConstrainedEvolution(evolved),
                    Evolution.Comparative => PromptEvolutionTemplate.ComparativeQuestionEvolution(evolved),
                    Evolution.Hypothetical => PromptEvolutionTemplate.HypotheticalScenarioEvolution(evolved),
                    Evolution.InBreadth => PromptEvolutionTemplate.InBreadthEvolution(evolved),
                    _ => evolved
                };

                // call LLM and record which one we used
                evolved = _model.Generate(prompt);
                used.Add(choice.ToString());
            }

            return (evolved, used);
        }
    }

    /// <summary>
    /// Represents a single synthetic data example (input only).
    /// </summary>
    internal class SyntheticData
    {
        public string Input { get; set; } = default!;
    }

    /// <summary>
    /// Holds the synthesized inputs, context, metadata, and optional expected output.
    /// </summary>
    public class Golden
    {
        /// <summary>
        /// 
        /// </summary>
        public string Input { get; }
        /// <summary>
        /// 
        /// </summary>
        public List<string>? Context { get; }
        /// <summary>
        /// 
        /// </summary>
        public string? SourceFile { get; }
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, object?> AdditionalMetadata { get; }
        /// <summary>
        /// 
        /// </summary>
        public string? ExpectedOutput { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <param name="sourceFile"></param>
        /// <param name="additionalMetadata"></param>
        public Golden(
            string input,
            List<string>? context = null,
            string? sourceFile = null,
            Dictionary<string, object?>? additionalMetadata = null)
        {
            Input = input;
            Context = context;
            SourceFile = sourceFile;
            AdditionalMetadata = additionalMetadata ?? new Dictionary<string, object?>();
        }
    }

    /// <summary>
    /// Templates for generating synthetic prompts from scratch.
    /// </summary>
    internal static class PromptSynthesizerTemplate
    {
        public static string GenerateSyntheticPrompts(
            string scenario,
            string task,
            string inputFormat,
            int numGoldens)
        {
            return $$"""
Generate a series of input prompts from scratch based on the provided scenario, task, and output format.
The inputs must align with the given scenario and task description, and conform to specified output format.

**
IMPORTANT: Please make sure to only return in JSON format, with the 'data' key as a list of JSON objects.
You MUST TRY to generate {{numGoldens}} data points.

Example scenario: technical SWE typing SQL queries to query from a database called FAST_FOOD_RESTAURANTS
Example task: Text2SQL LLM Assistant
Example input format: SQL String
Example num prompts: 2
Example JSON:
{"data": [
        {
            "input": "SELECT * FROM menu"
...            
        }
    ]  
}

You MUST include at least one statement as the input. `input` MUST be of `{{inputFormat}}` format.
You MUST TRY to generate {{numGoldens}} data points, unless the generated `input` is getting repetitive.
**

scenario: {{scenario}}
task: {{task}}
input format: {{inputFormat}}
num prompts: {{numGoldens}}
JSON:
""";
        }
    }

    /// <summary>
    /// Configuration for how many contexts to build and how to chunk documents.
    /// </summary>
    internal class ContextConstructionConfig
    {
        /// <summary>
        /// 
        /// </summary>
        public IEmbeddingModel Embedder { get; }
        /// <summary>
        /// 
        /// </summary>
        public ILLMModel CriticModel { get; }
        /// <summary>
        /// 
        /// </summary>
        public int MaxContextsPerDocument { get; set; } = 3;
        /// <summary>
        /// 
        /// </summary>
        public int MaxContextLength { get; set; } = 3;
        /// <summary>
        /// 
        /// </summary>
        public int ChunkSize { get; set; } = 1024;
        /// <summary>
        /// 
        /// </summary>
        public int ChunkOverlap { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        public double ContextQualityThreshold { get; set; } = 0.5;
        /// <summary>
        /// 
        /// </summary>
        public double ContextSimilarityThreshold { get; set; } = 0.0;
        /// <summary>
        /// 
        /// </summary>
        public int MaxRetries { get; set; } = 3;
        /// <summary>
        /// 
        /// </summary>
        public ContextConstructionConfig(
            IEmbeddingModel embedder,
            ILLMModel criticModel)
        {
            Embedder = embedder;
            CriticModel = criticModel;
        }
    }

    /// <summary>
    /// Settings for filtering and re-writing low-quality synthetic inputs.
    /// </summary>
    internal class FiltrationConfig
    {
        /// <summary>
        /// 
        /// </summary>
        public double SyntheticInputQualityThreshold { get; set; } = 0.5;
        /// <summary>
        /// 
        /// </summary>
        public int MaxQualityRetries { get; set; } = 3;
        /// <summary>
        /// 
        /// </summary>
        public ILLMModel CriticModel { get; }
        /// <summary>
        /// 
        /// </summary>
        public FiltrationConfig(
            ILLMModel criticModel)
        {
            CriticModel = criticModel;
        }
    }

    /// <summary>
    /// How many evolutions to apply and their relative probabilities.
    /// </summary>
    internal class EvolutionConfig
    {
        /// <summary>
        /// 
        /// </summary>
        public int NumEvolutions { get; set; } = 1;
        public Dictionary<Evolution, double> Evolutions { get; set; } = new()
        {
            { Evolution.Reasoning,    1.0 / 7 },
            { Evolution.MultiContext, 1.0 / 7 },
            { Evolution.Concretizing, 1.0 / 7 },
            { Evolution.Constrained,  1.0 / 7 },
            { Evolution.Comparative,  1.0 / 7 },
            { Evolution.Hypothetical, 1.0 / 7 },
            { Evolution.InBreadth,    1.0 / 7 },
        };
    }

    /// <summary>
    /// Optional styling hints for scenario, task, and formats.
    /// </summary>
    internal class StylingConfig
    {
        public string? Scenario { get; set; }
        public string? Task { get; set; }
        public string? InputFormat { get; set; }
        public string? ExpectedOutputFormat { get; set; }
    }

    internal enum Evolution
    {
        Reasoning,
        MultiContext,
        Concretizing,
        Constrained,
        Comparative,
        Hypothetical,
        InBreadth
    }
}
