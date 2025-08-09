using System;
using System.Collections.Generic;
using Dota2GSI;
using Dota2GSI.Nodes;
using Dota2GSI.Nodes.Helpers;

namespace GoodWin.Tracker
{
    /// <summary>
    /// Слушатель GSI GameState
    /// </summary>
    public class GsiListenerService : IDisposable
    {
        private readonly IDotaPathResolver _pathResolver;
        private GameStateListener _listener;

        /// <summary>
        /// Текущий порт, на котором слушается GSI.
        /// </summary>
        public int Port => _listener.Port;

        /// <summary>
        /// Событие нового состояния матча
        /// </summary>
        public event Action<MatchState>? OnNewMatchState;

        public GsiListenerService(IDotaPathResolver pathResolver, int port = 3000)
        {
            _pathResolver = pathResolver;
            _listener = new GameStateListener(port);
            _listener.NewGameState += HandleNewGameState;
        }

        private void HandleNewGameState(GameState gs)
        {
            var state = ToMatchState(gs);
            OnNewMatchState?.Invoke(state);
        }

        private static MatchState ToMatchState(GameState gs)
        {
            var players = new List<MatchPlayer>();

            void AddPlayers(FullTeamDetails? team)
            {
                if (team?.Players == null) return;
                foreach (var p in team.Players.Values)
                {
                    players.Add(new MatchPlayer
                    {
                        Name = p.Details?.Name ?? string.Empty,
                        HeroName = p.Hero?.Name,
                        Team = MapTeam(p.Details?.Team)
                    });
                }
            }

            AddPlayers(gs.RadiantTeamDetails);
            AddPlayers(gs.DireTeamDetails);

            var localTeam = MapTeam(gs.Player?.LocalPlayer.Team);
            var time = gs.Map?.ClockTime ?? gs.Map?.GameTime ?? 0;

            return new MatchState
            {
                Players = players,
                Time = time,
                LocalTeam = localTeam
            };
        }

        private static MatchTeam MapTeam(PlayerTeam? team) => team switch
        {
            PlayerTeam.Radiant => MatchTeam.Radiant,
            PlayerTeam.Dire => MatchTeam.Dire,
            _ => MatchTeam.Unknown
        };

        /// <summary>
        /// Запускает прослушивание GSI. Если выбранный порт занят,
        /// пытается увеличить его до 5 раз.
        /// </summary>
        public bool Start(int maxAttempts = 5)
        {
            _pathResolver.EnsureConfigCreated();
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
