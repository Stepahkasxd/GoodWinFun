using System;

namespace GoodWin.Core
{
    public class Event
    {
        public string Name { get; set; } = string.Empty;
        public Action? Logic { get; set; }

        public void Invoke()
        {
            Logic?.Invoke();
        }
    }
}
