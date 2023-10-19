using System.Runtime.InteropServices;
using Silk.NET.Shaderc;
using Silk.NET.SPIRV.Reflect;

namespace Licht.Vulkan.Pipelines;

public sealed unsafe class PipelineCompiler : IDisposable
{
    private readonly Shaderc _sc = Shaderc.GetApi();
    private readonly Reflect _reflect = Reflect.GetApi();

    private readonly VkGraphicsDevice _device;
    
    public PipelineCompiler(VkGraphicsDevice device) => _device = device;

    public byte[] CompileShaderBytes(string path)
    {
        var fileContents = File.ReadAllText(path);
        var shaderKind = ShaderKind.GlslInferFromSource;
        if (Path.GetExtension(path) == ".vert") shaderKind = ShaderKind.VertexShader;
        if (Path.GetExtension(path) == ".frag") shaderKind = ShaderKind.FragmentShader;
        if (Path.GetExtension(path) == ".comp") shaderKind = ShaderKind.ComputeShader;

        var compileOpts = _sc.CompileOptionsInitialize();
        _sc.CompileOptionsSetSourceLanguage(compileOpts, SourceLanguage.Glsl);
        _sc.CompileOptionsSetTargetSpirv(compileOpts, SpirvVersion.Shaderc13);
        
        var pCompiler = _sc.CompilerInitialize();
        var result = _sc.CompileIntoSpv(pCompiler, fileContents, (UIntPtr)fileContents.Length, shaderKind, path,
                                "main", compileOpts);
        
        _sc.CompileOptionsRelease(compileOpts);
        
        if (_sc.ResultGetNumErrors(result) > 0)
        {
            Console.WriteLine(_sc.ResultGetErrorMessageS(result));
        }

        var length = (uint) _sc.ResultGetLength(result);
        var bytes = new byte[length];
        var pBytes = _sc.ResultGetBytes(result);
        for (var i = 0; i < length; i++)
        {
            var ptr = (nint) (pBytes + (i * sizeof(byte)));
            bytes[i] = Marshal.PtrToStructure<byte>(ptr);
        }
        _sc.ResultRelease(result);
        _sc.CompilerRelease(pCompiler);

        return bytes;
    }

    public void Dispose()
    {
        _sc.Dispose();
        _reflect.Dispose();
    }
}