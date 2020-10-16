using System;

using Glacie.Configuration;
using Glacie.Targeting;

namespace Glacie
{
    [Obsolete("This is obsolete class, Project or ProjectContext will create Context.", true)]
    internal static class ContextBuilder
    {
        public static Context CreateContext(EngineType engineType, ContextConfiguration configuration)
        {
            throw Error.NotSupported("");
            // TODO: Move construction logic here.
            // return new Context(engineType, configuration);
        }
    }
}
