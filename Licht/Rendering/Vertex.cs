using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Licht.Vulkan.Extensions;
using Licht.Vulkan.Pipelines;
using Silk.NET.Vulkan;

namespace Licht.Rendering;

public struct Vertex
{
    public Vector3 Position;
    public Vector3 Color;
    public Vector3 Normal;
    public Vector2 TextureCoordinate;

    public static VertexInfo VertexInfo()
    {
        var descriptions = new VertexInputBindingDescription[1];
        descriptions[0] = new VertexInputBindingDescription
        {
            Binding = 0,
            Stride = (uint) Unsafe.SizeOf<Vertex>(),
            InputRate = VertexInputRate.Vertex
        };
        var attributes = new VertexInputAttributeDescription[4];
        attributes[0] = new VertexInputAttributeDescription
        {
            Binding = 0,
            Location = 0,
            Format = Format.R32G32B32Sfloat,
            Offset = (uint) Marshal.OffsetOf<Vertex>(nameof(Position))
        };
        attributes[1] = new VertexInputAttributeDescription
        {
            Binding = 0,
            Location = 1,
            Format = Format.R32G32B32Sfloat,
            Offset = (uint) Marshal.OffsetOf<Vertex>(nameof(Color))
        };
        attributes[2] = new VertexInputAttributeDescription
        {
            Binding = 0,
            Location = 2,
            Format = Format.R32G32B32Sfloat,
            Offset = (uint) Marshal.OffsetOf<Vertex>(nameof(Normal))
        };
        attributes[3] = new VertexInputAttributeDescription
        {
            Binding = 0,
            Location = 3,
            Format = Format.R32G32Sfloat,
            Offset = (uint) Marshal.OffsetOf<Vertex>(nameof(TextureCoordinate))
        };
        return new VertexInfo(descriptions, attributes);
    }
}