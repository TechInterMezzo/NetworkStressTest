using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkStressTest;

internal sealed class UdpSocket : IDisposable
{
    private readonly Socket _socket;
    private readonly EndPoint _endPoint;
    private long _lastTimestamp;
    private int _sentPackets;
    private int _sentPacketPerSecond;
    private int _sentBytes;
    private int _sentBytePerSecond;
    private long _totalSentPackets;
    private long _totalSentBytes;

    public UdpSocket(IPAddress ip, int port)
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _endPoint = new CachedIPEndPoint(ip, port);
        _lastTimestamp = Stopwatch.GetTimestamp();
    }

    public int SentPacketPerSecond => Interlocked.CompareExchange(ref _sentPacketPerSecond, 0, 0);
    public int SentBytePerSecond => Interlocked.CompareExchange(ref _sentBytePerSecond, 0, 0);
    public int SentMbitPerSecond => SentBytePerSecond / (1000 * 1000) * 8;
    public long TotalSentPackets => Interlocked.CompareExchange(ref _totalSentPackets, 0, 0);
    public long TotalSentBytes => Interlocked.CompareExchange(ref _totalSentBytes, 0, 0);

    public void Dispose()
    {
        _socket.Dispose();
    }

    public async ValueTask<int> SendAsync(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
    {
        int result = await _socket.SendToAsync(bytes, _endPoint, cancellationToken).ConfigureAwait(false);
        int sentPackets = Interlocked.Increment(ref _sentPackets);
        int sentBytes = Interlocked.Add(ref _sentBytes, result);
        Interlocked.Increment(ref _totalSentPackets);
        Interlocked.Add(ref _totalSentBytes, result);
        long lastTimestamp = Interlocked.CompareExchange(ref _lastTimestamp, 0, 0);
        if (Stopwatch.GetElapsedTime(lastTimestamp).TotalSeconds >= 0.25)
        {
            Interlocked.Exchange(ref _lastTimestamp, Stopwatch.GetTimestamp());
            Interlocked.Exchange(ref _sentPackets, 0);
            Interlocked.Exchange(ref _sentPacketPerSecond, sentPackets * 4);
            Interlocked.Exchange(ref _sentBytes, 0);
            Interlocked.Exchange(ref _sentBytePerSecond, sentBytes * 4);
        }
        return result;
    }
}
