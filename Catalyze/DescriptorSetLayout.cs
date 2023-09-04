using Catalyze.Tools;
using Silk.NET.Vulkan;

namespace Catalyze;

public readonly unsafe struct DescriptorSetLayout : IDisposable, IConvertibleTo<Silk.NET.Vulkan.DescriptorSetLayout>
{
    private readonly Device _device;
    public readonly Silk.NET.Vulkan.DescriptorSetLayout VkDescriptorSetLayout;
    public ulong Handle => VkDescriptorSetLayout.Handle;
    public DescriptorSetLayout(Device device, Dictionary<uint, DescriptorSetLayoutBinding> bindings)
    {
        _device = device;
        fixed (DescriptorSetLayoutBinding* pBindings = bindings.Values.ToArray())
        {
            var layoutInfo = new DescriptorSetLayoutCreateInfo
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                BindingCount = (uint) bindings.Count,
                PBindings = pBindings
            };
            vk.CreateDescriptorSetLayout(device,  layoutInfo, null, out VkDescriptorSetLayout).Validate();
        }
    }

    public void Dispose() => vk.DestroyDescriptorSetLayout(_device, VkDescriptorSetLayout, null);

    public static implicit operator Silk.NET.Vulkan.DescriptorSetLayout(DescriptorSetLayout dsl) =>dsl.VkDescriptorSetLayout;
    public Silk.NET.Vulkan.DescriptorSetLayout Convert() => VkDescriptorSetLayout;
}

public unsafe class DescriptorSetLayoutBuilder
{
    private readonly Dictionary<uint, DescriptorSetLayoutBinding> _bindings = new();
    private DescriptorSetLayoutBuilder(){}

    public static DescriptorSetLayoutBuilder Start() => new();

    public DescriptorSetLayoutBuilder WithSampler(uint binding, DescriptorType type, ShaderStageFlags stageFlags, Sampler sampler,
                                           uint count = 1)
    {
        AddBinding(binding, new DescriptorSetLayoutBinding
        {   
            Binding = binding,
            DescriptorCount = count,
            DescriptorType = type,
            StageFlags = stageFlags,
            PImmutableSamplers = &sampler
        });
        return this;
    }
    public DescriptorSetLayoutBuilder With(uint binding, DescriptorType type, ShaderStageFlags stageFlags, uint count = 1)
    {
        AddBinding(binding, new DescriptorSetLayoutBinding
        {   
            Binding = binding,
            DescriptorCount = count,
            DescriptorType = type,
            StageFlags = stageFlags
        });
        return this;
    }
    
    private void AddBinding(uint binding, DescriptorSetLayoutBinding layoutBinding)
    {
        if (_bindings.ContainsKey(binding))
        {
            if (!_bindings[binding].StageFlags.HasFlag(layoutBinding.StageFlags))
            {
                _bindings[binding] = _bindings[binding] with
                {
                    StageFlags = _bindings[binding].StageFlags | layoutBinding.StageFlags
                };
            }
            Console.WriteLine($"Binding {binding} already in use, ignoring {layoutBinding}");
            return;
        }
        _bindings.Add(binding, layoutBinding);
    }

    public DescriptorSetLayout CreateOn(Device device) => new(device, _bindings);
}