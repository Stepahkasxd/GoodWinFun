using System;

namespace GoodWin.Tracker
{
    /// <summary>
    /// Resolves Dota 2 installation paths and ensures that
    /// a Game State Integration configuration file exists.
    /// </summary>
    public interface IDotaPathResolver
    {
        /// <summary>
        /// Ensures that the GSI configuration file for GoodWin exists.
        /// </summary>
        /// <param name="manualRoot">Optional path to the Dota 2 installation.</param>
        /// <param name="port">Port for the local GSI HTTP listener.</param>
        /// <returns>Full path to the configuration file or null if Dota 2 was not found.</returns>
        string? EnsureConfigCreated(string? manualRoot, int port);

        /// <summary>
        /// Checks whether the supplied root path points to a valid Dota 2 installation.
        /// </summary>
        /// <param name="root">Path to the Dota 2 root directory.</param>
        /// <returns><c>true</c> if the installation appears valid; otherwise, <c>false</c>.</returns>
        bool IsValidRoot(string root);
    }
}
