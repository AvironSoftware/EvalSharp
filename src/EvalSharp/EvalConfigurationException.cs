namespace EvalSharp;

/// <summary>
/// An exception that is thrown when there is a configuration error in EvalSharp.
/// </summary>
public class EvalConfigurationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EvalConfigurationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public EvalConfigurationException(string message) : base(message)
    {
    }
}