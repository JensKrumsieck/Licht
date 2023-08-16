using System.Runtime.InteropServices;
using Silk.NET.Core.Native;

namespace Catalyst.Tools;

public sealed unsafe class ByteStringList : IDisposable
{
    private readonly List<ByteString> _items;
    private byte** _ptr;
    public ByteStringList(IReadOnlyList<string> items)
    {
        _items = items.Select(s => new ByteString(s)).ToList();
        _ptr = (byte**) SilkMarshal.StringArrayToPtr(items);
    }
    public ByteStringList(byte** pointer, uint count)
    {
        _ptr = pointer;
        _items = SilkMarshal.PtrToStringArray((nint)pointer, (int)count).Select(s => new ByteString(s)).ToList();
    }

    public void Add(string item)
    {
        _items.Add(new ByteString(item));
        _ptr = (byte**) SilkMarshal.StringArrayToPtr(ToStringList());
    }
    public uint Count => (uint) _items.Count;
    public static implicit operator byte**(ByteStringList bsl) => bsl._ptr;
    public IReadOnlyList<string> ToStringList() => _items.Select(s => s.ToString()).ToList();
    public string[] ToStringArray() => _items.Select(s => s.ToString()).ToArray();
    public void Dispose() => Marshal.FreeHGlobal((nint) _ptr);
}