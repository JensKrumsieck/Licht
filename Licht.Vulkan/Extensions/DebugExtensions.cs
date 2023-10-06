using System.Diagnostics;
using System.Runtime.CompilerServices;
using Licht.Core;
using Silk.NET.Vulkan;

namespace Licht.Vulkan.Extensions;

public static unsafe class DebugExtensions
{
    public static LogLevel GetLogLevel(this DebugUtilsMessageSeverityFlagsEXT severityFlagsExt) => severityFlagsExt switch
    {
        DebugUtilsMessageSeverityFlagsEXT.None => LogLevel.Trace,
        DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt => LogLevel.Verbose,
        DebugUtilsMessageSeverityFlagsEXT.InfoBitExt => LogLevel.Info,
        DebugUtilsMessageSeverityFlagsEXT.WarningBitExt => LogLevel.Warn,
        DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt => LogLevel.Error,
        _ => LogLevel.Trace
    };

    [StackTraceHidden]
    public static void Validate(this Result result, [CallerArgumentExpression(nameof(result))] string operation = "")
    {
        if (result == Result.Success) VkContext.Logger?.Trace($"{operation} reported {result}!");
        else if ((int) result < 0)
        {
            var msg = $"{operation} failed: {result}";
            VkContext.Logger?.Error(msg);
            throw new ApplicationException(msg);
        }
        else VkContext.Logger?.Info($"{operation} reported {result}!");
    }
    
    [StackTraceHidden]
    internal static uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT severityFlags,
        DebugUtilsMessageTypeFlagsEXT messageTypeFlags,
        DebugUtilsMessengerCallbackDataEXT* pCallbackData,
        void* userData)
    {
        var message = new ByteString(pCallbackData->PMessage);
        var logMessage = $"{messageTypeFlags}: {message}";
        VkContext.Logger?.LogOutput(severityFlags.GetLogLevel(), logMessage);
        return Vk.False;
    }
}
