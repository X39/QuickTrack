using System.Collections.Immutable;
using QuickTrack.Win32;
using X39.Util;
using X39.Util.Collections;
using X39.Util.Console;

namespace QuickTrack.Commands;

public class RdpActiveCommand : IConsoleCommand
{
    public string[] Keys { get; } = new[] {"rdp-active", "idle"};

    public string Description => "Starts to randomly issue keyboard commands to simulate presence for when RDP " +
                                 "connections are supposed to stay open for hours in absence.";

    public string Pattern { get; } = "( rdp-active | idle )";

    private Random _random = new();

    public async ValueTask ExecuteAsync(ImmutableArray<string> args, CancellationToken cancellationToken)
    {
        var consoleString = new ConsoleString("Press ANY key to stop rdp-active mode")
        {
            Background = ConsoleColor.Yellow,
            Foreground = ConsoleColor.Black,
        };
        consoleString.Write();
        using var cancellationTokenSource = new CancellationTokenSource();
        var task = Task.Run(
            async () =>
            {
                var validKeys = new[]
                {
                    new[] {EVirtualKeyCode.A, EVirtualKeyCode.Backspace},
                    new[] {EVirtualKeyCode.B, EVirtualKeyCode.Backspace},
                    new[] {EVirtualKeyCode.C, EVirtualKeyCode.Backspace},
                    new[] {EVirtualKeyCode.D, EVirtualKeyCode.Backspace},
                    new[] {EVirtualKeyCode.E, EVirtualKeyCode.Backspace},
                    new[] {EVirtualKeyCode.F, EVirtualKeyCode.Backspace},
                    new[] {EVirtualKeyCode.G, EVirtualKeyCode.Backspace},
                    new[] {EVirtualKeyCode.H, EVirtualKeyCode.Backspace},
                    new[] {EVirtualKeyCode.I, EVirtualKeyCode.Backspace},
                    new[] {EVirtualKeyCode.J, EVirtualKeyCode.Backspace},
                    new[] {EVirtualKeyCode.K, EVirtualKeyCode.Backspace},
                    new[] {EVirtualKeyCode.L, EVirtualKeyCode.Backspace},
                    new[] {EVirtualKeyCode.M, EVirtualKeyCode.Backspace},
                    new[] {EVirtualKeyCode.N, EVirtualKeyCode.Backspace},
                    new[] {EVirtualKeyCode.O, EVirtualKeyCode.Backspace},
                    new[] {EVirtualKeyCode.P, EVirtualKeyCode.Backspace},
                    new[] {EVirtualKeyCode.Q, EVirtualKeyCode.Backspace},
                    new[] {EVirtualKeyCode.R, EVirtualKeyCode.Backspace},
                    new[] {EVirtualKeyCode.S, EVirtualKeyCode.Backspace},
                    new[] {EVirtualKeyCode.T, EVirtualKeyCode.Backspace},
                    new[] {EVirtualKeyCode.U, EVirtualKeyCode.Backspace},
                    new[] {EVirtualKeyCode.V, EVirtualKeyCode.Backspace},
                    new[] {EVirtualKeyCode.W, EVirtualKeyCode.Backspace},
                    new[] {EVirtualKeyCode.X, EVirtualKeyCode.Backspace},
                    new[] {EVirtualKeyCode.Y, EVirtualKeyCode.Backspace},
                    new[] {EVirtualKeyCode.Z, EVirtualKeyCode.Backspace},
                    new[] {EVirtualKeyCode.SpaceBar, EVirtualKeyCode.Backspace},
                };
                // ReSharper disable once AccessToDisposedClosure
                while (!cancellationTokenSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    // ReSharper disable once AccessToDisposedClosure
                    await Task.Delay(TimeSpan.FromSeconds(_random.Next(30, 300)), cancellationTokenSource.Token)
                        .ConfigureAwait(false);
                    var randomIndex = _random.Next(0, validKeys.Length);
                    var keys = validKeys[randomIndex];
                    foreach (var virtualKeyCode in keys)
                    {
                        Interop.SendKeyboardInput.KeyPress(virtualKeyCode);
                        // ReSharper disable once AccessToDisposedClosure
                        await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationTokenSource.Token)
                            .ConfigureAwait(false);
                    }
                }
            });
        Console.ReadKey(true);
        cancellationTokenSource.Cancel();
        await Fault.IgnoreAsync(async () => await task.ConfigureAwait(false)).ConfigureAwait(false);
        consoleString.WriteBackspaces();
    }
}