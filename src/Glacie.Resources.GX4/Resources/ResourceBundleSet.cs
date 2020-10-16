using System;
using System.Collections.Generic;

using Glacie.Localization;

namespace Glacie.Resources
{
    public sealed class ResourceBundleSet : IResourceBundleSet, IDisposable
    {
        private Dictionary<LanguageSymbol, List<ResourceBundle>> _items;
        private bool _disposed;

        public ResourceBundleSet()
        {
            _items = new Dictionary<LanguageSymbol, List<ResourceBundle>>();
        }

        internal ResourceBundleSet(Dictionary<LanguageSymbol, List<ResourceBundle>> items)
        {
            _items = items;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                List<Exception>? exceptions = null;

                var items = _items;
                if (items != null)
                {
                    _items = null!;
                    foreach (var x in items.Values)
                    {
                        foreach (var y in x)
                        {
                            try
                            {
                                y.Dispose();
                            }
                            catch (Exception ex)
                            {
                                if (exceptions == null) exceptions = new List<Exception>();
                                exceptions.Add(ex);
                            }
                        }
                    }
                }

                _disposed = true;

                if (exceptions != null)
                {
                    throw new AggregateException(exceptions);
                }
            }
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
        {
            if (_items.TryGetValue(language, out var result)) return result;
            else return Array.Empty<IResourceBundleInfo>();
        }

        public void Open()
        {
            // TODO: Implement ResourceBundleSet opening...
        }
    }
}
