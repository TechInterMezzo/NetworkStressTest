using Spectre.Console;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkStressTest;

internal static class Program
{
    private static readonly CancellationTokenSource CancellationTokenSource = new();

    public static async Task Main()
    {
        Console.CancelKeyPress += static (_, e) =>
        {
            e.Cancel = true;
            Cancel();
        };

        IPAddress ip = PromptForIP();
        int port = PromptForPort();
        int mbitPerSecond = PromptForThrottle();
        int maxBytePerSecond = mbitPerSecond / 8 * 1000 * 1000;
        if (AnsiConsole.Confirm($"Do you want to generate traffic for endpoint {ip}:{port}?", false))
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine($"Traffic generation for endpoint {ip}:{port}");

            using var socket = new UdpSocket(ip, port);
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await AnsiConsole.Status()
                    .AutoRefresh(false)
                    .StartAsync("Sending...", ctx => RunAsync(socket, maxBytePerSecond, ctx));
            }
            catch (OperationCanceledException)
            { }

            AnsiConsole.Clear();
            AnsiConsole.MarkupLine($"Total time spent: {stopwatch.Elapsed}");
            AnsiConsole.MarkupLine($"Total packets sent: {socket.TotalSentPackets:N0}");
            AnsiConsole.MarkupLine($"Total MBytes sent: {socket.TotalSentBytes / (1000 * 1000):N0}");
            AnsiConsole.Console.Input.ReadKey(true);
        }
    }

    private static async Task RunAsync(UdpSocket socket, int maxBytePerSecond, StatusContext context)
    {
        context.Refresh();
        using var packet = PacketGenerator.GenerateRandom(65507);
        int packetPerSecond = maxBytePerSecond / packet.Memory.Length;
        var packetInterval = new Interval(TimeSpan.FromSeconds((packetPerSecond > 0) ? 1.0 / packetPerSecond : 0.0));
        var outputInterval = new Interval(TimeSpan.FromSeconds(0.25));
        while (!CancellationTokenSource.IsCancellationRequested)
        {
            packetInterval.WaitToElapse();
            await socket.SendAsync(packet.Memory, CancellationTokenSource.Token);
            if (outputInterval.IsElapsed())
            {
                if (CheckQuitKey())
                {
                    Cancel();
                    break;
                }
                context.Status($"Sending [green]{socket.SentMbitPerSecond:N0} Mbit/s[/] ({socket.SentPacketPerSecond:N0} p/s)");
                context.Refresh();
            }
        }
    }

    private static void Cancel()
    {
        CancellationTokenSource.Cancel();
    }

    private static IPAddress PromptForIP()
    {
        return IPAddress.Parse(AnsiConsole.Prompt(new TextPrompt<string>("IP")
            .DefaultValue("127.0.0.1")
            .ValidationErrorMessage("Invalid IP address")
            .Validate(x => IPAddress.TryParse(x, out _) ? ValidationResult.Success() : ValidationResult.Error())));
    }

    private static int PromptForPort()
    {
        return AnsiConsole.Prompt(new TextPrompt<int>("Port")
            .DefaultValue(65535)
            .ValidationErrorMessage("Invalid port")
            .Validate(x => x > 0 ? ValidationResult.Success() : ValidationResult.Error()));
    }

    private static int PromptForThrottle()
    {
        return AnsiConsole.Prompt(new TextPrompt<int>("Throttle [[Mbit/s]]")
            .DefaultValue(0)
            .ValidationErrorMessage("Invalid data rate")
            .Validate(x => x >= 0 ? ValidationResult.Success() : ValidationResult.Error()));
    }

    private static bool CheckQuitKey()
    {
        return AnsiConsole.Console.Input.IsKeyAvailable() && AnsiConsole.Console.Input.ReadKey(true)?.KeyChar == 'q';
    }
}
