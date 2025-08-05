using System.Collections.Generic;

namespace GoodWin.Core
{
    public interface IRouletteEngine
    {
        IDebuff Spin();
        void RegisterDebuffs(IEnumerable<IDebuff> debuffs);
    }
}
