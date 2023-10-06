using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Licht.Core;
public class Logger : ILogger
{

    [StackTraceHidden]
    public void LogOutput(LogLevel level, string message, params object[]? args)
    {
        var logMessage = args is null ? message : string.Format(message, args);
        var levelStr = $"[{level}]:";

        var defaultColor = Console.ForegroundColor;
        Console.ForegroundColor = level switch
        {
            LogLevel.Fatal => ConsoleColor.DarkRed,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Warn => ConsoleColor.Yellow,
            LogLevel.Info => ConsoleColor.Green,
            LogLevel.Verbose => ConsoleColor.Blue,
            LogLevel.Trace => ConsoleColor.DarkGray,
            _ => defaultColor
        };

        var outString = $"{levelStr} {logMessage}";
        if (level < (LogLevel) 2)
        {
            Console.Error.WriteLine(outString);
#if !DEBUG
            Environment.Exit(-1);
#endif
            throw new ApplicationException($"Application shuts down due this error:\n{logMessage}");
        }

        Console.WriteLine(outString);

        Console.ForegroundColor = defaultColor;
    }

    [StackTraceHidden]
    public void Fatal(string message, params object[]? args) => LogOutput(LogLevel.Fatal, message, args);
    
    [StackTraceHidden]
    public void Error(string message, params object[]? args) => LogOutput(LogLevel.Error, message, args);
    
    [StackTraceHidden]
    public void Warn(string message, params object[]? args)
    {
#if LWARN
        LogOutput(LogLevel.Warn, message, args);
#endif
    }
    
    [StackTraceHidden]
    public void Info(string message, params object[]? args)
    {
#if LINFO
        LogOutput(LogLevel.Info, message, args);
#endif
    }
    
    [StackTraceHidden]
    public void Verbose(string message, params object[]? args)
    {
#if DEBUG && LVERBOSE
        LogOutput(LogLevel.Verbose, message, args);
#endif
    }
    
    [StackTraceHidden]
    public void Trace(string message, params object[]? args)
    {
#if DEBUG && LTRACE
        LogOutput(LogLevel.Trace, message, args);
#endif
    }
    
    [StackTraceHidden]
    public void Assert([DoesNotReturnIf(true)] bool expr, [CallerArgumentExpression(nameof(expr))] string message = "", [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
#if DEBUG
        if (!expr)
        {
            if (message != "") message = " \"" + message + "\" ";
            Fatal($"Assertion{message}failed in file: {file} at line {line}");
        }
#endif
    }
}
