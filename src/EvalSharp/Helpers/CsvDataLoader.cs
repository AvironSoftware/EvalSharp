using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;


namespace EvalSharp.Helpers
{
    internal static class CsvDataLoader
    {
        internal static IEnumerable<T> LoadCsv<T>(string csvText, CsvConfiguration? config = null)
        {
            config ??= new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ","
            };

            using var reader = new StringReader(csvText);
            using var csv = new CsvReader(reader, config);

            foreach (var record in csv.GetRecords<T>())
            {
                yield return record;
            }
        }

        internal static IEnumerable<T> LoadCsvFile<T>(string filePath, CsvConfiguration? config = null)
        {
            config ??= new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ","
            };

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, config);

            foreach (var record in csv.GetRecords<T>())
            {
                yield return record;
            }
        }
    }
}
