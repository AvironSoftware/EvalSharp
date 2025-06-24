namespace EvalSharp.Models
{
    internal class MetricMetadata(string name)
    {
        public string Name { get; set; } = name;
        public string Model { get; set; } = string.Empty;
        public bool? StrictMode { get; set; } = false;
        public double? Threshold { get; set; } = 0.5;
    }
}
