using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using static Licht.Vulkan.Generator.Constants;

namespace Licht.Vulkan.Generator;

[Generator]
public class VulkanDisposableObjectsGenerator : ISourceGenerator
{
    

    private readonly string[] _types = 
    {
        "Instance",
        "Device",
        "CommandPool",
        "Buffer",
        "Sampler",
        "Image",
        "ImageView",
        "Fence",
        "Semaphore",
        "ShaderModule",
        "PipelineLayout",
        "PhysicalDevice",
        "SwapchainKHR"
    };
    
    public void Initialize(GeneratorInitializationContext context) { }

    public void Execute(GeneratorExecutionContext context)
    {
        //load vulkan assembly
        var api = context.LoadVulkan();

        //load khr extensions
        var vkKhr = context.Compilation.ExternalReferences.GetReferenceByString("Silk.NET.Vulkan.Extensions.KHR.dll");
        var asmKhr = (IAssemblySymbol)context.Compilation.GetAssemblyOrModuleSymbol(vkKhr)!;

        foreach (var type in _types)
        {
            var isKhrType = type.EndsWith("KHR");

            var khrType = "";
            if (isKhrType)
                khrType = $"Khr{type.Substring(0, type.Length - 3)}";

            //get api construction method without using pointers
            //will return null for some types like PhysicalDevice!
            var creationMethod = api.GetMembers($"Create{type}").Select(s => (IMethodSymbol)s).FirstOrDefault(s => !s.OriginalDefinition.ToDisplayString().Contains("*"));
            if(creationMethod is null)
            {
                if (isKhrType)
                {
                    var typeName = type.Substring(0, type.Length - 3);
                    var apiKhr = asmKhr.GetTypeByMetadataName($"Silk.NET.Vulkan.Extensions.KHR.{khrType}");
                    creationMethod = apiKhr.GetMembers($"Create{typeName}").Select(s => (IMethodSymbol)s).FirstOrDefault(s => !s.OriginalDefinition.ToDisplayString().Contains("*"));
                }
            }
            var sb = new StringBuilder($@"using Silk.NET.Vulkan;
namespace Licht.Vulkan
{{
    public unsafe readonly partial struct {type} {(creationMethod is not null ? ": IDisposable" : "")}
    {{
        private readonly Silk.NET.Vulkan.{type} _{LcF(type)};
        public readonly ulong Handle => (ulong) _{LcF(type)}.Handle;
        public static implicit operator Silk.NET.Vulkan.{type}({type} t) => t._{LcF(type)};
        public static implicit operator Silk.NET.Vulkan.{type}*({type} t) => &t._{LcF(type)};
");
            if (creationMethod is not null)
            {
                //we want allocation callbacks to be null, out parameter is created object
                var parameters = creationMethod.Parameters.Where(p => p.Type.Name != "AllocationCallbacks" && p.RefKind != RefKind.Out);
                var usesInstance = parameters.Any(p => p.Name == "instance");
                var usesDevice = parameters.Any(p => p.Name == "device"); ;
                if (usesInstance || (usesDevice && isKhrType)) sb.AppendLine($"{Space(2)}private readonly Instance _instance;");
                if (usesDevice) sb.AppendLine($"{Space(2)}private readonly Device _device;");
                if (isKhrType) sb.AppendLine($"{Space(2)}private readonly Silk.NET.Vulkan.Extensions.KHR.{khrType} _{LcF(khrType)};");
                var appendInstance = !usesInstance && usesDevice && isKhrType;
                sb.Append($@"{Space(2)}public {type} ({(appendInstance? "Instance instance, ":"")}{string.Join(", ", parameters.Select(s => ((int)s.RefKind > 1 ? LcF(s.RefKind.ToString()) + " " : "") + s.Type.Name + " " + s.Name))})
        {{
");
                if (appendInstance || usesInstance) sb.AppendLine($"{Space(3)}_instance = instance;");
                if (usesDevice) sb.AppendLine($"{Space(3)}_device = device;");
                if (isKhrType)
                {
                    sb.AppendLine($"{Space(3)}if(!vk.TryGet{(usesDevice ? "Device" : "Instance")}Extension(_instance, {(usesDevice ? "_device," : "")} out _{LcF(khrType)}))");
                    sb.AppendLine($"{Space(4)}throw new ApplicationException($\"Could not get extension {{Silk.NET.Vulkan.Extensions.KHR.{khrType}.ExtensionName}}\");");
                }
                sb.AppendLine($"{Space(3)}{(isKhrType ? $"_{LcF(khrType)}" : "vk")}.{creationMethod.Name}({string.Join(", ", parameters.Select(s => s.Name))}, null, out _{LcF(type)});");
                sb.AppendLine($"{Space(2)}}}");

                sb.AppendLine($"{Space(2)}public void Dispose()");
                sb.AppendLine($"{Space(2)}{{");
                var destroyArgs = (usesInstance ? "_instance, " : "") + (usesDevice ? "_device, ": "");
                sb.AppendLine($"{Space(3)}{(isKhrType ? $"_{LcF(khrType)}" : "vk")}.Destroy{(isKhrType ? type.Substring(0, type.Length - 3) : type)}({destroyArgs}_{LcF(type)}, null);");
                if(isKhrType) sb.AppendLine($"{Space(3)}_{LcF(khrType)}.Dispose();");
                sb.AppendLine($"{Space(2)}}}");
            }
            sb.Append(@"
    }
}");
            context.AddSource($"{type}_generated.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }        
    }
}