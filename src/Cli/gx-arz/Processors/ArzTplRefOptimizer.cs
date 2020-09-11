using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Glacie.Data.Arz;

namespace Glacie.Cli.Arz.Processors
{
    internal sealed class ArzTplRefOptimizer
    {
        private ArzDatabase _database;
        private Dictionary<string, string> _remappedStrings = new Dictionary<string, string>(StringComparer.Ordinal);
        private Dictionary<string, string> _stringTransformCache = new Dictionary<string, string>();
        private bool _applyPathSeparator;
        private bool _standardPathSeparator;

        public ArzTplRefOptimizer(ArzDatabase database)
        {
            Check.Argument.NotNull(database, nameof(database));

            _database = database;

            _database.GetContext().TryInferFormat(out var _);
            _applyPathSeparator = _database.GetContext().Format.Complete;
            _standardPathSeparator = _database.GetContext().Format.StandardPathSeparator;
        }

        public ArzOptimizerResult Run(Glacie.CommandLine.UI.ProgressView? progress) // TODO: use IProgress
        {
            progress?.AddMaximumValue(_database.Count);

            var sw = Stopwatch.StartNew();
            foreach (var record in _database.GetAll())
            {
                ProcessRecord(record);

                progress?.AddValue(1);
            }
            sw.Stop();

            var estimatedSizeReduction = 0;
            if (_remappedStrings.Count > 0 && _stringTransformCache.Count > 0)
            {
                // Each string encoded as length (4 bytes) + char data in ascii.
                estimatedSizeReduction = _remappedStrings.Keys.Select(x => 4 + x.Length).Sum()
                    - _stringTransformCache.Keys.Select(x => 4 + x.Length).Sum();
            }

            // _stringTransformCache no more needed.
            _stringTransformCache = null!;

            return new ArzOptimizerResult
            {
                NumberOfRemappedStrings = _remappedStrings.Count,
                EstimatedSizeReduction = estimatedSizeReduction,
                CompletedIn = sw.Elapsed,
            };
        }

        private void ProcessRecord(ArzRecord record)
        {
            if (record.TryGet(WellKnownFieldNames.TemplateName, ArzRecordOptions.NoFieldMap, out var field))
            {
                if (field.ValueType == ArzValueType.String)
                {
                    var value = field.Get<string>(0);
                    if (TryNormalizeTemplateRef(value, out var nv))
                    {
                        record[WellKnownFieldNames.TemplateName] = nv;
                    }
                }
            }
        }

        private string CachedNormalizePathString(string value)
        {
            if (_stringTransformCache.TryGetValue(value, out var result))
            {
                return result;
            }
            else
            {
                result = value.ToLowerInvariant();
                if (_applyPathSeparator)
                {
                    if (_standardPathSeparator)
                    {
                        result = result.Replace('\\', '/');
                    }
                    else
                    {
                        result = result.Replace('/', '\\');
                    }
                }
                _stringTransformCache.Add(value, result);
                return result;
            }
        }

        private bool TryNormalizeTemplateRef(string value, out string newValue)
        {
            var v = CachedNormalizePathString(value);
            if ((object)v != value)
            {
                _remappedStrings[value] = v;
                newValue = v;
                return true;
            }
            newValue = null!;
            return false;
        }
    }
}
