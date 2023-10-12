using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using static Licht.Vulkan.Generator.GeneratorUtilities;

namespace Licht.Vulkan.Generator;

[Generator]
public class VulkanDisposableObjectsGenerator : ISourceGenerator
{
    

    private readonly string[] _types = 
    {
        "Instance",
        "Device",
        "CommandPool",
        "Framebuffer",
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
        var khrAsm = context.LoadKhrAssembly();
        
        foreach (var type in _types)
        {
            //if the type ends with "KHR" its from KHR Assembly
            //we will need KhrExtension class to handle it instead of Vk
            //e.g. SurfaceKHR => KhrSurface
            var isKhrType = type.EndsWith("KHR");
            var khrTypeName = type.Substring(0, type.Length - 3);
            var khrExtensionApi = isKhrType ? $"Khr{khrTypeName}" : "";
            //get api construction method without using pointers
            //will return null for some types like PhysicalDevice!
            var constructor = api.GetConstructor(type);
            if(constructor is null)
            {
                if (isKhrType)
                {
                    var apiKhr = khrAsm.GetTypeByMetadataName($"Silk.NET.Vulkan.Extensions.KHR.{khrExtensionApi}")!;
                    constructor = apiKhr.GetConstructor(khrTypeName);
                }
            }
            var sb = new StringBuilder($@"using Silk.NET.Vulkan;
namespace Licht.Vulkan
{{
    public unsafe readonly partial struct {type} {(constructor is not null ? ": IDisposable" : "")}
    {{
        private readonly Silk.NET.Vulkan.{type} _{type.LowerCaseFirst()};
        public readonly ulong Handle => (ulong) _{type.LowerCaseFirst()}.Handle;
        public static implicit operator Silk.NET.Vulkan.{type}({type} t) => t._{type.LowerCaseFirst()};
        public static implicit operator Silk.NET.Vulkan.{type}*({type} t) => &t._{type.LowerCaseFirst()};
");
            if (constructor is not null)
            {
                //we want allocation callbacks to be null, out parameter is created object
                var parameters = constructor.Parameters.Where(p => p.Type.Name != "AllocationCallbacks" && p.RefKind != RefKind.Out).ToList();
                var usesInstance = parameters.Any(p => p.Name == "instance");
                var usesDevice = parameters.Any(p => p.Name == "device"); ;
                if (usesInstance || (usesDevice && isKhrType)) sb.AppendLine($"{Space(2)}private readonly Instance _instance;");
                if (usesDevice) sb.AppendLine($"{Space(2)}private readonly Device _device;");
                if (isKhrType) sb.AppendLine($"{Space(2)}private readonly Silk.NET.Vulkan.Extensions.KHR.{khrExtensionApi} _{khrExtensionApi.LowerCaseFirst()};");
                var appendInstance = !usesInstance && usesDevice && isKhrType;
                
                //ctor
                sb.Append($@"{Space(2)}public {type} ({(appendInstance? "Instance instance, ":"")}{string.Join(", ", parameters.Select(s => ((int)s.RefKind > 1 ? s.RefKind.ToString().LowerCaseFirst() + " " : "") + s.Type.Name + " " + s.Name))})
        {{
");
                sb.AppendDependencies(appendInstance || usesInstance, usesDevice);
                if (isKhrType) sb.AppendExtensionLoad(usesDevice, khrExtensionApi);
                sb.AppendLine($"{Space(3)}{(isKhrType ? $"_{khrExtensionApi.LowerCaseFirst()}" : "vk")}.{constructor.Name}({string.Join(", ", parameters.Select(s => s.Name))}, null, out _{type.LowerCaseFirst()});");
                sb.AppendLine($"{Space(2)}}}");
                
                //copy ctor
                sb.AppendLine($"{Space(2)}public {type} ({(appendInstance || usesInstance ? "Instance instance, " : "")}{(usesDevice ? "Device device, " : "")}Silk.NET.Vulkan.{type} {type.LowerCaseFirst()})");
                sb.AppendLine($"{Space(2)}{{");
                sb.AppendDependencies(appendInstance || usesInstance, usesDevice);
                sb.AppendLine($"{Space(3)}_{type.LowerCaseFirst()} = {type.LowerCaseFirst()};");
                if (isKhrType) sb.AppendExtensionLoad(usesDevice, khrExtensionApi);
                sb.AppendLine($"{Space(2)}}}");
                
                //dispose
                sb.AppendLine($"{Space(2)}public void Dispose()");
                sb.AppendLine($"{Space(2)}{{");
                var destroyArgs = (usesInstance ? "_instance, " : "") + (usesDevice ? "_device, ": "");
                sb.AppendLine($"{Space(3)}{(isKhrType ? $"_{khrExtensionApi.LowerCaseFirst()}" : "vk")}.Destroy{(isKhrType ? type.Substring(0, type.Length - 3) : type)}({destroyArgs}_{type.LowerCaseFirst()}, null);");
                if(isKhrType) sb.AppendLine($"{Space(3)}_{khrExtensionApi.LowerCaseFirst()}.Dispose();");
                sb.AppendLine($"{Space(2)}}}");
            }
            sb.Append(@"
    }
}");
            context.AddSource($"{type}_generated.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }        
    }
}