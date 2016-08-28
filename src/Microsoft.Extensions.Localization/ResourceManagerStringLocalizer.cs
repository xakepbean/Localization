// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Resources;
using Microsoft.Extensions.Localization.Internal;
using Microsoft.Extensions.FileProviders;
using System.Xml.Linq;
using System.Linq;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Localization
{
    /// <summary>
    /// An <see cref="IStringLocalizer"/> that uses the <see cref="ResourceManager"/> and
    /// <see cref="ResourceReader"/> to provide localized strings.
    /// </summary>
    /// <remarks>This type is thread-safe.</remarks>
    public class ResourceManagerStringLocalizer : IStringLocalizer
    {
        private readonly ConcurrentDictionary<string, object> _missingManifestCache = new ConcurrentDictionary<string, object>();
        private readonly IResourceNamesCache _resourceNamesCache;
        private readonly ResourceManager _resourceManager;
        private readonly IResourceStringProvider _resourceStringProvider;
        private readonly string _resourceBaseName;
        private readonly string _resourcePath;
        private readonly IFileProvider _fileProvider;

        protected internal List<string> CultureFileCache { get; set; }
        protected internal string PathName { get; set; }

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _fileResourceCache=new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>();

        /// <summary>
        /// Creates a new <see cref="ResourceManagerStringLocalizer"/>.
        /// </summary>
        /// <param name="resourceManager">The <see cref="ResourceManager"/> to read strings from.</param>
        /// <param name="resourceAssembly">The <see cref="Assembly"/> that contains the strings as embedded resources.</param>
        /// <param name="baseName">The base name of the embedded resource that contains the strings.</param>
        /// <param name="resourceNamesCache">Cache of the list of strings for a given resource assembly name.</param>
        /// <param name="fileProvider"></param>
        /// <param name="resourcePath"></param>
        /// <param name="pathName">´æ´¢Â·¾¶</param>
        public ResourceManagerStringLocalizer(
            ResourceManager resourceManager,
            Assembly resourceAssembly,
            string baseName,
            IResourceNamesCache resourceNamesCache, IFileProvider fileProvider, string resourcePath, string pathName)
            : this(
                  resourceManager,
                  new AssemblyResourceStringProvider(
                      resourceNamesCache,
                      new AssemblyWrapper(resourceAssembly),
                      baseName),
                  baseName,
                  resourceNamesCache,
                  fileProvider,
                  resourcePath,
                  pathName)
        {
            if (resourceAssembly == null)
            {
                throw new ArgumentNullException(nameof(resourceAssembly));
            }
        }

        /// <summary>
        /// Intended for testing purposes only.
        /// </summary>
        public ResourceManagerStringLocalizer(
            ResourceManager resourceManager,
            IResourceStringProvider resourceStringProvider,
            string baseName,
            IResourceNamesCache resourceNamesCache, IFileProvider fileProvider,string resourcePath, string pathName)
        {
            if (resourceManager == null)
            {
                throw new ArgumentNullException(nameof(resourceManager));
            }

            if (resourceStringProvider == null)
            {
                throw new ArgumentNullException(nameof(resourceStringProvider));
            }

            if (baseName == null)
            {
                throw new ArgumentNullException(nameof(baseName));
            }

            if (resourceNamesCache == null)
            {
                throw new ArgumentNullException(nameof(resourceNamesCache));
            }
            
            _fileProvider = fileProvider;
            _resourcePath = resourcePath;
            PathName = pathName;
           
            _resourceStringProvider = resourceStringProvider;
            _resourceManager = resourceManager;
            _resourceBaseName = baseName;
            _resourceNamesCache = resourceNamesCache;
            CultureFileCache = new List<string>();
        }

        /// <inheritdoc />
        public virtual LocalizedString this[string name]
        {
            get
            {
                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                var value = GetStringSafely(name, null);
                return new LocalizedString(name, value ?? name, resourceNotFound: value == null);
            }
        }

        /// <inheritdoc />
        public virtual LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                var format = GetStringSafely(name, null);
                var value = string.Format(format ?? name, arguments);
                return new LocalizedString(name, value, resourceNotFound: format == null);
            }
        }

        /// <summary>
        /// Creates a new <see cref="ResourceManagerStringLocalizer"/> for a specific <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="culture">The <see cref="CultureInfo"/> to use.</param>
        /// <returns>A culture-specific <see cref="ResourceManagerStringLocalizer"/>.</returns>
        public IStringLocalizer WithCulture(CultureInfo culture)
        {
            return culture == null
                ? new ResourceManagerStringLocalizer(
                    _resourceManager,
                    _resourceStringProvider,
                    _resourceBaseName,
                    _resourceNamesCache,
                    _fileProvider,
                    _resourcePath,
                    PathName)
                : new ResourceManagerWithCultureStringLocalizer(
                    _resourceManager,
                    _resourceStringProvider,
                    _resourceBaseName,
                    _resourceNamesCache,
                    culture, 
                    _fileProvider,
                    _resourcePath,
                    PathName);
        }

        /// <inheritdoc />
        public virtual IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            GetAllStrings(includeParentCultures, CultureInfo.CurrentUICulture);

        /// <summary>
        /// Returns all strings in the specified culture.
        /// </summary>
        /// <param name="includeParentCultures"></param>
        /// <param name="culture">The <see cref="CultureInfo"/> to get strings for.</param>
        /// <returns>The strings.</returns>
        protected IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures, CultureInfo culture)
        {
            if (culture == null)
            {
                throw new ArgumentNullException(nameof(culture));
            }

            var resourceNames = includeParentCultures
                ? GetResourceNamesFromCultureHierarchy(culture)
                : _resourceStringProvider.GetAllResourceStrings(culture, true);

            foreach (var name in resourceNames)
            {
                var value = GetStringSafely(name, culture);
                yield return new LocalizedString(name, value ?? name, resourceNotFound: value == null);
            }
        }

        /// <summary>
        /// Gets a resource string from the <see cref="_resourceManager"/> and returns <c>null</c> instead of
        /// throwing exceptions if a match isn't found.
        /// </summary>
        /// <param name="name">The name of the string resource.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to get the string for.</param>
        /// <returns>The resource string, or <c>null</c> if none was found.</returns>
        protected string GetStringSafely(string name, CultureInfo culture)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            var cultureName = (culture ?? CultureInfo.CurrentUICulture).Name;
            var cacheKey = $"name={name}&culture={cultureName}";

            if (_missingManifestCache.ContainsKey(cacheKey))
            {
                return null;
            }

            try
            {
                var fileCacheKey= $"culture={PathName}.{cultureName}.resx".ToLower();
                if (!_missingManifestCache.ContainsKey(fileCacheKey))
                {
                    if (_fileResourceCache.ContainsKey(fileCacheKey))
                    {
                        if (_fileResourceCache[fileCacheKey].ContainsKey(name))
                        {
                            return _fileResourceCache[fileCacheKey][name];
                        }
                    }
                    else
                    {
                        if (!CultureFileCache.Contains(fileCacheKey))
                            CultureFileCache.Add(fileCacheKey);
                        IChangeToken CToken;
                        ConcurrentDictionary<string, string> _cultureResourceCache = GetFileChangeTokenResource(cultureName,out CToken);
                        if (CToken!=null && CToken.ActiveChangeCallbacks)
                        {
                            CToken.RegisterChangeCallback(itemKey =>
                            {
                                if (_fileResourceCache.ContainsKey(fileCacheKey))
                                {
                                    ConcurrentDictionary<string, string> outCache;
                                    _fileResourceCache.TryRemove(fileCacheKey, out outCache);
                                }
                                if (_missingManifestCache.ContainsKey(fileCacheKey))
                                {
                                    object outobject;
                                    _missingManifestCache.TryRemove(fileCacheKey, out outobject);
                                }
                            }, null);
                        }
                        if (_cultureResourceCache == null)
                        {
                            _missingManifestCache.TryAdd(fileCacheKey, null);
                        }
                        else
                        {
                            _fileResourceCache.TryAdd(fileCacheKey, _cultureResourceCache);
                            if (_cultureResourceCache.ContainsKey(name))
                            {
                                return _cultureResourceCache[name];
                            }
                        }
                    }
                }
                return culture == null ?  _resourceManager.GetString(name) : _resourceManager.GetString(name, culture);
            }
            catch (MissingManifestResourceException)
            {
                _missingManifestCache.TryAdd(cacheKey, null);
                return null;
            }
        }

        private ConcurrentDictionary<string, string> GetFileChangeTokenResource(string cultureName, out IChangeToken CToken)
        {
            string subPath = $"{_resourcePath}{PathName.Replace('.', '/')}.{cultureName}.resx";
            ConcurrentDictionary<string, string> _cultureResourceCache = GetFileResource(subPath, out CToken);
            if (_cultureResourceCache == null)
            {
                var dotsubPath = $"{_resourcePath}{PathName}.{cultureName}.resx";
                _cultureResourceCache = GetFileResource(dotsubPath, out CToken);
                if (_cultureResourceCache == null)
                {
                    CToken = _fileProvider.Watch(subPath);
                }
            }
            return _cultureResourceCache;
        }

        private ConcurrentDictionary<string, string> GetFileResource(string subPath, out IChangeToken CToken)
        {
            ConcurrentDictionary<string, string> _cultureResourceCache = null;
            var vFileInfo = _fileProvider.GetFileInfo(subPath);
            if (vFileInfo.Exists)
            {
                var stream = vFileInfo.CreateReadStream();
                XElement XRoot = XElement.Load(stream);
                var rData = XRoot.Elements("data").Where(w => w.Attribute("name") != null && w.Element("value") != null);
                if (rData.Count() > 0)
                {
                    _cultureResourceCache= new ConcurrentDictionary<string, string>();
                    foreach (var item in rData)
                    {
                        _cultureResourceCache.TryAdd(item.Attribute("name").Value, item.Element("value").Value);
                    }
                }
                CToken = _fileProvider.Watch(subPath);
            }
            else
                CToken = null;
            return _cultureResourceCache;
        }

        private IEnumerable<string> GetResourceNamesFromCultureHierarchy(CultureInfo startingCulture)
        {
            var currentCulture = startingCulture;
            var resourceNames = new HashSet<string>();

            var hasAnyCultures = false;

            while (true)
            {

                var cultureResourceNames = _resourceStringProvider.GetAllResourceStrings(currentCulture, false);

                if (cultureResourceNames != null)
                {
                    foreach (var resourceName in cultureResourceNames)
                    {
                        resourceNames.Add(resourceName);
                    }
                    hasAnyCultures = true;
                }

                if (currentCulture == currentCulture.Parent)
                {
                    // currentCulture begat currentCulture, probably time to leave
                    break;
                }

                currentCulture = currentCulture.Parent;
            }

            if (!hasAnyCultures)
            {
                throw new MissingManifestResourceException(Resources.Localization_MissingManifest_Parent);
            }

            return resourceNames;
        }
    }
}