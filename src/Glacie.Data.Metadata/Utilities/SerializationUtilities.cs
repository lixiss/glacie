using Glacie.Data.Arz;

namespace Glacie.Metadata
{
    internal static class SerializationUtilities
    {
        public static string FormatToString(ArzValueType value)
        {
            return value switch
            {
                ArzValueType.Integer => "int",
                ArzValueType.Real => "real",
                ArzValueType.String => "string",
                ArzValueType.Boolean => "bool",
                _ => throw Error.Argument(nameof(value)),
            };
        }

        public static string FormatToString(bool value)
        {
            return value switch
            {
                true => "true",
                false => "false",
            };
        }
    }
}
