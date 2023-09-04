using Catalyze.Tools;
using Silk.NET.Vulkan;

namespace Catalyze;

public readonly unsafe struct DescriptorSet : IConvertibleTo<Silk.NET.Vulkan.DescriptorSet>
{
    public readonly Silk.NET.Vulkan.DescriptorSet VkDescriptorSet;
    public ulong Handle => VkDescriptorSet.Handle;
    
    public DescriptorSet(Device device, DescriptorPool pool, DescriptorSetLayout[] setLayouts)
    {
        var vkSetLayouts = setLayouts.AsArray<DescriptorSetLayout, Silk.NET.Vulkan.DescriptorSetLayout>();
        fixed (Silk.NET.Vulkan.DescriptorSetLayout* pSetLayouts = vkSetLayouts)
        {
            var allocInfo = new DescriptorSetAllocateInfo
            {
                SType = StructureType.DescriptorSetAllocateInfo,
                DescriptorPool = pool,
                DescriptorSetCount = 1,
                PSetLayouts = pSetLayouts
            };
            vk.AllocateDescriptorSets(device, allocInfo, out VkDescriptorSet).Validate();
        }
    }

    public static implicit operator Silk.NET.Vulkan.DescriptorSet(DescriptorSet set) => set.VkDescriptorSet;
    public Silk.NET.Vulkan.DescriptorSet Convert() => VkDescriptorSet;
}