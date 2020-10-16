using System.Collections.Generic;

using Glacie.Localization;
using Glacie.Modules.Builder;

namespace Glacie.Resources.Builder
{
    using ResourceBundleMap = Dictionary<LanguageSymbol, List<ResourceBundleBuilder>>;

    public sealed class ResourceBundleSetBuilder : IResourceBundleSet
    {
        private ResourceBundleMap _items;

        public ResourceBundleSetBuilder()
        {
            _items = new ResourceBundleMap();
        }

        public int Count
        {
            get
            {
                int result = 0;
                foreach (var v in _items.Values)
                {
                    result += v.Count;
                }
                return result;
            }
        }

        public IReadOnlyCollection<LanguageSymbol> Languages => _items.Keys;

        public IEnumerable<IResourceBundleInfo> SelectAll()
        {
            foreach (var bundleList in _items.Values)
            {
                foreach (var bundleInfo in bundleList)
                {
                    yield return bundleInfo;
                }
            }
        }

        public IReadOnlyCollection<IResourceBundleInfo> Select(LanguageSymbol language)
            => _items[language];

        public void Add(ResourceBundleBuilder resourceBundle)
        {
            if (!_items.TryGetValue(resourceBundle.Language, out var resourceBundleList))
            {
                resourceBundleList = new List<ResourceBundleBuilder>();
                _items.Add(resourceBundle.Language, resourceBundleList);
            }
            resourceBundleList.Add(resourceBundle);
        }

        public ResourceBundleSet Build()
        {
            var items = new Dictionary<LanguageSymbol, List<ResourceBundle>>();
            foreach (var kv in _items)
            {
                var list = new List<ResourceBundle>(kv.Value.Count);
                foreach (var resourceBundleBuilder in kv.Value)
                {
                    list.Add(resourceBundleBuilder.Build());
                }
                items.Add(kv.Key, list);
            }
            return new ResourceBundleSet(items);
        }
    }
}
