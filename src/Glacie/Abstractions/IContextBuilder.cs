using System;
using Glacie.Data.Arz;

namespace Glacie.Abstractions
{
    // TODO: (Gx) Configure logging and diagnostics bag
    public interface IContextBuilder
    {
        void Source(Action<ISourceBuilder> configureSource);

        void Target(Action<ITargetBuilder> configureTarget);


        public void Source(string sourcePath)
            => Source(x => x.Path(sourcePath));

        public void Target(string targetPath)
            => Target(x => x.Path(targetPath));

        public void Source(IArzDatabase sourceDatabase)
            => Source(x => x.Database((ArzDatabase)sourceDatabase));

        public void Target(IArzDatabase targetDatabase)
            => Target(x => x.Database((ArzDatabase)targetDatabase));
    }
}
