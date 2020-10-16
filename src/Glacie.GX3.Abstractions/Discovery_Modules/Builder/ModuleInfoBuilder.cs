using System;
using System.Collections.Generic;

using Glacie.Discovery_Engines;
using Glacie.Localization;

using IO = System.IO;

namespace Glacie.Discovery_Modules.Builder
{
    public sealed class ModuleInfoBuilder
    {
        public const short DefaultPriorityClass = 0;
        public const short OverridePriorityClass = 2;

        private bool _built;
        private bool _ambigious;
        private string? _physicalPath;
        private DatabaseInfoBuilder? _databaseInfo;
        private EngineFamily? _engineFamily;
        private readonly Dictionary<LanguageSymbol, List<ResourceBundleInfoBuilder>> _resourceBundlesMap;

        public ModuleInfoBuilder()
        {
            _resourceBundlesMap = new Dictionary<LanguageSymbol, List<ResourceBundleInfoBuilder>>();
        }

        public void SetPhysicalPath(string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                value = IO.Path.GetFullPath(value);
            }

            _physicalPath = value;
        }

        public void AddEngineFamily(EngineFamily value)
        {
            if (_engineFamily == null)
            {
                _engineFamily = value;
            }
            else if (_engineFamily.Value != value)
            {
                _ambigious = true;
            }
        }

        public void AddDatabase(string physicalPath)
        {
            Check.Argument.NotNullNorEmpty(physicalPath, nameof(physicalPath));

            if (_databaseInfo == null)
            {
                _databaseInfo = new DatabaseInfoBuilder(physicalPath);
            }
            else
            {
                _ambigious = true;
            }
        }

        public void AddResourceBundle(
            string physicalPath,
            ResourceBundleKind kind,
            short priorityClass,
            string prefix,
            Language language)
        {
            var languageSymbol = language.Symbol;

            var priority = GetPriority(priorityClass, languageSymbol);

            var resourceBundleInfo = new ResourceBundleInfoBuilder(kind, priority, physicalPath, prefix, languageSymbol);

            // TODO: normalize language codes before use them as key (better to pass here language symbol)
            // TODO: split by language, but also we might decide to treat EN as invariant

            {
                if (!_resourceBundlesMap.TryGetValue(languageSymbol, out var resourceBundleList))
                {
                    resourceBundleList = new List<ResourceBundleInfoBuilder>();
                    _resourceBundlesMap.Add(languageSymbol, resourceBundleList);
                }
                resourceBundleList.Add(resourceBundleInfo);
            }
        }


        public ModuleInfo Build()
        {
            Check.That(!_built);
            _built = true;

            var physicalPath = EmptyToNull(_physicalPath);
            var ambigious = _ambigious;
            var engineFamily = _engineFamily;

            var databaseInfo = _databaseInfo?.Build(physicalPath);

            string? name;
            if (databaseInfo != null)
            {
                name = IO.Path.GetFileNameWithoutExtension(databaseInfo.RelativePath);
                if ("database".Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    name = null;
                }
            }
            else
            {
                name = null;
            }


            Dictionary<LanguageSymbol, ResourceBundleInfo[]> bundleMap = new Dictionary<LanguageSymbol, ResourceBundleInfo[]>();
            foreach (var kv in _resourceBundlesMap)
            {
                var list = Map(kv.Value, physicalPath);
                if (list != null)
                {
                    bundleMap.Add(kv.Key, list);
                }
            }

            return new ModuleInfo(name,
                physicalPath,
                databaseInfo,
                bundleMap,
                engineFamily,
                ambigious);
        }

        private static ResourceBundleInfo[]? Map(List<ResourceBundleInfoBuilder> value, string? physicalPath)
        {
            if (value == null || value.Count == 0) return null;

            var result = new ResourceBundleInfo[value.Count];
            int i = 0;
            foreach (var rbib in value)
            {
                result[i] = rbib.Build(physicalPath);
                i++;
            }
            return result;
        }

        private static string? EmptyToNull(string? value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            return value;
        }

        private short GetPriority(short priorityClass, LanguageSymbol languageSymbol)
        {
            if (languageSymbol == LanguageSymbol.Invariant) return priorityClass;
            else return checked((short)(priorityClass + 1));
        }

        private string? GetRelativePathOrNull(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            if (string.IsNullOrEmpty(_physicalPath))
            {
                return path;
            }
            else
            {
                return IO.Path.GetRelativePath(_physicalPath, path);
            }
        }
    }
}
