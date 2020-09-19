using System;
using System.Collections.Generic;
using System.Text;

using Glacie.Diagnostics;

namespace Glacie.Data.Tpl
{
    internal static class TokenExtensions
    {
        public static Location ToLocation(this in Token token, string? path)
        {
            return Location.Create(path, token.LineNumber, token.LinePosition);
        }
    }
}
