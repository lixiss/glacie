using System;
using System.Collections.Generic;
using System.Linq;

using Glacie.Discovery_Engines;
using Glacie.Localization;

namespace Glacie.Discovery_Modules
{
    public sealed class ModuleInfo
    {
        private readonly string? _name;
        private readonly string? _physicalPath;
        private readonly DatabaseInfo? _databaseInfo;
        private readonly Dictionary<LanguageSymbol, ResourceBundleInfo[]> _resourceBundlesMap;
        private readonly EngineFamily? _engineFamily;
        private readonly bool _ambigious;
        private readonly bool _hasResourceBundles;

        internal ModuleInfo(string? name,
            string? physicalPath,
            DatabaseInfo? databaseInfo,
            Dictionary<LanguageSymbol, ResourceBundleInfo[]> resourceBundles,
            EngineFamily? engineFamily,
            bool ambigious)
        {
            _name = name;
            _physicalPath = physicalPath;
            _databaseInfo = databaseInfo;
            _resourceBundlesMap = resourceBundles;
            _engineFamily = engineFamily;
            _ambigious = ambigious;
            _hasResourceBundles = GetHasResourceBundles(resourceBundles);
        }

        public string? Name => _name;

        public string? PhysicalPath => _physicalPath;

        public DatabaseInfo? Database => _databaseInfo;

        public IReadOnlyCollection<LanguageSymbol> Languages => _resourceBundlesMap.Keys;

        public IReadOnlyCollection<ResourceBundleInfo> GetResourceBundles(LanguageSymbol languageSymbol)
            => _resourceBundlesMap[languageSymbol];

        public EngineFamily? EngineFamily => _engineFamily;

        public bool Ambigious => _ambigious;

        public bool Exists => HasDatabase || HasResourceBundles;

        public bool HasDatabase => _databaseInfo != null;

        public bool HasResourceBundles => _hasResourceBundles;

        private static bool GetHasResourceBundles(Dictionary<LanguageSymbol, ResourceBundleInfo[]> resourceBundles)
        {
            foreach (var list in resourceBundles.Values)
            {
                if (list.Length > 0) return true;
            }
            return false;
        }
    }
}
