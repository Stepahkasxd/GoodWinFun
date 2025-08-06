using GoodWin.Core;
using GoodWin.Utils;

namespace GoodWin.Debuffs.Easy
{
    [DebuffSchedule(DebuffPhase.Easy, 4, 10, 60)]
    public class MirrorMapDebuff : DebuffBase
    {
        public override string Name => "Отзеркалить карту";

        public override void Apply()
        {
            CommandExecutor.ExecuteCommand("dota_minimap_position_option 1");
        }

        public override void Remove()
        {
            CommandExecutor.ExecuteCommand("dota_minimap_position_option 0");
        }
    }
}
