// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Resources;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.FileProviders;
using System.Linq;
using System.Globalization;

namespace Microsoft.Extensions.Localization
{
    /// <summary>
    /// An <see cref="IStringLocalizerFactory"/> that creates instances of <see cref="ResourceManagerStringLocalizer"/>.
    /// </summary>
    public class ResourceManagerStringLocalizerFactory : IStringLocalizerFactory
    {
        private readonly IResourceNamesCache _resourceNamesCache = new ResourceNamesCache();
        private readonly ConcurrentDictionary<string, ResourceManagerStringLocalizer> _localizerCache =
            new ConcurrentDictionary<string, ResourceManagerStringLocalizer>();
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly string _resourcesRelativePath;
        private readonly string _ResourcePath;
        private readonly IFileProvider _fileProvider;
        /// <summary>
        /// Creates a new <see cref="ResourceManagerStringLocalizer"/>.
        /// </summary>
        /// <param name="hostingEnvironment">The <see cref="IHostingEnvironment"/>.</param>
        /// <param name="localizationOptions">The <see cref="IOptions{LocalizationOptions}"/>.</param>
        public ResourceManagerStringLocalizerFactory(
            IHostingEnvironment hostingEnvironment,
            IOptions<LocalizationOptions> localizationOptions)
        {
            if (hostingEnvironment == null)
            {
                throw new ArgumentNullException(nameof(hostingEnvironment));
            }

            if (localizationOptions == null)
            {
                throw new ArgumentNullException(nameof(localizationOptions));
            }
            
            _hostingEnvironment = hostingEnvironment;
            _resourcesRelativePath = localizationOptions.Value.ResourcesPath ?? string.Empty;
            _fileProvider = localizationOptions.Value.FileProvider ?? _hostingEnvironment.ContentRootFileProvider;
            if (!string.IsNullOrEmpty(_resourcesRelativePath))
            {
                _ResourcePath += _resourcesRelativePath.TrimEnd(Path.AltDirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar) + "/";
                _resourcesRelativePath = _resourcesRelativePath.Replace(Path.AltDirectorySeparatorChar, '.')
                    .Replace(Path.DirectorySeparatorChar, '.') + ".";
            }
        }
        

        /// <summary>
        /// Creates a <see cref="ResourceManagerStringLocalizer"/> using the <see cref="Assembly"/> and
        /// <see cref="Type.FullName"/> of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="resourceSource">The <see cref="Type"/>.</param>
        /// <returns>The <see cref="ResourceManagerStringLocalizer"/>.</returns>
        public IStringLocalizer Create(Type resourceSource)
        {
            if (resourceSource == null)
            {
                throw new ArgumentNullException(nameof(resourceSource));
            }

            var typeInfo = resourceSource.GetTypeInfo();
            var assembly = typeInfo.Assembly;

            string pathName = TrimPrefix(typeInfo.FullName, _hostingEnvironment.ApplicationName + ".");
            // Re-root the base name if a resources path is set
            var baseName = string.IsNullOrEmpty(_resourcesRelativePath)
                ? typeInfo.FullName
                : _hostingEnvironment.ApplicationName + "." + _resourcesRelativePath + pathName;

            return _localizerCache.GetOrAdd(baseName, _ =>
                new ResourceManagerStringLocalizer(
                    new ResourceManager(baseName, assembly),
                    assembly,
                    baseName,
                    _resourceNamesCache,
                    _fileProvider,
                    _ResourcePath,
                    pathName)
            );
        }

        /// <summary>
        /// Creates a <see cref="ResourceManagerStringLocalizer"/>.
        /// </summary>
        /// <param name="baseName">The base name of the resource to load strings from.</param>
        /// <param name="location">The location to load resources from.</param>
        /// <returns>The <see cref="ResourceManagerStringLocalizer"/>.</returns>
        public IStringLocalizer Create(string baseName, string location)
        {
            if (baseName == null)
            {
                throw new ArgumentNullException(nameof(baseName));
            }

            location = location ?? _hostingEnvironment.ApplicationName;
            string pathName = TrimPrefix(baseName, location + ".");
            baseName = location + "." + _resourcesRelativePath + pathName;
            return _localizerCache.GetOrAdd($"B={baseName},L={location}", _ =>
            {
                var assembly = Assembly.Load(new AssemblyName(location));
                return new ResourceManagerStringLocalizer(
                    new ResourceManager(baseName, assembly),
                    assembly,
                    baseName,
                    _resourceNamesCache, _fileProvider, _ResourcePath, pathName);
            });
        }

        private static string TrimPrefix(string name, string prefix)
        {
            if (name.StartsWith(prefix, StringComparison.Ordinal))
            {
                return name.Substring(prefix.Length);
            }

            return name;
        }

    }
}