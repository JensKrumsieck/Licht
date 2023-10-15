using Licht.GraphicsCore;
using Silk.NET.Vulkan;

namespace Licht.Vulkan.Pipelines;

public readonly struct ShaderStage
{
    public readonly ShaderModule ShaderModule;
    public readonly ShaderStageFlags StageFlags;

    private ShaderStage(ShaderModule module, ShaderStageFlags stageFlags)
    {
        StageFlags = stageFlags;
        ShaderModule = module;
    }
    public static ShaderStage FromFile(VkGraphicsDevice device, string filename, ShaderStageFlags stageFlags) =>
        new(FromFile(device, filename), stageFlags);

    public static ShaderStage FromBytes(VkGraphicsDevice device, byte[] code, ShaderStageFlags stageFlags) =>
        new(FromBytes(device, code), stageFlags);
    public static ShaderStage FromUInts(VkGraphicsDevice device, uint[] code, ShaderStageFlags stageFlags) =>
        new(FromUInts(device, code), stageFlags);
    
    public PipelineShaderStageCreateInfo GetShaderStageCreateInfo() => new()
    {
        SType = StructureType.PipelineShaderStageCreateInfo,
        Stage = StageFlags,
        Module = ShaderModule,
        PName = new ByteString("main"),
        Flags = PipelineShaderStageCreateFlags.None
    };
    
    private static ShaderModule FromFile(VkGraphicsDevice device, string path)
    {
        var shaderCode = File.ReadAllBytes(path);
        return FromBytes(device, shaderCode);
    }
    private static unsafe ShaderModule FromUInts(VkGraphicsDevice device, uint[] shaderCode)
    {
        fixed (uint* pShaderCode = shaderCode)
        {
            var createInfo = new ShaderModuleCreateInfo
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint) shaderCode.Length * sizeof(uint),
                PCode = pShaderCode,
            };
            return new ShaderModule(device, createInfo);
        }
    }
    
    private static unsafe ShaderModule FromBytes(VkGraphicsDevice device, byte[] shaderBytes)
    {
        fixed (byte* pShaderCode = shaderBytes)
        {
            var createInfo = new ShaderModuleCreateInfo
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint) shaderBytes.Length,
                PCode = (uint*) pShaderCode,
            };
            return new ShaderModule(device, createInfo);
        }
    }
}
