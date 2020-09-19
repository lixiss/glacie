using Glacie.CommandLine;

namespace Glacie.Lab
{
    public abstract class Command
    {
        public static T Create<T>(IConsole console) where T : Command, new()
        {
            var command = new T();
            command.Console = console;
            return command;
        }

        protected IConsole Console { get; private set; } = default!;


        protected abstract void RunCore();

        public void Run()
        {
            RunCore();
        }
    }
}
