using System;
using System.Collections.Generic;

using Glacie.GX1.Abstractions;

namespace Glacie.GX1.Discovery
{
    /// <summary>
    /// PhysicalPath is base path for any other paths.
    /// </summary>
    public sealed class ModuleInfo
    {
        internal const short DefaultPriority = 0;
        internal const short DefaultLanguagePriority = 1;
        internal const short OverridePriority = 2;

        private string? _name;
        private string? _databasePath;
        private readonly Dictionary<string, List<ResourceBundleInfo>> _resourceBundlesMap;
        private bool _ambigious;
        private EngineFamily? _engineFamily;

        internal ModuleInfo(string physicalPath)
        {
            PhysicalPath = physicalPath;
            _resourceBundlesMap = new Dictionary<string, List<ResourceBundleInfo>>(StringComparer.OrdinalIgnoreCase);
        }

        public string PhysicalPath { get; }

        public string? DatabasePath => _databasePath;

        public string? Name => _name;

        public IReadOnlyCollection<string> Languages => _resourceBundlesMap.Keys;

        public IReadOnlyCollection<ResourceBundleInfo> GetResourceBundlesByLanguage(string? language)
        {
            return _resourceBundlesMap[language ?? ""];
        }

        public bool HasDatabase => _databasePath != null;

        public bool HasResources => _resourceBundlesMap.Count > 0;

        public bool Ambigious => _ambigious;

        public EngineFamily? EngineFamily => _engineFamily;

        internal void SetDatabasePath(string value)
        {
            Check.That(_databasePath == null);

            _databasePath = GetRelativePath(value);
            _name = System.IO.Path.GetFileNameWithoutExtension(_databasePath);
        }

        internal void SetAmbigious()
        {
            _ambigious = true;
        }

        internal void SetEngineFamily(EngineFamily engineFamily)
        {
            if (_engineFamily == null)
            {
                _engineFamily = engineFamily;
            }
            else if (_engineFamily.Value != engineFamily)
            {
                SetAmbigious();
            }
        }

        internal void AddBundle(ResourceBundleKind kind, short priority, string physicalPath, string prefix, string? language)
        {
            var bundlePath = GetRelativePath(physicalPath);

            var languageKey = GetLanguageKey(language);
            if (priority == DefaultPriority)
            {
                if (!string.IsNullOrEmpty(languageKey))
                {
                    priority = DefaultLanguagePriority;
                }
            }

            var resourceBundleInfo = new ResourceBundleInfo(kind, priority, bundlePath, prefix, language);

            // TODO: normalize language codes before use them as key (better to pass here language symbol)
            // TODO: split by language, but also we might decide to treat EN as invariant

            {
                if (!_resourceBundlesMap.TryGetValue(languageKey, out var resourceBundleList))
                {
                    resourceBundleList = new List<ResourceBundleInfo>();
                    _resourceBundlesMap.Add(languageKey, resourceBundleList);
                }
                resourceBundleList.Add(resourceBundleInfo);
            }
        }

        private string GetRelativePath(string path)
        {
            return System.IO.Path.GetRelativePath(PhysicalPath, path);
        }

        private string GetLanguageKey(string? language)
        {
            if (string.IsNullOrEmpty(language)) return "";
            else return language;
        }
    }
}
