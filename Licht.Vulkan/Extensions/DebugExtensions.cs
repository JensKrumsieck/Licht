using System.Diagnostics;
using System.Runtime.CompilerServices;
using Licht.Core;
using Microsoft.Extensions.Logging;
using Silk.NET.Vulkan;

namespace Licht.Vulkan.Extensions;

public static unsafe class DebugExtensions
{
    private static LogLevel GetLogLevel(this DebugUtilsMessageSeverityFlagsEXT severityFlagsExt) => severityFlagsExt switch
    {
        DebugUtilsMessageSeverityFlagsEXT.None => LogLevel.Trace,
        DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt => LogLevel.Debug,
        DebugUtilsMessageSeverityFlagsEXT.InfoBitExt => LogLevel.Information,
        DebugUtilsMessageSeverityFlagsEXT.WarningBitExt => LogLevel.Warning,
        DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt => LogLevel.Error,
        _ => LogLevel.Trace
    };

    [StackTraceHidden]
    public static void Validate(this Result result, [CallerArgumentExpression(nameof(result))] string operation = "")
    {
        if ((int) result < 0)
        {
            var msg = $"{operation} failed: {result}";
            VulkanDevice.Logger?.LogError(new ApplicationException(msg), msg);
        }
        VulkanDevice.Logger?.LogDebug("{Operation} reported {Result}!", operation, result);
    }
    
    [StackTraceHidden]
    internal static uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT severityFlags,
        DebugUtilsMessageTypeFlagsEXT messageTypeFlags,
        DebugUtilsMessengerCallbackDataEXT* pCallbackData,
        void* userData)
    {
        var message = new ByteString(pCallbackData->PMessage);
        VulkanDevice.Logger?.Log(severityFlags.GetLogLevel(), "{MessageTypeFlags}: {Message}", messageTypeFlags, message);
        return Vk.False;
    }
}
