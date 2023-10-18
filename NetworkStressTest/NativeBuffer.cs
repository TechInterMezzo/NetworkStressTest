using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace NetworkStressTest;

internal unsafe sealed class NativeBuffer : MemoryManager<byte>
{
    private readonly int _size;
    private byte* _pointer;

    public NativeBuffer(int size)
    {
        _size = size;
        _pointer = (byte*)NativeMemory.Alloc((nuint)size);
    }

    protected override void Dispose(bool disposing)
    {
        if (_pointer is not null)
        {
            NativeMemory.Free(_pointer);
            _pointer = null;
        }
    }

    public override Span<byte> GetSpan()
    {
        return (_pointer is not null) ? new Span<byte>(_pointer, _size) : Span<byte>.Empty;
    }

    public override MemoryHandle Pin(int elementIndex = 0)
    {
        if (_pointer is null)
        {
            throw new ObjectDisposedException(nameof(NativeBuffer));
        }
        return new MemoryHandle(_pointer + elementIndex);
    }

    public override void Unpin()
    { }
}
