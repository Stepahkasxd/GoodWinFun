namespace GoodWin.Core
{
    public interface IDebuff
    {
        string Name { get; }
        void Apply();
        void Remove();
    }
}