using System;

namespace GoodWin.Core
{
    /// <summary>
    /// Запись реестра: дебафф и его атрибут расписания.
    /// </summary>
    public class ScheduledDebuffEntry
    {
        public IDebuff Debuff { get; }
        public DebuffScheduleAttribute Schedule { get; }

        public ScheduledDebuffEntry(IDebuff debuff, DebuffScheduleAttribute schedule)
        {
            Debuff = debuff ?? throw new ArgumentNullException(nameof(debuff));
            Schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
        }
    }
}
