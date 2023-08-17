using Catalyst.Tools;
using Silk.NET.Vulkan;

namespace Catalyst;

public readonly unsafe struct DescriptorPool : IDisposable, IConvertibleTo<Silk.NET.Vulkan.DescriptorPool>
{
    private readonly Device _device;
    
    public readonly Silk.NET.Vulkan.DescriptorPool VkDescriptorPool;
    
    public DescriptorPool(Device device, DescriptorPoolSize[] poolSizes, uint maxSets = 1)
    {
        _device = device;
        fixed (DescriptorPoolSize* pPoolSizes = poolSizes)
        {
            var createInfo = new DescriptorPoolCreateInfo
            {
                SType = StructureType.DescriptorPoolCreateInfo,
                PoolSizeCount = (uint) poolSizes.Length,
                PPoolSizes = pPoolSizes,
                MaxSets = maxSets,
                Flags = DescriptorPoolCreateFlags.FreeDescriptorSetBit
            };
            vk.CreateDescriptorPool(_device, createInfo, null, out VkDescriptorPool).Validate();
        }
    }

    public DescriptorSet AllocateDescriptorSet(DescriptorSetLayout[] setLayouts) => new(_device, this, setLayouts);

    public void FreeDescriptorSets(DescriptorSet[] sets) =>
        vk.FreeDescriptorSets(_device, VkDescriptorPool, (uint) sets.Length,
                              sets.AsArray<DescriptorSet, Silk.NET.Vulkan.DescriptorSet>());

    public void FreeDescriptorSet(DescriptorSet set) => FreeDescriptorSets(new[] {set});
    public static implicit operator Silk.NET.Vulkan.DescriptorPool(DescriptorPool d) => d.VkDescriptorPool;
    public void ResetPool() => vk.ResetDescriptorPool(_device, VkDescriptorPool, 0);
    public Silk.NET.Vulkan.DescriptorPool Convert() => VkDescriptorPool;
    public void Dispose() => vk.DestroyDescriptorPool(_device, VkDescriptorPool, null);
    
}