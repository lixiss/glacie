﻿namespace Glacie.Data.Tpl
{
    internal enum TokenType
    {
        EndOfStream,

        Assignment,
        BeginBlock,
        EndBlock,

        StringLiteral,

        Group,
        Variable,
        FileNameHistoryEntry,

        Name,
        Class,
        Type,
        Description,
        Value,
        DefaultValue,

        // Error tokens.
        UnexpectedEndOfStreamInStringLiteral,
        Unknown,
    }
}
