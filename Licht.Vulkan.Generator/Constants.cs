using System.Text;
using Microsoft.CodeAnalysis;

namespace Licht.Vulkan.Generator;
public static class Constants
{
    private const string Indent = "    ";
    public static string Space(int spacers)
    {
        var res = new StringBuilder();
        for (var i = 0; i < spacers; i++)
            res.Append(Indent);
        return res.ToString();
    }

    public static string LcF(string input) => char.ToLower(input[0]) + input.Substring(1);

    public static INamedTypeSymbol LoadVulkan(this GeneratorExecutionContext context)
    {
        //load vulkan assembly
        var references = context.Compilation.GetUsedAssemblyReferences();
        var vk = references.First(s => s.Display.Contains("Silk.NET.Vulkan.dll"));
        var asm = (IAssemblySymbol)context.Compilation.GetAssemblyOrModuleSymbol(vk);
        var api = asm.GetTypeByMetadataName("Silk.NET.Vulkan.Vk");
        return api;
    }
}
