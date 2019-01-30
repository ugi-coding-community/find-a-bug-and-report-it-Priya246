﻿using TelimenaClient.Model;
using TelimenaClient.Model.Internal;

namespace TelimenaClient
{
    /// <summary>
    /// Synchronous app update methods
    /// </summary>
    public interface IBlockingUpdatesHandler : IFluentInterface
    {
        /// <summary>
        ///     Performs an update check and returns the result which allows custom handling of the update process.
        ///     It will return info about beta versions as well.
        ///     <para>
        ///         This method is a synchronous wrapper over its async counterpart. It will block the thread. It is recommended
        ///         to use async method and handle awaiting properly
        ///     </para>
        /// </summary>
        /// <returns></returns>
        UpdateCheckResult CheckForUpdates();

        /// <summary>
        ///     Handles the updating process from start to end
        ///     <para>
        ///         This method is a synchronous wrapper over its async counterpart. It will block the thread. It is recommended
        ///         to use async method and handle awaiting properly
        ///     </para>
        /// </summary>
        /// <param name="acceptBeta">Determines whether update packages marked as 'beta' version should be used</param>
        /// <returns></returns>
        UpdateCheckResult HandleUpdates(bool acceptBeta);
    }
}