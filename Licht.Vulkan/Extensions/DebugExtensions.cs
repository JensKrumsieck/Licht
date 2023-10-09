using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Silk.NET.Vulkan;

namespace Licht.Vulkan.Extensions;

public static unsafe class DebugExtensions
{
    public static LogLevel GetLogLevel(this DebugUtilsMessageSeverityFlagsEXT severityFlagsExt) => severityFlagsExt switch
    {
        DebugUtilsMessageSeverityFlagsEXT.None => LogLevel.Trace,
        DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt => LogLevel.Debug,
        DebugUtilsMessageSeverityFlagsEXT.InfoBitExt => LogLevel.Information,
        DebugUtilsMessageSeverityFlagsEXT.WarningBitExt => LogLevel.Warning,
        DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt => LogLevel.Error,
        _ => LogLevel.Trace
    };

    [StackTraceHidden]
    public static void Validate(this Result result, ILogger? logger = null, [CallerArgumentExpression(nameof(result))] string operation = "")
    {
        if (logger is null)
        {
            Console.WriteLine("{0} reported {1}!", operation, result);
            return;
        }
        
        if ((int) result < 0)
        {
            var msg = $"{operation} failed: {result}";
            logger.LogError(new ApplicationException(msg), msg);
            return;
        }
        logger.LogDebug("{Operation} reported {Result}!", operation, result);
    }
}
