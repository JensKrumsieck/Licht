using System.Runtime.InteropServices;

namespace Licht.GraphicsCore;

public readonly unsafe struct ByteString : IDisposable
{
    public nint Ptr { get; }
    public ByteString(string s) => Ptr = Marshal.StringToHGlobalAnsi(s);
    public ByteString(byte* ptr) => Ptr = (nint) ptr;
    public static implicit operator byte*(ByteString bs) => (byte*)bs.Ptr;
    public static implicit operator string(ByteString bs) => bs.ToString();
    public override string ToString() => Marshal.PtrToStringAnsi(Ptr)!;
    public void Dispose() => Marshal.FreeHGlobal(Ptr);
}
