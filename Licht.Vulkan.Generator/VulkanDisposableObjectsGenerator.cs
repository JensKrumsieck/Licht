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
        "SwapchainKHR",
        "DebugUtilsMessengerEXT"
    };
    
    public void Initialize(GeneratorInitializationContext context) { }

    public void Execute(GeneratorExecutionContext context)
    {
        //load vulkan assembly
        var api = context.LoadVulkan();
        var khrAsm = context.LoadExtensionAssembly("KHR");
        var extAsm = context.LoadExtensionAssembly("EXT");

        foreach (var type in _types)
        {
            var typeLower = type.LowerCaseFirst();
            
            var isKhr = type.EndsWith("KHR");
            var isExt = type.EndsWith("EXT");
            var isExtension = isExt || isKhr;
            var extensionAsm = isKhr ? khrAsm : extAsm;

            var apiRepresentation = isExtension ? type.Substring(0, type.Length - 3) : type;
            var extensionApi = type == "DebugUtilsMessengerEXT" 
                                                ? "ExtDebugUtils" 
                                                : isKhr 
                                                    ? $"Khr{apiRepresentation}" 
                                                    : isExt 
                                                        ? $"Ext{apiRepresentation}" 
                                                        : "";
            var ext = isKhr ? "KHR" : isExt ? "EXT" : "";

            //try to get construction method
            var usedApi = isExtension ? extensionAsm.GetTypeByMetadataName($"Silk.NET.Vulkan.Extensions.{ext}.{extensionApi}")! : api;
            var constructor = usedApi.GetConstructor(apiRepresentation);
            
            var (deviceSlot, instanceSlot, apiSlot, ctorSlot, copyCtorSlot, methodSlot) = FillSlots(type, constructor, isExtension, ext, extensionApi, apiRepresentation);
           
            //language=csharp
            var template = $@"using Silk.NET.Vulkan;
namespace Licht.Vulkan
{{
    public unsafe readonly partial struct {type} {(constructor is not null ? ": IDisposable" : "")}
    {{
        private readonly Silk.NET.Vulkan.{type} _{typeLower};
        public readonly ulong Handle => (ulong) _{typeLower}.Handle;
        public static implicit operator Silk.NET.Vulkan.{type}({type} t) => t._{typeLower};
        public static implicit operator Silk.NET.Vulkan.{type}*({type} t) => &t._{typeLower};
{deviceSlot}
{instanceSlot}
{apiSlot}
{ctorSlot}
{copyCtorSlot}
{methodSlot}
    }}
}}
";
            context.AddSource($"{type}_generated.g.cs", SourceText.From(template, Encoding.UTF8));
        }
    }

    private (string deviceSlot, 
        string instanceSlot, 
        string apiSlot, 
        string ctorSlot, 
        string copyCtorSlot, 
        string methodSlot) 
        FillSlots(string type, 
            IMethodSymbol? constructor, 
            bool isExtension, 
            string ext = "", 
            string extensionApi = "", 
            string apiRepresentation = "")
    {
        if (constructor is null) return ("", "", "", "", "", "");
        
        var extensionApiLower = extensionApi.LowerCaseFirst();
        var typeLower = type.LowerCaseFirst();
        var parameters = constructor.Parameters
            .Where(p => p.Type.Name != "AllocationCallbacks" && p.RefKind != RefKind.Out).ToList();
        var usesInstance = parameters.Any(p => p.Name == "instance");
        var usesDevice = parameters.Any(p => p.Name == "device");

        var deviceSlot = usesDevice ? $"{Space(2)}private readonly Device _device;" : "";
        var instanceSlot = usesInstance || usesDevice && isExtension ? $"{Space(2)}private readonly Instance _instance;" : "";
        var apiSlot = isExtension ? $"{Space(2)}private readonly Silk.NET.Vulkan.Extensions.{ext}.{extensionApi} _{extensionApiLower};" : "";
              
        //as this is needed for Extension-TryGet Methods but not used in construction method of object
        var appendInstanceInCtor = !usesInstance && usesDevice && isExtension;

        //ctor
        var ctorBuilder = new StringBuilder($"{Space(2)}public {type} ({(appendInstanceInCtor ? "Instance instance, " : "")}{string.Join(", ", parameters.Select(s => ((int) s.RefKind > 1 ? s.RefKind.ToString().LowerCaseFirst() + " " : "") + s.Type.Name + " " + s.Name))})");
        ctorBuilder.AppendLine($"n{Space(2)}{{");
        ctorBuilder.AppendDependencies(appendInstanceInCtor || usesInstance, usesDevice);
        if (isExtension) ctorBuilder.AppendExtensionLoad(usesDevice, extensionApi);
        ctorBuilder.AppendLine($"{Space(3)}{(isExtension ? $"_{extensionApiLower}" : "vk")}.{constructor.Name}({string.Join(", ", parameters.Select(s => s.Name))}, null, out _{typeLower});");
        ctorBuilder.AppendLine($"{Space(2)}}}");
        var ctorSlot = ctorBuilder.ToString();
                
        //copy ctor
        var copyCtorBuilder = new StringBuilder($"{Space(2)}public {type} ({(appendInstanceInCtor || usesInstance ? "Instance instance, " : "")}{(usesDevice ? "Device device, " : "")}Silk.NET.Vulkan.{type} {typeLower})");
        copyCtorBuilder.AppendLine($"\n{Space(2)}{{");
        copyCtorBuilder.AppendDependencies(appendInstanceInCtor || usesInstance, usesDevice);
        copyCtorBuilder.AppendLine($"{Space(3)}_{typeLower} = {typeLower};");
        if (isExtension) copyCtorBuilder.AppendExtensionLoad(usesDevice, extensionApi);
        copyCtorBuilder.AppendLine($"{Space(2)}}}");
        var copyCtorSlot = copyCtorBuilder.ToString();
                
        //dispose
        var disposeBuilder = new StringBuilder($"{Space(2)}public void Dispose()");
        disposeBuilder.AppendLine($"\n{Space(2)}{{");
        var destroyArgs = (usesInstance ? "_instance, " : "") + (usesDevice ? "_device, ": "");
        disposeBuilder.AppendLine($"{Space(3)}{(isExtension ? $"_{extensionApiLower}" : "vk")}.Destroy{(isExtension ? apiRepresentation : type)}({destroyArgs}_{typeLower}, null);");
        if(isExtension) disposeBuilder.AppendLine($"{Space(3)}_{extensionApiLower}.Dispose();");
        disposeBuilder.AppendLine($"{Space(2)}}}");
        var methodSlot = disposeBuilder.ToString();

        return (deviceSlot, instanceSlot, apiSlot, ctorSlot, copyCtorSlot, methodSlot);
    }
}