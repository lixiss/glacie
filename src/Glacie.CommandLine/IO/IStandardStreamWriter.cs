using System;

namespace Glacie.CommandLine.IO
{
    public interface IStandardStreamWriter
    {
        //void Write(char value);
        //void Write(char[]? buffer);
        //void Write(char[]? buffer, int index, int count);
        //void Write(ReadOnlySpan<char> buffer);
        void Write(string? value);

        //void WriteLine();
        //void WriteLine(char value);
        //void WriteLine(char[]? buffer);
        //void WriteLine(char[]? buffer, int index, int count);
        //void WriteLine(ReadOnlySpan<char> buffer);
        //void WriteLine(string? value);
    }
}
