using System.Collections.Immutable;
using System.Diagnostics;
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
        var vk = context.Compilation.ExternalReferences.GetReferenceByString("Silk.NET.Vulkan.dll");
        var assembly = (IAssemblySymbol)context.Compilation.GetAssemblyOrModuleSymbol(vk)!;
        var api = assembly.GetTypeByMetadataName("Silk.NET.Vulkan.Vk");
        return api;
    }

    public static PortableExecutableReference GetReferenceByString(this ImmutableArray<MetadataReference> haystack, string needle)
    {
        return haystack.Where(s => s is PortableExecutableReference)
                        .Cast<PortableExecutableReference>()
                        .FirstOrDefault(
                                r => r.GetMetadata() is AssemblyMetadata asmMetaData 
                                && asmMetaData.GetModules()[0].Name == needle);
    }
}
