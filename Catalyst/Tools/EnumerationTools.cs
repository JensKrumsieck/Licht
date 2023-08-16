using Silk.NET.Vulkan;

namespace Catalyst.Tools;

public static unsafe class EnumerationTools
{
    public delegate void EnumerationFunc<TProp, in TOwner>(TOwner owner, uint* count, TProp* prop) where TProp : unmanaged;
    public static TProp[] Enumerate<TProp, TOwner>(TOwner owner, EnumerationFunc<TProp, TOwner> func) where TProp : unmanaged
    {
        var count = 0u;
        func(owner, &count, null);
        var array = new TProp[count];
        fixed (TProp* pArray = array)
            func(owner, &count, pArray);
        return array;
    }

    public static TProp[] Enumerate<TProp>(this PhysicalDevice physicalDevice,
                                           EnumerationFunc<TProp, PhysicalDevice> func) where TProp : unmanaged =>
        Enumerate<TProp, PhysicalDevice>(physicalDevice, func);
}