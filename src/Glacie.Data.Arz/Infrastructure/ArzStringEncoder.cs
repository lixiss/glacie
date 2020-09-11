namespace Glacie.Data.Arz.Infrastructure
{
    public sealed class ArzStringEncoder
    {
        private readonly arz_string_id[] _map;
        private readonly ArzStringTable _source;
        private readonly ArzStringTable _target;

        public ArzStringEncoder(ArzStringTable source, ArzStringTable target)
        {
            // TODO: allocate uninitialized array and then fill it with special value (-1)

            _map = new arz_string_id[source.Count];
            _source = source;
            _target = target;
        }

        public ArzStringTable SourceStringTable => _source;

        public ArzStringTable TargetStringTable => _target;

        public arz_string_id Encode(arz_string_id sourceStringIndex)
        {
            var encodedValue = _map[(int)sourceStringIndex];
            if (encodedValue != 0)
            {
                return encodedValue;
            }
            else
            {
                // lock (_map)
                {
                    // TODO: (Low) (ArzStringEncoder) Might use Add instead of GetOrAdd, but then will need track not mapped not by zero, but with special value (like -1).
                    // This might be achieved by allocate uninitialized array and fill it with special value.
                    encodedValue = _target.GetOrAdd(_source[sourceStringIndex]);
                    return _map[(int)sourceStringIndex] = encodedValue;
                }
            }
        }
    }
}
