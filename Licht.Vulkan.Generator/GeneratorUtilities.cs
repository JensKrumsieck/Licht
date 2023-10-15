using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Licht.Vulkan.Generator;
public static class GeneratorUtilities
{
    //string utils
    private const string Indent = "    ";
    public static string Space(int spacers)
    {
        var res = new StringBuilder();
        for (var i = 0; i < spacers; i++)
            res.Append(Indent);
        return res.ToString();
    }

    public static string LowerCaseFirst(this string input) => input == "" ? "" : char.ToLower(input[0]) + input.Substring(1);

    //assembly utils
    public static void AppendExtensionLoad(this StringBuilder sb, bool deviceExtension, string apiName)
    {
        var ext = apiName.StartsWith("Khr") ? "KHR" : "EXT";
        sb.AppendLine($"{Space(3)}if(!vk.TryGet{(deviceExtension ? "Device" : "Instance")}Extension(_instance, {(deviceExtension ? "_device," : "")} out _{apiName.LowerCaseFirst()}))");
        sb.AppendLine($"{Space(4)}throw new ApplicationException($\"Could not get extension {{Silk.NET.Vulkan.Extensions.{ext}.{apiName}.ExtensionName}}\");");
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

    public static IAssemblySymbol LoadExtensionAssembly(this GeneratorExecutionContext context, string type)
    {
        var vk = context.Compilation.ExternalReferences.GetReferenceByString($"Silk.NET.Vulkan.Extensions.{type}.dll")!;
        return (IAssemblySymbol)context.Compilation.GetAssemblyOrModuleSymbol(vk)!;
    }
    public static PortableExecutableReference? GetReferenceByString(this ImmutableArray<MetadataReference> haystack, string needle)
    {
        return haystack.Where(s => s is PortableExecutableReference)
                        .Cast<PortableExecutableReference>()
                        .FirstOrDefault(
                                r => r.GetMetadata() is AssemblyMetadata asmMetaData
                                && asmMetaData.GetModules()[0].Name == needle);
    }

    //symbol utils
    public static IMethodSymbol? GetConstructor(this ITypeSymbol api, string typeName)
        => api.GetMembers($"Create{typeName}").Select(s => (IMethodSymbol)s).FirstOrDefault(s => !s.OriginalDefinition.ToDisplayString().Contains("*"));

    public static IEnumerable<IFieldSymbol> GetAllFields(this ITypeSymbol type) => type.GetMembers().Where(m => m is IFieldSymbol f && f.Kind == SymbolKind.Field).Cast<IFieldSymbol>();
    public static IEnumerable<IMethodSymbol> GetAllMethods(this ITypeSymbol type) => type.GetMembers().Where(m => m is IMethodSymbol && m.Kind == SymbolKind.Method).Cast<IMethodSymbol>();
    public static bool HasAttribute(this IFieldSymbol fieldSymbol, string fullQualifiedName) => fieldSymbol.GetAttributes().Any(ad => ad.AttributeClass!.ToDisplayString() == fullQualifiedName);
    public static List<string> BuildArgumentList(this IEnumerable<IParameterSymbol> parameters) => parameters.Select(s => (s.RefKind != RefKind.None ? s.RefKind.ToString().LowerCaseFirst() + " " : "") + s.Name).ToList();
    public static string RemoveNamespace(this ISymbol symbol, string @namespace) => symbol.ToDisplayString().Replace(@namespace + ".", "");
    public static IEnumerable<IGrouping<INamedTypeSymbol, IFieldSymbol>> GroupByContainingType(this IEnumerable<IFieldSymbol> fields) => fields.GroupBy<IFieldSymbol, INamedTypeSymbol>(f => f.ContainingType, SymbolEqualityComparer.Default);
    public static string ToNamespaceString(this ITypeSymbol symbol) => symbol.ContainingNamespace.ToDisplayString();
}
