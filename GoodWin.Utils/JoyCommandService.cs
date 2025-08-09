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

        private readonly ViGEmClient _client;
        private readonly Xbox360Controller _controller;
        private readonly Dictionary<string, int> _commandToButton = new();
        private int _nextButton = 1;

        private JoyCommandService()
        {
            _client = new ViGEmClient();
            _controller = new Xbox360Controller(_client);
            _controller.Connect();
        }

        public int Register(string command)
        {
            if (_commandToButton.TryGetValue(command, out var btn))
                return btn;
            btn = _nextButton++;
            _commandToButton[command] = btn;
            return btn;
        }

        public async Task InitializeBindingsAsync(CancellationToken token)
        {
            const int ConsoleKey = 0xDC;
            const int EnterKey = 0x0D;
            InputHookHost.Instance.SendKey(ConsoleKey);
            await Task.Delay(100, token);
            foreach (var pair in _commandToButton)
            {
                string joyName = $"joy{pair.Value}";
                InputHookHost.Instance.SendText($"bind \\\"{joyName}\\\" \\\"{pair.Key}\\\"");
                await Task.Delay(50, token);
                InputHookHost.Instance.SendKey(EnterKey);
                await Task.Delay(50, token);
            }
            InputHookHost.Instance.SendKey(ConsoleKey);
        }

        public void Press(int buttonIndex, int holdMs = 200)
        {
            var button = GetButton(buttonIndex);
            var report = new Xbox360Report();
            report.SetButtons(button);
            _controller.SendReport(report);
            Thread.Sleep(holdMs);
            _controller.SendReport(new Xbox360Report());
        }

        private static Xbox360Buttons GetButton(int index) => index switch
        {
            1 => Xbox360Buttons.A,
            2 => Xbox360Buttons.B,
            3 => Xbox360Buttons.X,
            4 => Xbox360Buttons.Y,
            5 => Xbox360Buttons.LeftShoulder,
            6 => Xbox360Buttons.RightShoulder,
            7 => Xbox360Buttons.Back,
            8 => Xbox360Buttons.Start,
            9 => Xbox360Buttons.LeftThumb,
            10 => Xbox360Buttons.RightThumb,
            11 => Xbox360Buttons.Up,
            12 => Xbox360Buttons.Down,
            13 => Xbox360Buttons.Left,
            14 => Xbox360Buttons.Right,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

        public void Dispose()
        {
            _controller.Disconnect();
            _controller.Dispose();
            _client.Dispose();
        }
    }
}
