namespace EvalSharp.Models.Enums
{
    /// <summary>
    /// Defines the modes of matching supported by MatchMetric.
    /// </summary>
    internal enum MatchMetricMode
    {
        /// <summary>
        /// Exact matching mode.
        /// </summary>
        Exact,

        /// <summary>
        /// Regex-based matching mode.
        /// </summary>
        Regex,

        /// <summary>
        /// Matching text after a specific string occurrence.
        /// </summary>
        AfterOccurrenceOfString
    }
}
