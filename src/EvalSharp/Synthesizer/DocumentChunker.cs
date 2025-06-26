using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EvalSharp.Synthesizer
{
    /// <summary>
    /// Splits documents into chunks and performs vector-based similarity.
    /// </summary>
    internal class DocumentChunker
    {
        private readonly IEmbeddingModel _embedder;

        public string? SourceFile { get; private set; }
        public List<string>? Sections { get; private set; }
        public List<string>? Chunks { get; private set; }
        private List<float[]>? _embeddings;

        public int TextTokenCount { get; private set; }

        private readonly Dictionary<string, Func<string, List<string>>> _loaderMapping;

        public DocumentChunker(IEmbeddingModel embedder)
        {
            _embedder = embedder;
            _loaderMapping = new Dictionary<string, Func<string, List<string>>>(StringComparer.OrdinalIgnoreCase)
            {
                [".txt"] = path => new List<string> { File.ReadAllText(path) },
                [".pdf"] = path => PdfLoader.LoadText(path),
                [".docx"] = path => DocxLoader.LoadText(path)
            };
        }

        /// <summary>
        /// Loads a document at the given path into text sections.
        /// </summary>
        public void LoadDocument(string path)
        {
            var ext = Path.GetExtension(path);
            if (!_loaderMapping.TryGetValue(ext, out var loader))
                throw new NotSupportedException($"Unsupported file format: {ext}");

            Sections = loader(path);
            SourceFile = path;
            TextTokenCount = Sections.Sum(s => Tokenize(s).Count);
        }

        /// <summary>
        /// Splits loaded sections into overlapping chunks and embeds them.
        /// </summary>
        public void ChunkDocument(int chunkSize = 1024, int chunkOverlap = 0)
        {
            if (Sections == null)
                throw new InvalidOperationException("Document not loaded.");

            // Tokenize all sections end-to-end
            var tokens = Sections.SelectMany(s => Tokenize(s)).ToList();
            var chunks = new List<string>();
            int step = chunkSize - chunkOverlap;
            for (int i = 0; i < tokens.Count; i += step)
            {
                var segment = tokens.Skip(i).Take(chunkSize);
                if (!segment.Any()) break;
                chunks.Add(string.Join(" ", segment));
                if (i + chunkSize >= tokens.Count) break;
            }

            Chunks = chunks;
            _embeddings = _embedder.EmbedTexts(Chunks);
        }

        /// <summary>
        /// Returns up to <paramref name="count"/> random chunks.
        /// </summary>
        public List<string> GetRandomChunks(int count)
        {
            if (Chunks == null)
                throw new InvalidOperationException("Chunks not generated.");

            var rng = new Random();
            return Chunks.OrderBy(_ => rng.Next()).Take(count).ToList();
        }

        /// <summary>
        /// Retrieves chunks whose embedding cosine similarity to the given chunk exceeds threshold.
        /// </summary>
        public List<string> GetSimilarChunks(string chunk, double similarityThreshold, int maxResults)
        {
            if (Chunks == null || _embeddings == null)
                throw new InvalidOperationException("Chunks not generated.");

            var queryEmbedding = _embedder.EmbedText(chunk);
            var sims = _embeddings
                .Select((vec, idx) => (Index: idx, Score: CosineSimilarity(queryEmbedding, vec)))
                .Where(x => x.Score > similarityThreshold)
                .OrderByDescending(x => x.Score)
                .Take(maxResults)
                .Select(x => Chunks[x.Index])
                .ToList();

            return sims;
        }

        private static List<string> Tokenize(string text)
        {
            // Split on whitespace by passing an empty separator array
            return text.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        private static double CosineSimilarity(float[] a, float[] b)
        {
            double dot = 0, da = 0, db = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                da += a[i] * a[i];
                db += b[i] * b[i];
            }
            return dot / (Math.Sqrt(da) * Math.Sqrt(db));
        }
    }

    // Stubs for external loaders
    internal static class PdfLoader
    {
        public static List<string> LoadText(string path)
        {
            // TODO: implement PDF text extraction
            throw new NotImplementedException();
        }
    }

    internal static class DocxLoader
    {
        public static List<string> LoadText(string path)
        {
            // TODO: implement DOCX text extraction
            throw new NotImplementedException();
        }
    }
}
