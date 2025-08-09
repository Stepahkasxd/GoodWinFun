using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Timer = System.Threading.Timer;
using GoodWin.Core;

namespace GoodWin.Utils
{
    public class InputHookHost : IDisposable
    {
        public static InputHookHost Instance { get; } = new InputHookHost();

        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;
        private IntPtr _keyboardHook = IntPtr.Zero;
        private IntPtr _mouseHook = IntPtr.Zero;
        private Thread? _thread;

        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
        private readonly HookProc _kbProc;
        private readonly HookProc _msProc;

        private readonly HashSet<byte> _blockedKeys = new();
        private readonly object _blockedKeysLock = new();
        private int _blockAllKeys;
        private bool _ctrlDown;
        private bool _altDown;
        private int _invertY;
        private int _mouseLag;
        private int _inputLag;
        private int _blockWheel;
        private POINT _lastMousePt;
        private bool _hasLastPt;

        private struct KeyLagEvent
        {
            public int Vk;
            public bool Up;
            public long Time;
        }

        private struct MouseLagSample
        {
            public int X;
            public int Y;
            public long Time;
        }

        private readonly ConcurrentQueue<KeyLagEvent> _keyLagQueue = new();
        private readonly ConcurrentQueue<MouseLagSample> _mouseLagQueue = new();
        private readonly Stopwatch _lagStopwatch = Stopwatch.StartNew();
        private MouseLagSample _lastLagSample;
        private bool _hasLastLagSample;
        private Timer? _lagTimer;

        private const double LagDelayMs = 330;
        private const double MaxBufferMs = 600;

        private InputHookHost()
        {
            _kbProc = LowLevelKeyboardProc;
            _msProc = LowLevelMouseProc;
            StartHooks();
        }

        private void StartHooks()
        {
            if (_thread != null) return;
            _thread = new Thread(() =>
            {
                Application.Idle += (_, __) =>
                {
                    var modName = Process.GetCurrentProcess().MainModule?.ModuleName;
                    if (modName != null)
                    {
                        if (_keyboardHook == IntPtr.Zero)
                            _keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, _kbProc, GetModuleHandle(modName), 0);
                        if (_mouseHook == IntPtr.Zero)
                            _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _msProc, GetModuleHandle(modName), 0);
                    }
                };
                Application.Run();
            })
            {
                IsBackground = true,
                Name = "InputHookHostThread"
            };
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();
        }

        private void EnsureLagTimer()
        {
            if (_lagTimer == null)
                _lagTimer = new Timer(LagTick, null, 0, 8);
        }

        private void StopLagTimer()
        {
            _lagTimer?.Dispose();
            _lagTimer = null;
            _hasLastLagSample = false;
            while (_keyLagQueue.TryDequeue(out _)) { }
            while (_mouseLagQueue.TryDequeue(out _)) { }
        }

        private void PruneKeyQueue(long threshold)
        {
            while (_keyLagQueue.TryPeek(out var e) && e.Time < threshold)
                _keyLagQueue.TryDequeue(out _);
        }

        private void PruneMouseQueue(long threshold)
        {
            while (_mouseLagQueue.TryPeek(out var e) && e.Time < threshold)
                _mouseLagQueue.TryDequeue(out _);
        }

        private void LagTick(object? state)
        {
            long target = _lagStopwatch.ElapsedMilliseconds - (long)LagDelayMs;

            while (_keyLagQueue.TryPeek(out var ke) && ke.Time <= target)
            {
                _keyLagQueue.TryDequeue(out ke);
                var input = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = (ushort)ke.Vk,
                            dwFlags = ke.Up ? KEYEVENTF_KEYUP : 0,
                        }
                    }
                };
                SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
            }

            while (_mouseLagQueue.TryPeek(out var me) && me.Time <= target)
            {
                _mouseLagQueue.TryDequeue(out me);
                _lastLagSample = me;
                _hasLastLagSample = true;
            }

            if (_hasLastLagSample)
            {
                int x, y;
                if (_mouseLagQueue.TryPeek(out var next))
                {
                    double t0 = _lastLagSample.Time;
                    double t1 = next.Time;
                    double f = t1 == t0 ? 1 : (target - t0) / (t1 - t0);
                    if (f < 0) f = 0; if (f > 1) f = 1;
                    x = (int)Math.Round(_lastLagSample.X + (next.X - _lastLagSample.X) * f);
                    y = (int)Math.Round(_lastLagSample.Y + (next.Y - _lastLagSample.Y) * f);
                }
                else
                {
                    x = _lastLagSample.X;
                    y = _lastLagSample.Y;
                }
                Cursor.Position = new Point(x, y);
            }

            long cutoff = _lagStopwatch.ElapsedMilliseconds - (long)MaxBufferMs;
            PruneKeyQueue(cutoff);
            PruneMouseQueue(cutoff);
            if (_hasLastLagSample && _lastLagSample.Time < cutoff)
                _hasLastLagSample = false;
        }

        private IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                bool injected = (data.flags & (LLKHF_INJECTED | LLKHF_LOWER_IL_INJECTED)) != 0;
                if (!injected)
                {
                    int vk = (int)data.vkCode & 0xFF;
                    bool isDown = wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN;
                    bool isUp = wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP;

                    if (isDown)
                    {
                        if (vk == (int)Keys.LControlKey || vk == (int)Keys.RControlKey) _ctrlDown = true;
                        if (vk == (int)Keys.LMenu || vk == (int)Keys.RMenu) _altDown = true;

                        if (vk == (int)Keys.P && _ctrlDown && _altDown)
                        {
                            PanicService.Trigger();
                            return new IntPtr(1);
                        }
                    }
                    else if (isUp)
                    {
                        if (vk == (int)Keys.LControlKey || vk == (int)Keys.RControlKey) _ctrlDown = false;
                        if (vk == (int)Keys.LMenu || vk == (int)Keys.RMenu) _altDown = false;
                    }

                    bool blocked;
                    lock (_blockedKeysLock)
                    {
                        blocked = _blockedKeys.Contains((byte)vk);
                    }
                    if (_blockAllKeys > 0 || blocked)
                        return new IntPtr(1);
                    if (_inputLag > 0)
                    {
                        long now = _lagStopwatch.ElapsedMilliseconds;
                        _keyLagQueue.Enqueue(new KeyLagEvent { Vk = vk, Up = isUp, Time = now });
                        PruneKeyQueue(now - (long)MaxBufferMs);
                        return new IntPtr(1);
                    }
                }
            }
            return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
        }

        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;
        private const uint LLMHF_INJECTED = 0x00000001;
        private const uint LLMHF_LOWER_IL_INJECTED = 0x00000002;
        private const uint LLKHF_INJECTED = 0x00000010;
        private const uint LLKHF_LOWER_IL_INJECTED = 0x00000002;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x, y; }
        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        private IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if (_blockWheel > 0 && wParam == (IntPtr)WM_MOUSEWHEEL)
                    return new IntPtr(1);

                if (wParam == (IntPtr)WM_MOUSEMOVE)
                {
                    var data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    bool injected = (data.flags & (LLMHF_INJECTED | LLMHF_LOWER_IL_INJECTED)) != 0;

                    if (!injected)
                    {
                        if (_mouseLag > 0)
                        {
                            long now = _lagStopwatch.ElapsedMilliseconds;
                            _mouseLagQueue.Enqueue(new MouseLagSample { X = data.pt.x, Y = data.pt.y, Time = now });
                            PruneMouseQueue(now - (long)MaxBufferMs);
                            return new IntPtr(1);
                        }

                        if (!_hasLastPt)
                        {
                            _lastMousePt = data.pt;
                            _hasLastPt = true;
                        }
                        int screenHeight = Screen.PrimaryScreen.Bounds.Height;
                        if (data.pt.y <= 0 || data.pt.y >= screenHeight - 1)
                        {
                            _lastMousePt = data.pt;
                            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
                        }
                        if (_invertY > 0)
                        {
                            int dx = data.pt.x - _lastMousePt.x;
                            int dy = data.pt.y - _lastMousePt.y;
                            mouse_event(MOUSEEVENTF_MOVE, dx, -dy, 0, UIntPtr.Zero);
                            GetCursorPos(out _lastMousePt);
                            return new IntPtr(1);
                        }
                        _lastMousePt = data.pt;
                    }
                }
            }
            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        private const uint INPUT_MOUSE = 0;
        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_UNICODE = 0x0004;

        public void SendKey(int vk)
        {
            var inputs = new[]
            {
                new INPUT
                {
                    type = INPUT_KEYBOARD,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT { wVk = (ushort)vk }
                    }
                },
                new INPUT
                {
                    type = INPUT_KEYBOARD,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT { wVk = (ushort)vk, dwFlags = KEYEVENTF_KEYUP }
                    }
                }
            };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        }

        public void SendWheel(int delta)
        {
            var input = new INPUT
            {
                type = INPUT_MOUSE,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT { mouseData = (uint)delta, dwFlags = MOUSEEVENTF_WHEEL }
                }
            };
            SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
        }

        // Unicode-ввод символов без учёта раскладки
        public void SendText(string text)
        {
            foreach (char c in text)
            {
                var inputs = new[]
                {
                    new INPUT
                    {
                        type = INPUT_KEYBOARD,
                        U = new InputUnion
                        {
                            ki = new KEYBDINPUT { wScan = c, dwFlags = KEYEVENTF_UNICODE }
                        }
                    },
                    new INPUT
                    {
                        type = INPUT_KEYBOARD,
                        U = new InputUnion
                        {
                            ki = new KEYBDINPUT { wScan = c, dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP }
                        }
                    }
                };
                SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
                Thread.Sleep(1);
            }
        }

        // Ввод команды в консоль Dota2: '\' + команда + Enter + '\'
        private void SendConsoleCommand(string cmd)
        {
            const byte CK = 0xDC;
            const int Delay = 1;
            SendKey(CK);
            Thread.Sleep(Delay);
            SendText(cmd);
            Thread.Sleep(Delay);
            SendKey((int)Keys.Enter);
            Thread.Sleep(Delay);
            SendKey(CK);
        }

        public void Cmd(string cmd) => SendConsoleCommand(cmd);

        public void BlockKey(int vk)
        {
            if (vk == (int)Keys.LControlKey || vk == (int)Keys.RControlKey ||
                vk == (int)Keys.LMenu || vk == (int)Keys.RMenu ||
                vk == (int)Keys.P)
                return;
            lock (_blockedKeysLock)
                _blockedKeys.Add((byte)vk);
        }
        public void UnblockKey(int vk)
        {
            lock (_blockedKeysLock)
                _blockedKeys.Remove((byte)vk);
        }

        public void BlockAllKeys()
        {
            if (Interlocked.Increment(ref _blockAllKeys) == 1)
                BlockInput(true);
        }

        public void UnblockAllKeys()
        {
            if (Interlocked.Decrement(ref _blockAllKeys) <= 0)
            {
                _blockAllKeys = 0;
                BlockInput(false);
            }
        }

        public void SetInvertY(bool on)
        {
            if (on)
            {
                if (Interlocked.Increment(ref _invertY) == 1)
                {
                    GetCursorPos(out _lastMousePt);
                    _hasLastPt = true;
                }
            }
            else if (Interlocked.Decrement(ref _invertY) <= 0)
            {
                _invertY = 0;
                _hasLastPt = false;
            }
        }

        public void SetMouseLag(bool on)
        {
            if (on)
            {
                if (Interlocked.Increment(ref _mouseLag) == 1)
                    EnsureLagTimer();
            }
            else if (Interlocked.Decrement(ref _mouseLag) <= 0)
            {
                _mouseLag = 0;
                if (_inputLag <= 0)
                    StopLagTimer();
            }
        }

        public void SetInputLag(bool on)
        {
            if (on)
            {
                if (Interlocked.Increment(ref _inputLag) == 1)
                    EnsureLagTimer();
            }
            else if (Interlocked.Decrement(ref _inputLag) <= 0)
            {
                _inputLag = 0;
                if (_mouseLag <= 0)
                    StopLagTimer();
            }
        }

        public void SetCameraWheelBlocked(bool on)
        {
            if (on)
                Interlocked.Increment(ref _blockWheel);
            else if (Interlocked.Decrement(ref _blockWheel) < 0)
                _blockWheel = 0;
        }

        public void Dispose()
        {
            if (_keyboardHook != IntPtr.Zero) UnhookWindowsHookEx(_keyboardHook);
            if (_mouseHook != IntPtr.Zero) UnhookWindowsHookEx(_mouseHook);
            StopLagTimer();
            Application.ExitThread();
            _thread = null;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)] private static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("user32.dll")] private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);
        [DllImport("user32.dll")] private static extern bool BlockInput(bool fBlockIt);
        [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT lpPoint);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
    }
}
