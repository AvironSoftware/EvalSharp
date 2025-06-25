namespace EvalSharp.Exceptions;

/// <summary>
/// An exception that is thrown when there is a configuration error in DeepEval.
/// </summary>
public class EvalFailException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EvalFailException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public EvalFailException(string message) : base(message)
    {
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var className = GetType().ToString();
        var message = Message;
        string result;

        if (message == null || message.Length <= 0)
            result = className;
        else
            result = $"{className}: {message}";

        var stackTrace = StackTrace;
        if (stackTrace != null)
            result = $"{result}{Environment.NewLine}{stackTrace}";

        return result;
    }
}