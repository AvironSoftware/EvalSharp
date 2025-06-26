using EvalSharp.Models; // For ILLMModel
using System;
using System.Collections.Generic;
using System.Linq;

namespace EvalSharp.Synthesizer
{
    /// <summary>
    /// Builds contexts from documents by chunking and semantic similarity,
    /// then filtering via a critic LLM.
    /// </summary>
    internal class ContextGenerator
    {
        private readonly IEmbeddingModel _embedder;
        private readonly ILLMModel _criticModel;
        private readonly int _chunkSize;
        private readonly int _chunkOverlap;
        private readonly double _filterThreshold;
        private readonly double _similarityThreshold;
        private readonly int _maxRetries;

        private readonly Dictionary<string, DocumentChunker> _docChunkers = new();

        public ContextGenerator(
            IEmbeddingModel embedder,
            ILLMModel criticModel,
            int chunkSize = 1024,
            int chunkOverlap = 0,
            double filterThreshold = 0.5,
            double similarityThreshold = 0.5,
            int maxRetries = 3)
        {
            _embedder = embedder;
            _criticModel = criticModel;
            _chunkSize = chunkSize;
            _chunkOverlap = chunkOverlap;
            _filterThreshold = filterThreshold;
            _similarityThreshold = similarityThreshold;
            _maxRetries = maxRetries;
        }

        /// <summary>
        /// Loads and chunks each document at the given paths.
        /// </summary>
        public void LoadDocs(IEnumerable<string> documentPaths)
        {
            foreach (var path in documentPaths)
            {
                var chunker = new DocumentChunker(_embedder);
                chunker.LoadDocument(path);
                chunker.ChunkDocument(_chunkSize, _chunkOverlap);
                _docChunkers[path] = chunker;
            }
        }

        /// <summary>
        /// Generates contexts for each document by sampling random chunks,
        /// expanding with similar chunks, then filtering by context quality.
        /// </summary>
        public (List<List<string>> Contexts, List<string> SourceFiles, List<double> Scores)
            GenerateContexts(int maxContextsPerDocument, int maxContextSize)
        {
            if (!_docChunkers.Any())
                throw new InvalidOperationException("No documents loaded. Call LoadDocs first.");

            var contexts = new List<List<string>>();
            var sourceFiles = new List<string>();
            var scores = new List<double>();

            foreach (var kv in _docChunkers)
            {
                string path = kv.Key;
                var chunker = kv.Value;

                // 1. Sample random chunks
                var randomChunks = chunker.GetRandomChunks(maxContextsPerDocument);

                foreach (var chunk in randomChunks)
                {
                    // 2. Retrieve semantically similar chunks
                    var similar = chunker.GetSimilarChunks(chunk, _similarityThreshold, maxContextSize - 1);

                    // 3. Build context (lead chunk + similar)
                    var context = new List<string> { chunk };
                    context.AddRange(similar);

                    // 4. Evaluate context quality
                    var evalPrompt = FilterTemplate.EvaluateContext(context);
                    var feedback = GenerateSchema<InputFeedback>(evalPrompt);

                    if (feedback.Score >= _filterThreshold)
                    {
                        contexts.Add(context);
                        sourceFiles.Add(path);
                        scores.Add(feedback.Score);
                    }
                }
            }

            return (contexts, sourceFiles, scores);
        }

        // Note: Async variants and retry logic can be added similarly if needed.

        // Helper to parse JSON feedback
        private T GenerateSchema<T>(string prompt)
        {
            var json = _criticModel.Generate(prompt);
            return System.Text.Json.JsonSerializer.Deserialize<T>(json, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })!;
        }
    }
}
