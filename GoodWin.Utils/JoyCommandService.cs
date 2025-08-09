using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace GoodWin.Utils
{
    public class JoyCommandService : IDisposable
    {
        public static JoyCommandService Instance { get; } = new JoyCommandService();

        private readonly ViGEmClient? _client;
        private readonly IXbox360Controller? _controller;
        private readonly Dictionary<string, int> _commandToButton = new();
        private readonly Dictionary<int, string> _buttonToCommand = new();
        private int _nextButton = 1;

        public bool IsOperational { get; private set; }

        private JoyCommandService()
        {
            try
            {
                _client = new ViGEmClient();
                _controller = _client.CreateXbox360Controller();
                _controller.Connect();
                IsOperational = true;
            }
            catch (Exception ex)
            {
                // Если драйвер ViGEm не установлен или библиотека недоступна,
                // не прерываем загрузку дебаффов. Сервис работает в "немом" режиме.
                _client = null;
                _controller = null;
                IsOperational = false;
                Log($"[JoyCommandService] init failed: {ex.Message}");
            }
        }

        public int Register(string command)
        {
            if (_commandToButton.TryGetValue(command, out var btn))
                return btn;
            btn = _nextButton++;
            _commandToButton[command] = btn;
            _buttonToCommand[btn] = command;
            return btn;
        }

        public async Task InitializeBindingsAsync(CancellationToken token)
        {
            if (_controller is null)
                return;

            const int ConsoleKey = 0xDC;
            const int EnterKey = 0x0D;
            InputHookHost.Instance.SendKey(ConsoleKey);
            await Task.Delay(100, token);
            foreach (var pair in _commandToButton)
            {
                string joyName = $"joy{pair.Value}";
                InputHookHost.Instance.SendText($"bind \"{joyName}\" \"{pair.Key}\"");
                await Task.Delay(50, token);
                InputHookHost.Instance.SendKey(EnterKey);
                await Task.Delay(50, token);
            }
            InputHookHost.Instance.SendKey(ConsoleKey);
        }

        public void Press(int buttonIndex, int holdMs = 200)
        {
            if (_controller is not null)
            {
                var button = GetButton(buttonIndex);
                _controller.SetButtonState(button, true);
                Thread.Sleep(holdMs);
                _controller.SetButtonState(button, false);
            }
            else if (_buttonToCommand.TryGetValue(buttonIndex, out var cmd))
            {
                InputHookHost.Instance.Cmd(cmd);
            }
        }

        public bool SelfTest()
        {
            if (!IsOperational)
                return false;

            var idx = Register("echo GoodWin");
            try
            {
                Press(idx, 10);
                return true;
            }
            catch (Exception ex)
            {
                Log($"[JoyCommandService] self-test failed: {ex.Message}");
                return false;
            }
        }

        private static void Log(string message)
        {
            try
            {
                var type = Type.GetType("GoodWin.Gui.Services.DebugLogService, GoodWin.Gui");
                var method = type?.GetMethod("Log", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                method?.Invoke(null, new object[] { message });
            }
            catch
            {
                Console.WriteLine(message);
            }
        }

        private static Xbox360Button GetButton(int index) => index switch
        {
            1 => Xbox360Button.A,
            2 => Xbox360Button.B,
            3 => Xbox360Button.X,
            4 => Xbox360Button.Y,
            5 => Xbox360Button.LeftShoulder,
            6 => Xbox360Button.RightShoulder,
            7 => Xbox360Button.Back,
            8 => Xbox360Button.Start,
            9 => Xbox360Button.LeftThumb,
            10 => Xbox360Button.RightThumb,
            11 => Xbox360Button.Up,
            12 => Xbox360Button.Down,
            13 => Xbox360Button.Left,
            14 => Xbox360Button.Right,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

        public void Dispose()
        {
            _controller?.Disconnect();
            _client?.Dispose();
        }
    }
}
