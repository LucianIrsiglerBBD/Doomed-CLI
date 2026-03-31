using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace DoomedCLI;

class InputManager : IDisposable
{
    private readonly IKeyStateProvider _provider;

    public InputManager()
    {
        _provider = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new WindowsProvider()
            : new TerminalProvider();
    }

    public bool IsKeyDown(ConsoleKey key) => _provider.IsKeyDown(key);
    public void Dispose() => _provider.Dispose();

    // Platform-specific implementations
    private interface IKeyStateProvider : IDisposable
    {
        bool IsKeyDown(ConsoleKey key);
    }

    // Windows impimentation

    private sealed class WindowsProvider : IKeyStateProvider
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private readonly Thread _drain;
        private volatile bool _running = true;

        public WindowsProvider()
        {
            _drain = new Thread(() =>
            {
                while (_running)
                {
                    while (Console.KeyAvailable) Console.ReadKey(intercept: true);
                    Thread.Sleep(5);
                }
            })
            { IsBackground = true, Name = "InputDrain" };
            _drain.Start();
        }

        public bool IsKeyDown(ConsoleKey key)
            => (GetAsyncKeyState(ToVKey(key)) & 0x8000) != 0;

        private static int ToVKey(ConsoleKey key) => key switch
        {
            ConsoleKey.A => 0x41,
            ConsoleKey.B => 0x42,
            ConsoleKey.C => 0x43,
            ConsoleKey.D => 0x44,
            ConsoleKey.E => 0x45,
            ConsoleKey.F => 0x46,
            ConsoleKey.G => 0x47,
            ConsoleKey.H => 0x48,
            ConsoleKey.I => 0x49,
            ConsoleKey.J => 0x4A,
            ConsoleKey.K => 0x4B,
            ConsoleKey.L => 0x4C,
            ConsoleKey.M => 0x4D,
            ConsoleKey.N => 0x4E,
            ConsoleKey.O => 0x4F,
            ConsoleKey.P => 0x50,
            ConsoleKey.Q => 0x51,
            ConsoleKey.R => 0x52,
            ConsoleKey.S => 0x53,
            ConsoleKey.T => 0x54,
            ConsoleKey.U => 0x55,
            ConsoleKey.V => 0x56,
            ConsoleKey.W => 0x57,
            ConsoleKey.X => 0x58,
            ConsoleKey.Y => 0x59,
            ConsoleKey.Z => 0x5A,
            ConsoleKey.Spacebar => 0x20,
            ConsoleKey.Enter => 0x0D,
            ConsoleKey.Escape => 0x1B,
            ConsoleKey.UpArrow => 0x26,
            ConsoleKey.DownArrow => 0x28,
            ConsoleKey.LeftArrow => 0x25,
            ConsoleKey.RightArrow => 0x27,
            _ => (int)key,
        };

        public void Dispose() => _running = false;
    }

    // Linux / macOS: Fallback using key-press timestamp window (less reliable than Windows API).

    private sealed class TerminalProvider : IKeyStateProvider
    {
        private const int HoldWindowMs = 100;

        private readonly ConcurrentDictionary<ConsoleKey, long> _lastSeen = new();
        private readonly Thread _reader;
        private volatile bool _running = true;

        public TerminalProvider()
        {
            _reader = new Thread(() =>
            {
                while (_running)
                {
                    if (Console.KeyAvailable)
                    {
                        long currentTick = Environment.TickCount64;
                        do { _lastSeen[Console.ReadKey(intercept: true).Key] = currentTick; }
                        while (Console.KeyAvailable);
                    }
                    else Thread.Sleep(1);
                }
            })
            { IsBackground = true, Name = "InputRead" };
            _reader.Start();
        }

        public bool IsKeyDown(ConsoleKey key)
            => _lastSeen.TryGetValue(key, out long lastSeenTick)
            && (Environment.TickCount64 - lastSeenTick) < HoldWindowMs;

        public void Dispose() => _running = false;
    }
}
