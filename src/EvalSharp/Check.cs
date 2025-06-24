namespace EvalSharp;

internal static class Check
{
    public static void IfNullOrEmptyOrStringsHaveNoValue(IEnumerable<string> strs, string paramName)
    {
        if (strs == null)
        {
            throw new ArgumentNullException(paramName);
        }

        if (!strs.Any())
        {
            throw new ArgumentException("Evaluation steps cannot be empty.", paramName);
        }

        if (strs.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Evaluation steps cannot contain null or whitespace strings.", paramName);
        }
    }

    public static void NullOrWhitespaceString(string? str, string paramName)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            throw new ArgumentException("String cannot be null or whitespace.", paramName);
        }
    }

    public static void NullOrWhitespaceString(string? str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            throw new ArgumentException("String cannot be null or whitespace.");
        }
    }

    public static void NotNull(object obj)
    {
        if (obj == null)
        {
            throw new ArgumentNullException();
        }
    }
}