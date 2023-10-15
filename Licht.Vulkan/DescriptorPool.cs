using Silk.NET.Vulkan;

namespace Licht.Vulkan;

public unsafe partial struct DescriptorPool
{ 
    public DescriptorSet AllocateDescriptorSet(DescriptorSetLayout setLayout)
    {
        var vkLayout = (Silk.NET.Vulkan.DescriptorSetLayout) setLayout;
        var allocInfo = new DescriptorSetAllocateInfo
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = this,
            DescriptorSetCount = 1,
            PSetLayouts = &vkLayout
        };
        vk.AllocateDescriptorSets(_device, allocInfo, out var descriptorSet);
        return descriptorSet;
    }
    public void FreeDescriptorSet(DescriptorSet set) => vk.FreeDescriptorSets(_device, this, 1, set);
}
