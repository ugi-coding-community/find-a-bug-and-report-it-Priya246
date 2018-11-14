﻿using System;
using System.Collections.Generic;

namespace TelimenaClient
{
    /// <summary>
    ///     A data about a program
    /// </summary>
    public class ProgramInfo
    {
        /// <summary>
        /// The unique key for this program's telemetry monitoring
        /// </summary>
        public Guid TelemetryKey { get; set; }

        /// <summary>
        ///     A typical program has a primary assembly or an 'entry point'.
        ///     This is where it's info should be defined
        /// </summary>
        public AssemblyInfo PrimaryAssembly { get; set; }

        /// <summary>
        ///     The name of the application.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     An optional collection of helper assemblies data
        /// </summary>
        public List<AssemblyInfo> HelperAssemblies { get; set; }

        /// <summary>
        /// Gets the primary assembly path.
        /// </summary>
        /// <value>The primary assembly path.</value>
        public string PrimaryAssemblyPath => this.PrimaryAssembly?.Location;

    }
}