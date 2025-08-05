using System;
using Dota2GSI;
using Dota2GSI.Nodes;

namespace GoodWin.Tracker
{
    /// <summary>
    /// Слушатель GSI GameState
    /// </summary>
    public class GsiListenerService : IDisposable
    {
        private GameStateListener _listener;

        /// <summary>
        /// Текущий порт, на котором слушается GSI.
        /// </summary>
        public int Port => _listener.Port;

        /// <summary>
        /// Событие нового состояния игры
        /// </summary>
        public event Action<GameState>? OnNewGameState;

        public GsiListenerService(int port = 3000)
        {
            _listener = new GameStateListener(port);
            _listener.NewGameState += HandleNewGameState;
        }

        private void HandleNewGameState(GameState gs)
        {
            // Передаём состояние дальше
            OnNewGameState?.Invoke(gs);
        }

        public bool GenerateConfig(string configName = "GoodWinDebuff") =>
            _listener.GenerateGSIConfigFile(configName);

        /// <summary>
        /// Запускает прослушивание GSI. Если выбранный порт занят,
        /// пытается увеличить его до 5 раз.
        /// </summary>
        public bool Start(int maxAttempts = 5)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                if (_listener.Start())
                    return true;

                var nextPort = _listener.Port + 1;
                _listener.NewGameState -= HandleNewGameState;
                _listener.Dispose();
                _listener = new GameStateListener(nextPort);
                _listener.NewGameState += HandleNewGameState;
            }
            return false;
        }

        public void Stop() => _listener.Stop();

        public void Dispose()
        {
            Stop();
            _listener.NewGameState -= HandleNewGameState;
            _listener.Dispose();
        }
    }
}