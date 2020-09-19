namespace Glacie.Data.Tpl
{
    internal readonly struct Token
    {
        public readonly TokenType Type;

        public readonly int LineNumber;
        public readonly int LinePosition;

        public readonly int Index;
        public readonly int Length;

        public Token(TokenType type, int line, int column)
        {
            Type = type;
            LineNumber = line;
            LinePosition = column;
            Index = 0;
            Length = 0;
        }

        public Token(TokenType type, int line, int column, int index, int length)
        {
            Type = type;
            LineNumber = line;
            LinePosition = column;
            Index = index;
            Length = length;
        }
    }
}
