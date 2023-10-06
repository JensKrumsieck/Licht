namespace Licht.Core;

public enum LogLevel
{
    /// <summary>
    /// Fatal error log level, should be used when application crashes
    /// </summary>
    Fatal,
    /// <summary>
    /// Error log level, should be used when application is in problematic state
    /// </summary>
    Error,
    /// <summary>
    /// Warning log level, should be used when application is in suboptimal state
    /// </summary>
    Warn,
    /// <summary>
    /// Info log level, should be used for informational non-error messages
    /// </summary>
    Info,
    /// <summary>
    /// Verbose log level, should be used for debugging
    /// </summary>
    Verbose,
    /// <summary>
    /// Trace log level, puts out all information
    /// </summary>
    Trace
}

public interface ILogger
{
    void LogOutput(LogLevel level, string message, params object[]? args);
    public void Fatal(string message, params object[]? args);
    public void Error(string message, params object[]? args);
    public void Warn(string message, params object[]? args);
    public void Info(string message, params object[]? args);
    public void Verbose(string message, params object[]? args);
    public void Trace(string message, params object[]? args);
    public void Assert(bool expr, string message = "", string file = "", int line = 0);
}
