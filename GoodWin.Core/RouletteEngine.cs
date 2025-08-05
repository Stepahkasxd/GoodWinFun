using System;
using System.Collections.Generic;

namespace GoodWin.Core
{
    public class RouletteEngine : IRouletteEngine
    {
        private List<IDebuff> _debuffs = new List<IDebuff>();
        private readonly Random _random = new Random();

        public void RegisterDebuffs(IEnumerable<IDebuff> debuffs)
        {
            _debuffs = new List<IDebuff>(debuffs);
        }

        public IDebuff Spin()
        {
            if (_debuffs.Count == 0)
                throw new InvalidOperationException("No debuffs registered.");

            int idx = _random.Next(_debuffs.Count);
            return _debuffs[idx];
        }
    }
}