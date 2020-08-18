using System;

namespace Glacie.Abstractions
{
    /// <remarks>
    /// Used to allow resolve method overloads, without having be referenced
    /// <c>Glacie.Data.Arz</c> assembly.
    /// </remarks>
    public interface IArzDatabase : IDisposable
    {
    }
}
