using System;

namespace GoodWin.Core
{
    /// <summary>
    /// Provides a global panic mechanism to instantly remove active debuffs.
    /// </summary>
    public static class PanicService
    {
        /// <summary>
        /// Occurs when the panic hotkey is triggered.
        /// </summary>
        public static event EventHandler? Triggered;

        /// <summary>
        /// Triggers the panic event.
        /// </summary>
        public static void Trigger() => Triggered?.Invoke(null, EventArgs.Empty);
    }
}
