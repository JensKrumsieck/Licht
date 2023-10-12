using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Licht.Vulkan.Generator;
public static class GeneratorUtilities
{
    private const string Indent = "    ";
    public static string Space(int spacers)
    {
        var res = new StringBuilder();
        for (var i = 0; i < spacers; i++)
            res.Append(Indent);
        return res.ToString();
    }
    
    public static string LowerCaseFirst(this string input) => char.ToLower(input[0]) + input.Substring(1);

    public static void AppendExtensionLoad(this StringBuilder sb, bool deviceExtension, string apiName)
    {
        sb.AppendLine($"{Space(3)}if(!vk.TryGet{(deviceExtension ? "Device" : "Instance")}Extension(_instance, {(deviceExtension ? "_device," : "")} out _{apiName.LowerCaseFirst()}))");
        sb.AppendLine($"{Space(4)}throw new ApplicationException($\"Could not get extension {{Silk.NET.Vulkan.Extensions.KHR.{apiName}.ExtensionName}}\");");
    }

    public static void AppendDependencies(this StringBuilder sb, bool useInstance, bool useDevice)
    {
        if(useInstance) sb.AppendLine($"{Space(3)}_instance = instance;");
        if(useDevice) sb.AppendLine($"{Space(3)}_device = device;");
    }
    
    public static INamedTypeSymbol LoadVulkan(this GeneratorExecutionContext context)
    {
        var vk = context.Compilation.ExternalReferences.GetReferenceByString("Silk.NET.Vulkan.dll")!;
        var assembly = (IAssemblySymbol)context.Compilation.GetAssemblyOrModuleSymbol(vk)!;
        var api = assembly.GetTypeByMetadataName("Silk.NET.Vulkan.Vk");
        return api!;
    }

    public static IAssemblySymbol LoadKhrAssembly(this GeneratorExecutionContext context)
    {
        var vk = context.Compilation.ExternalReferences.GetReferenceByString("Silk.NET.Vulkan.Extensions.KHR.dll")!;
        return (IAssemblySymbol)context.Compilation.GetAssemblyOrModuleSymbol(vk)!;
    }

    public static IMethodSymbol? GetConstructor(this INamedTypeSymbol api, string typeName)
        => api.GetMembers($"Create{typeName}").Select(s => (IMethodSymbol)s).FirstOrDefault(s => !s.OriginalDefinition.ToDisplayString().Contains("*"));

    public static PortableExecutableReference? GetReferenceByString(this ImmutableArray<MetadataReference> haystack, string needle)
    {
        return haystack.Where(s => s is PortableExecutableReference)
                        .Cast<PortableExecutableReference>()
                        .FirstOrDefault(
                                r => r.GetMetadata() is AssemblyMetadata asmMetaData 
                                && asmMetaData.GetModules()[0].Name == needle);
    }
}
