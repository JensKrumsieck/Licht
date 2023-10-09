using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Licht.Core;
public class Logger : ILogger
{
    [StackTraceHidden]
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;
        if(state == null && exception == null) return;
        ArgumentNullException.ThrowIfNull(formatter);
        
        var defaultColor = Console.ForegroundColor;
        Console.ForegroundColor = logLevel switch
        {
            LogLevel.Trace => ConsoleColor.DarkGray,
            LogLevel.Debug => ConsoleColor.Blue,
            LogLevel.Information => ConsoleColor.Green,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Critical => ConsoleColor.DarkRed,
            _ => ConsoleColor.White
        };

        var message = formatter(state, exception);
        if (string.IsNullOrWhiteSpace(message)) message = string.Empty;

        var text = exception?.Message ?? message;
        Console.WriteLine("[{0}]: {1}", logLevel, text);
        Console.ForegroundColor = defaultColor;
    }
    
    [StackTraceHidden]
    public bool IsEnabled(LogLevel logLevel)
    {
        if (logLevel is LogLevel.Critical or LogLevel.Error) return true;
#if LWARN
        if (logLevel == LogLevel.Warning) return true;
#endif
#if LINFO
        if (logLevel == LogLevel.Information) return true;
#endif
#if DEBUG
#if LTRACE
        if (logLevel == LogLevel.Trace) return true;
#endif
#if LVERBOSE
        if (logLevel == LogLevel.Debug) return true;
#endif
#endif
        return false;
    }
    
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
}
