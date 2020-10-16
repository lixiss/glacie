using Glacie.Abstractions;

namespace Glacie.Modules.Builder
{
    public sealed class ModuleBuilder
    {
        public void SetPhysicalPath(string path) => throw Error.NotImplemented();

        public void AddDatabase(string path) => throw Error.NotImplemented();

        public void AddDatabase(IArzDatabase database) => throw Error.NotImplemented();






    }
}
