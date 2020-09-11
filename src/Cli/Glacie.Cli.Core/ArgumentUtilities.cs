using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.Globalization;
using System.Linq;
using System.Text;

using Glacie.Data.Arc;
using Glacie.Data.Arz;
using Glacie.Data.Compression;

namespace Glacie.Cli
{
    public static class ArgumentUtilities
    {
        private const CompressionLevel DefaultCompressionLevel = CompressionLevel.Maximum;

        public static CompressionLevel ParseArcCompressionLevel(ArgumentResult result)
        {
            if (result.Tokens.Count == 0) return DefaultCompressionLevel;

            var tokenValue = result.Tokens.Single().Value;
            if (int.TryParse(tokenValue, NumberStyles.None, CultureInfo.InvariantCulture, out var intValue))
            {
                if (0 <= intValue && intValue <= (int)CompressionLevel.Maximum)
                {
                    return (CompressionLevel)intValue;
                }
                else
                {
                    result.ErrorMessage = "Cannot parse argument '{0}' for option '{1}'.".FormatWith(tokenValue, result.Argument.Name);
                    return default;
                }
            }

            switch (tokenValue.ToLowerInvariant())
            {
                case "none":
                case "no":
                case "nocompression":
                case "store":
                    return CompressionLevel.NoCompression;

                case "fastest":
                    return CompressionLevel.Fastest;

                case "max":
                case "maximum":
                    return CompressionLevel.Maximum;

                default:
                    result.ErrorMessage = "Cannot parse argument '{0}' for option '{1}'.".FormatWith(tokenValue, result.Argument.Name);
                    return default;
            }
        }

        public static CompressionLevel ParseArzCompressionLevel(ArgumentResult result)
        {
            // TODO: Arc allow no-compression / store, but arz doesn't allow.
            return ParseArcCompressionLevel(result);
        }

        public static ArcFileFormat ParseArcFileFormat(ArgumentResult parsed)
        {
            if (parsed.Tokens.Count == 0) return default;

            var tokenValue = parsed.Tokens.Single().Value;

            if (ArcFileFormat.TryParse(tokenValue, out var result))
            {
                return result;
            }

            switch (tokenValue.ToLowerInvariant())
            {
                case "auto":
                    return default;

                case "1":
                case "tq":
                case "tqit":
                case "tqae":
                    return ArcFileFormat.FromVersion(1);

                case "3":
                case "gd":
                    return ArcFileFormat.FromVersion(3);

                default:
                    parsed.ErrorMessage = "Cannot parse argument '{0}' for option '{1}'.".FormatWith(tokenValue, parsed.Argument.Name);
                    return default;
            }
        }

        public static ArzFileFormat ParseArzFileFormat(ArgumentResult parsed)
        {
            if (parsed.Tokens.Count == 0) return default;

            var tokenValue = parsed.Tokens.Single().Value;

            if (ArzFileFormat.TryParse(tokenValue, out var result))
            {
                return result;
            }

            switch (tokenValue.ToLowerInvariant())
            {
                case "none":
                case "auto":
                case "automatic":
                    return ArzFileFormat.Automatic;

                default:
                    parsed.ErrorMessage = "Cannot parse argument '{0}' for option '{1}'.".FormatWith(tokenValue, parsed.Argument.Name);
                    return default;
            }
        }
    }
}
