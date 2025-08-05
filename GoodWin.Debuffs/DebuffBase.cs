using GoodWin.Core;

namespace GoodWin.Debuffs
{
    public abstract class DebuffBase : IDebuff
    {
        public abstract string Name { get; }
        public abstract void Apply();
        public abstract void Remove();
    }
}
