// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.Localization
{
    /// <summary>
    /// Provides programmatic configuration for localization.
    /// </summary>
    public class LocalizationOptions
    {
        /// <summary>
        /// The relative path under application root where resource files are located.
        /// </summary>
        public string ResourcesPath { get; set; } = string.Empty;

        /// <summary>
        /// *,resx file read provider abstraction.
        /// </summary>
        public IFileProvider FileProvider { get; set; }

        /// <summary>
        /// Enable *.resx resource file
        /// </summary>
        public bool EnabledFiles { get; set; } = true;

    }
}
