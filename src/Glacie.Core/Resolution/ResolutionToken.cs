using System.Collections.Generic;
using System.Threading;

namespace Glacie.Abstractions
{
    /// <summary>
    /// Each instance of this class takes unique identifier (in process),
    /// and might be used to get additional information about resolve process.
    /// It intended to use this class in private static field,
    /// as <see cref="Resolution<T>"/> type may be created only with Id.
    /// </summary>
    public sealed class ResolutionToken
    {
        private static readonly List<ResolutionToken> _tokens = new List<ResolutionToken>() { null! };

        internal static ResolutionToken GetToken(ResolutionTokenId tokenId)
        {
            lock (_tokens)
            {
                return _tokens[(int)tokenId];
            }
        }

        private readonly ResolutionTokenId _id;
        private readonly string _name;

        public ResolutionToken(string name)
        {
            _name = name;

            lock (_tokens)
            {
                _id = (ResolutionTokenId)_tokens.Count;
                _tokens.Add(this);
            }
        }

        public ResolutionTokenId Id => _id;

        public string Name => _name;
    }
}
