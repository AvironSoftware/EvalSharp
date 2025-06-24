using CsvHelper.Configuration;
using System.Text.Json;
using EvalSharp.Helpers;

namespace EvalSharp
{
    /// <summary>
    /// Provides methods for deserializing JSON and CSV data into strongly typed objects.
    /// </summary>
    public static class EvalData
    {
        /// <summary>
        /// Deserializes JSON strings to type IEnumerable&lt;T&gt;.
        /// </summary>
        public static IEnumerable<T> FromJson<T>(string json, JsonSerializerOptions? jsonOptions = null) => JsonDataLoader.LoadJson<T>(json, jsonOptions);

        /// <summary>
        /// Reads JSON files and deserializes to type IEnumerable&lt;T&gt;.
        /// </summary>
        public static IEnumerable<T> FromJsonFile<T>(string filePath, JsonSerializerOptions? jsonOptions = null) => JsonDataLoader.LoadJsonFile<T>(filePath, jsonOptions);

        /// <summary>
        /// Asynchronously reads JSON files and deserializes to type IEnumerable&lt;T&gt;.
        /// Use this if you use System.Linq.Take() for a subset of the data.
        /// </summary>
        public static IAsyncEnumerable<T> FromJsonFileAsync<T>(string filePath, JsonSerializerOptions? jsonOptions = null) => JsonDataLoader.LoadJsonFileAsync<T>(filePath, jsonOptions);

        /// <summary>
        /// Deserializes a single JSON string to type IEnumerable&lt;T&gt;.
        /// </summary>
        public static IEnumerable<T> FromJsonL<T>(string jsonL, JsonSerializerOptions? jsonOptions = null) => JsonDataLoader.LoadJsonLines<T>(jsonL, jsonOptions);

        /// <summary>
        /// Deserializes a list of JSON strings to type IEnumerable&lt;T&gt;.
        /// </summary>
        public static IEnumerable<T> FromJsonL<T>(IEnumerable<string> jsonLines, JsonSerializerOptions? jsonOptions = null) => JsonDataLoader.LoadJsonLines<T>(jsonLines, jsonOptions);

        /// <summary>
        /// Parses a string in CSV format to type IEnumerable&lt;T&gt;.
        /// </summary>
        public static IEnumerable<T> FromCsv<T>(string csv, CsvConfiguration? csvConfiguration = null) => CsvDataLoader.LoadCsv<T>(csv, csvConfiguration);

        /// <summary>
        /// Reads a CSV file and parses to type IEnumerable&lt;T&gt;.
        /// </summary>
        public static IEnumerable<T> FromCsvFile<T>(string filePath, CsvConfiguration? csvConfiguration = null) => CsvDataLoader.LoadCsvFile<T>(filePath, csvConfiguration);
    }
}
