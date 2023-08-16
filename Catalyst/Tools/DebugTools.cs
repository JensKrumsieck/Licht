using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace Catalyst.Tools;

public static class DebugTools
{
    private static string SeverityFlagToString(this DebugUtilsMessageSeverityFlagsEXT severityFlags) => severityFlags switch
    {
        DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt => "Error",
        DebugUtilsMessageSeverityFlagsEXT.WarningBitExt => "Warning",
        _ => "Info"
    };

    public static void Validate(this Result result, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string function = "")
    {
        if (result == Result.Success) return;
        if (result < 0)
            throw new Exception($"[Vulkan] An error has occured: {result}\r\n\tat {function} in {file}:line {line}");
        Console.WriteLine($"[Vulkan] Vulkan did not report success: {result}");
    }

    public static unsafe uint DefaultDebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity,
                                                   DebugUtilsMessageTypeFlagsEXT messageTypes,
                                                   DebugUtilsMessengerCallbackDataEXT* pCallbackData,
                                                   void* pUserData)
    {
        using var message = new ByteString(pCallbackData->PMessage);
        Console.WriteLine($"[Vulkan] {messageSeverity}: {message}");
        return Vk.False;
    }
}