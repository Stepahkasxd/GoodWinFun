using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GoodWin.Utils
{
    public static class CommandExecutor
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern short VkKeyScan(char ch);

        const uint INPUT_KEYBOARD = 1;
        const uint KEYEVENTF_KEYUP = 0x0002;
        const ushort VK_OEM_3 = 0xC0; // '~'
        const ushort VK_RETURN = 0x0D;
        const ushort VK_SHIFT = 0x10;

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct InputUnion
        {
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        public static void ExecuteCommand(string command)
        {
            var prevWnd = GetForegroundWindow();
            var dotaWnd = FindWindow("SDL_app", null);
            if (dotaWnd == IntPtr.Zero) return;
            SetForegroundWindow(dotaWnd);

            List<INPUT> inputs = new List<INPUT>();
            void Key(ushort vk, bool up = false)
            {
                inputs.Add(new INPUT
                {
                    type = INPUT_KEYBOARD,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = vk,
                            wScan = 0,
                            dwFlags = up ? KEYEVENTF_KEYUP : 0,
                            time = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                });
            }

            // open console '~'
            Key(VK_OEM_3); Key(VK_OEM_3, true);
            // type command
            foreach (char c in command)
            {
                short vk = VkKeyScan(c);
                if (vk == -1) continue;
                ushort vkCode = (ushort)(vk & 0xFF);
                bool shift = (vk & 0x0100) != 0;
                if (shift) Key(VK_SHIFT);
                Key(vkCode); Key(vkCode, true);
                if (shift) Key(VK_SHIFT, true);
            }
            // enter
            Key(VK_RETURN); Key(VK_RETURN, true);
            // close console
            Key(VK_OEM_3); Key(VK_OEM_3, true);

            SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf(typeof(INPUT)));
            SetForegroundWindow(prevWnd);
        }
    }
}
