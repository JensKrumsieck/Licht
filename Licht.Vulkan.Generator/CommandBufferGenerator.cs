﻿using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using static Licht.Vulkan.Generator.GeneratorUtilities;

namespace Licht.Vulkan.Generator;

[Generator]
public class CommandBufferGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context) { }
    public void Execute(GeneratorExecutionContext context) 
    {
        //load vulkan assembly
        var api = context.LoadVulkan();
        var type = "CommandBuffer";
        var typeLower = "commandBuffer";
        var sb = new StringBuilder($@"/// <auto-generated />
using Silk.NET.Vulkan;
namespace Licht.Vulkan
{{
    public unsafe readonly partial struct {type}
    {{
        private readonly Silk.NET.Vulkan.{type} _{typeLower};
        public readonly ulong Handle => (ulong) _{typeLower}.Handle;
        public static implicit operator Silk.NET.Vulkan.{type}({type} t) => t._{typeLower};
        public static implicit operator {type}(Silk.NET.Vulkan.{type} t) => new(t);
        public static implicit operator Silk.NET.Vulkan.{type}*({type} t) => &t._{typeLower};
        public {type} (Silk.NET.Vulkan.{type} vkStruct) => _{typeLower} = vkStruct;
");
        foreach (var symbol in api.GetAllMethods().Where(s => s.Name.StartsWith("Cmd")))
        {
            var parameters = symbol.Parameters.Skip(1).ToList(); //skip cmd parameter
            var args = parameters.BuildArgumentList().Select(s => s == "event" ? "@event" : s);
            var definitions = parameters.Select(s => s.ToDisplayString());
            var methodName = symbol.Name.Substring(3);
            var typeDefs = symbol.IsGenericMethod ? "<" + string.Join(", ", symbol.TypeParameters) + ">" : "";
            var wheres = "";
            foreach (var t in symbol.TypeParameters) wheres += $"where {t} : unmanaged,";
            if(symbol.TypeParameters.Length > 0) wheres = wheres.Substring(0, wheres.Length - 1);
            sb.AppendLine($"{Space(2)}public {symbol.ReturnType} {methodName}{typeDefs}({string.Join(", ", definitions)}) {wheres} => vk.{symbol.Name}{typeDefs}(this{(args.Count() > 0 ? ", " : "")}{string.Join(", ", args)});");
        }      

        sb.AppendLine($"{Space(1)}}}");
        sb.AppendLine("}");

        context.AddSource($"{type}_generated.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

}
