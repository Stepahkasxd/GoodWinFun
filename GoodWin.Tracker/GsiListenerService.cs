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
        private readonly GameStateListener _listener;

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

        public bool Start() => _listener.Start();
        public void Stop() => _listener.Stop();

        public void Dispose()
        {
            Stop();
            _listener.NewGameState -= HandleNewGameState;
            _listener.Dispose();
        }
    }
}