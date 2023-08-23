using Catalyst.Tools;
using Silk.NET.Vulkan;

namespace Catalyst.Pipeline;

public readonly struct ShaderStage
{
    public readonly ShaderModule ShaderModule;
    public readonly ShaderStageFlags StageFlags;
    private ShaderStage(ShaderModule module, ShaderStageFlags stageFlags)
    {
        ShaderModule = module;
        StageFlags = stageFlags;
    }

    public static ShaderStage FromFile(Device device, string filename, ShaderStageFlags stageFlags) =>
        new(ShaderModule.FromSpvFile(device, filename), stageFlags);

    public static ShaderStage FromBytes(Device device, byte[] code, ShaderStageFlags stageFlags) =>
        new(ShaderModule.FromBytes(device, code), stageFlags);
    public static ShaderStage FromUInts(Device device, uint[] code, ShaderStageFlags stageFlags) =>
        new(ShaderModule.FromUInts(device, code), stageFlags);

    public PipelineShaderStageCreateInfo GetShaderStageCreateInfo() => new()
    {
        SType = StructureType.PipelineShaderStageCreateInfo,
        Stage = StageFlags,
        Module = ShaderModule.VkShaderModule,
        PName = new ByteString("main"),
        Flags = PipelineShaderStageCreateFlags.None
    };
}
public readonly struct ShaderModule : IConvertibleTo<Silk.NET.Vulkan.ShaderModule>
{
    public readonly Silk.NET.Vulkan.ShaderModule VkShaderModule;
    public readonly uint[] Code;

    private ShaderModule(Silk.NET.Vulkan.ShaderModule shaderModule, uint[] code)
    {
        VkShaderModule = shaderModule;
        Code = code;
    }
    public static implicit operator Silk.NET.Vulkan.ShaderModule(ShaderModule m) => m.VkShaderModule;
    
    public static ShaderModule FromSpvFile(Device device, string path)
    {
        var shaderCode = File.ReadAllBytes(path);
        return FromBytes(device, shaderCode);
    }

    public static unsafe ShaderModule FromUInts(Device device, uint[] shaderCode)
    {
        fixed (uint* pShaderCode = shaderCode)
        {
            var createInfo = new ShaderModuleCreateInfo
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint) shaderCode.Length * sizeof(uint),
                PCode = pShaderCode,
            };
            vk.CreateShaderModule(device, createInfo, null, out var module);
            return new ShaderModule(module, shaderCode);
        }
    }
    public Silk.NET.Vulkan.ShaderModule Convert() => VkShaderModule;
    
    public static unsafe ShaderModule FromBytes(Device device, byte[] shaderCode)
    {
        fixed (byte* pShaderCode = shaderCode)
        {
            var createInfo = new ShaderModuleCreateInfo
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint) shaderCode.Length,
                PCode = (uint*)pShaderCode,
            };
            vk.CreateShaderModule(device, createInfo, null, out var module);
            return new ShaderModule(module, Array.ConvertAll(shaderCode, (b) => (uint) b));
        }
    }
}
