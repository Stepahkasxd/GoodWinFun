using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GoodWin.Core
{
    public class DebuffsRegistry
    {
        private readonly List<ScheduledDebuffEntry> _entries = new();

        /// <summary>
        /// Регистрирует дебафф, читая его атрибут [DebuffSchedule].
        /// </summary>
        public void Register(IDebuff debuff)
        {
            var attr = debuff.GetType()
                            .GetCustomAttribute<DebuffScheduleAttribute>();
            if (attr == null)
                throw new InvalidOperationException(
                    $"Класс {debuff.GetType().Name} не помечен [DebuffSchedule].");

            _entries.Add(new ScheduledDebuffEntry(debuff, attr));
        }

        /// <summary>
        /// Все зарегистрированные дебаффы с их атрибутами.
        /// </summary>
        public IReadOnlyList<ScheduledDebuffEntry> GetAllEntries()
            => _entries.ToList();  // или AsReadOnly()
    }
}
