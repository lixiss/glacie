using System.Collections.Generic;

namespace Glacie.Data.Arz.Infrastructure
{
    public interface IArzStringEncoderFactory
    {
        ArzStringEncoder Create(ArzDatabase database, List<ArzRecord> records);
    }
}
