using System.Collections.Generic;

namespace GoodWin.Tracker;

public enum MatchTeam
{
    Radiant,
    Dire,
    Unknown
}

public sealed class MatchPlayer
{
    public string Name { get; set; } = string.Empty;
    public string? HeroName { get; set; }
    public MatchTeam Team { get; set; }
}

public sealed class MatchState
{
    public IReadOnlyList<MatchPlayer> Players { get; init; } = new List<MatchPlayer>();
    public double Time { get; init; }
    public MatchTeam LocalTeam { get; init; } = MatchTeam.Unknown;
}
