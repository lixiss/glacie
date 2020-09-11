namespace Glacie.CommandLine.Rendering
{
    // TODO: Should able to render to regions and support different output modes.
    // TODO: it should not be abstract, because we may want render to non-IConsole or non-ITerminal targets.
    // for example rendering to markdown or HTML.

    // TODO: Add Spans and Views: views is kind of document tree, spans is pieces of that document,
    // usually text, color, control codes or other. (However document should not use ansi codes directly).

    internal class Renderer // ConsoleRenderer?
    {
        protected Renderer(IConsole console) // OutputMode mode = OutputMode.Auto, reset after render?
        {
        }
    }
}
