using System;
using System.Collections.Generic;

namespace Glacie.Data.Resources.V1
{
    [Obsolete("Undecided if this interface needed.")]
    public interface IResourceCollection
    {
        IReadOnlyCollection<Resource> SelectAll();
    }
}
