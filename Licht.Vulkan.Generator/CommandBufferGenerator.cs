using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using static Licht.Vulkan.Generator.Constants;

namespace Licht.Vulkan.Generator;

[Generator]
public class CommandBufferGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context) { }
    public void Execute(GeneratorExecutionContext context) 
    {
        //load vulkan assembly
        var api = context.LoadVulkan();
        var cmdMethods = api.GetMembers().Where(s => s.Name.StartsWith("Cmd")).Select(s => (IMethodSymbol)s);
        var type = "CommandBuffer";
        var sb = new StringBuilder($@"using Silk.NET.Vulkan;
namespace Licht.Vulkan
{{
    public unsafe readonly partial struct {type}
    {{
        private readonly Silk.NET.Vulkan.{type} _{LcF(type)};
        public readonly ulong Handle => (ulong) _{LcF(type)}.Handle;
        public static implicit operator Silk.NET.Vulkan.{type}({type} t) => t._{LcF(type)};
        public static implicit operator {type}(Silk.NET.Vulkan.{type} t) => new(t);
        public static implicit operator Silk.NET.Vulkan.{type}*({type} t) => &t._{LcF(type)};
        public {type} (Silk.NET.Vulkan.{type} vkStruct) => _{LcF(type)} = vkStruct;
");
        foreach(var symbol in cmdMethods)
        {
            var parameters = symbol.Parameters.Skip(1); //skip cmd parameter
            var args = parameters.Select(s => (s.RefKind != RefKind.None ? LcF(s.RefKind.ToString()) + " " : "") + s.Name).Select(s => s == "event" ? "@event" : s);
            var definitions = parameters.Select(s => s.OriginalDefinition.ToDisplayString());
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
