namespace EvalSharp.Synthesizer
{
    /// <summary>
    /// Defines a model that can convert text into vector embeddings.
    /// </summary>
    internal interface IEmbeddingModel
    {
        /// <summary>
        /// Converts a single text string into its embedding vector.
        /// </summary>
        /// <param name="text">The input text to embed.</param>
        /// <returns>A float array representing the embedding.</returns>
        float[] EmbedText(string text);

        /// <summary>
        /// Converts multiple text strings into their embedding vectors.
        /// </summary>
        /// <param name="texts">An enumerable of input texts.</param>
        /// <returns>A list of float arrays, each representing an embedding.</returns>
        List<float[]> EmbedTexts(IEnumerable<string> texts);
    }
}
