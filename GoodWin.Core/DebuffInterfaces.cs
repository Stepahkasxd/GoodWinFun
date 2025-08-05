using System;

namespace GoodWin.Core
{
    /// <summary>
    /// Дебаффы, эмулирующие ввод
    /// </summary>
    public interface IInputDebuff : IDebuff { }

    /// <summary>
    /// Дебаффы, рисующие оверлей поверх игры
    /// </summary>
    public interface IOverlayDebuff : IDebuff { }

    /// <summary>
    /// Дебаффы, изменяющие параметры трекера или GSI
    /// </summary>
    public interface ITrackerDebuff : IDebuff { }

    /// <summary>
    /// Дебаффы, воспроизводящие звук
    /// </summary>
    public interface IAudioDebuff : IDebuff { }
}
