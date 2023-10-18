using System.Net;
using System.Threading;

namespace NetworkStressTest;

internal sealed class CachedIPEndPoint : IPEndPoint
{
    private SocketAddress? _cachedSocketAddress;

    public CachedIPEndPoint(IPAddress address, int port)
        : base(address, port)
    { }

    public override SocketAddress Serialize()
    {
        SocketAddress? socketAddress = Interlocked.CompareExchange(ref _cachedSocketAddress, null, null);
        if (socketAddress == null)
        {
            socketAddress = base.Serialize();
            Interlocked.CompareExchange(ref _cachedSocketAddress, socketAddress, null);
        }
        return socketAddress;
    }
}
