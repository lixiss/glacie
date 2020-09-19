using System;

namespace Glacie.Collections
{
    // TODO: Strong vs Weak string interner
    // TODO: Symbol -> interning may return symbol (reference or index)
    public interface IStringInterner
    {
        string Intern(string value);
        string Intern(ReadOnlySpan<char> value);
    }
}
