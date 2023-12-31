﻿using Silk.NET.Vulkan;

namespace Licht.Vulkan.Pipelines;

public readonly struct VertexInfo
{
    public readonly VertexInputBindingDescription[]? BindingDescriptions;
    public readonly VertexInputAttributeDescription[]? AttributeDescriptions;
    public VertexInfo(VertexInputBindingDescription[] bindingDescriptions, VertexInputAttributeDescription[] attributeDescriptions)
    {
        BindingDescriptions = bindingDescriptions;
        AttributeDescriptions = attributeDescriptions;
    }
}
