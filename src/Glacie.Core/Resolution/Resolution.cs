using System;
using System.Diagnostics.CodeAnalysis;

namespace Glacie.Abstractions
{
    // Intended to be used in Resolvers.
    // See MetadataResolver for example.

    public readonly struct Resolution<T>
    {
        private readonly T _value;
        private readonly bool _hasValue;
        private readonly ResolutionTokenId _tokenId;

        public Resolution(T value, bool resolved)
        {
            _value = value;
            _hasValue = resolved;
            _tokenId = ResolutionTokenId.None;
        }

        private Resolution(T value, bool resolved, ResolutionTokenId tokenId)
        {
            _value = value;
            _hasValue = resolved;
            _tokenId = tokenId;
        }

        public bool HasValue => _hasValue;

        public T Value
        {
            get
            {
                if (!_hasValue) ThrowNoValue(); 
                return _value;
            }
        }

        private ResolutionTokenId TokenId => _tokenId;

        public bool HasToken => _tokenId != ResolutionTokenId.None;

        public ResolutionToken Token
        {
            get
            {
                if (_tokenId == ResolutionTokenId.None) ThrowNoToken();
                return ResolutionToken.GetToken(_tokenId);
            }
        }

        public Resolution<T> WithToken(ResolutionToken resolutionToken)
        {
            return new Resolution<T>(_value, _hasValue, resolutionToken.Id);
        }

        [DoesNotReturn]
        private void ThrowNoValue()
        {
            throw Error.InvalidOperation("Resolution has no associated value.");
        }

        [DoesNotReturn]
        private void ThrowNoToken()
        {
            throw Error.InvalidOperation("Resolution has no associated token.");
        }
    }
}
