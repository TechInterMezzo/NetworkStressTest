using System;
using System.Buffers;

namespace NetworkStressTest;

internal static class PacketGenerator
{
    public static IMemoryOwner<byte> GenerateRandom(int size)
    {
        IMemoryOwner<byte> buffer = new NativeBuffer(size);
        Random.Shared.NextBytes(buffer.Memory.Span);
        return buffer;
    }
}
