namespace Glacie.Metadata.Builder.Infrastructure
{
    public abstract class Builder<TBuiltType>
        where TBuiltType : class
    {
        private TBuiltType? _builtObject;

        protected internal Builder()
        {
            _builtObject = null;
        }

        public TBuiltType Build()
        {
            if (_builtObject != null) return _builtObject;
            return _builtObject = BuildCore();
        }

        protected abstract TBuiltType BuildCore();

        protected void ThrowIfBuilt()
        {
            if (!(_builtObject is null))
            {
                throw Error.InvalidOperation("{0} is already built and can't be modified.", GetType().ToString());
            }
        }
    }
}
