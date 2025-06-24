using System.Text.Json;

namespace EvalSharp.Helpers
{
    internal static class JsonDataLoader
    {
        internal static IEnumerable<T> LoadJson<T>(string json, JsonSerializerOptions? jsonOptions = null)
        {
            return JsonSerializer.Deserialize<IEnumerable<T>>(json, jsonOptions)!;
        }

        internal static IEnumerable<T> LoadJsonLines<T>(string jsonLine, JsonSerializerOptions? jsonOptions = null)
        {
            var line = JsonSerializer.Deserialize<T>(jsonLine, jsonOptions);
            return line != null ? [line] : [];            
        }

        internal static IEnumerable<T> LoadJsonLines<T>(IEnumerable<string> jsonLines, JsonSerializerOptions? jsonOptions = null)
        {
            foreach (var line in jsonLines) 
            {
                yield return JsonSerializer.Deserialize<T>(line, jsonOptions)!;
            }
        }

        internal static async IAsyncEnumerable<T> LoadJsonFileAsync<T>(string filePath, JsonSerializerOptions? jsonOptions = null)
        {
            await using var fs = File.OpenRead(filePath);
            await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<T>(fs, jsonOptions))
            {
                if (item is not null)
                    yield return item;
            }
        }

        internal static IEnumerable<T> LoadJsonFile<T>(string filePath, JsonSerializerOptions? jsonOptions = null)
        {
            using var reader = new StreamReader(filePath);
            string json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<IEnumerable<T>>(json, jsonOptions)!;
        }
    }
}
