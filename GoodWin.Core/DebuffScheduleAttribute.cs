using System;

namespace GoodWin.Core
{
    /// <summary>
    /// Фаза применения дебаффов (Easy: 0–10 мин, Medium: 10–20, Hard: >20).
    /// </summary>
    public enum DebuffPhase
    {
        Easy,
        Medium,
        Hard
    }

    /// <summary>
    /// Атрибут для пометки класса IDebuff: в какой фазе и сколько спинов мин/макс, длительность в секундах.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DebuffScheduleAttribute : Attribute
    {
        public DebuffPhase Phase { get; }
        public int MinSpins { get; }
        public int MaxSpins { get; }
        public int DurationSeconds { get; }

        public DebuffScheduleAttribute(DebuffPhase phase, int minSpins, int maxSpins, int durationSeconds)
        {
            Phase = phase;
            MinSpins = minSpins;
            MaxSpins = maxSpins;
            DurationSeconds = durationSeconds;
        }
    }
}