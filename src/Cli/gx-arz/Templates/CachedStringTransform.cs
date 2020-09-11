using System;
using System.Collections.Generic;

namespace Glacie.Cli.Arz.Templates
{
    // TODO: This is dirty.

    internal sealed class CachedStringTransform
    {
        private Dictionary<string, string> _map;

        public CachedStringTransform()
        {
            _map = new Dictionary<string, string>();
        }

        public string Get(string value)
        {
            if (!_map.TryGetValue(value, out var mappedValue))
            {
                mappedValue = Transform(value);
                _map.Add(value, mappedValue);
            }
            return mappedValue;
        }

        private string Transform(string value)
        {
            // TODO: Change to froward-slashes, but comparsion also should be slash-invariant.
            // .Replace('\\', '/');
            return value.ToLowerInvariant();
        }
    }
}
