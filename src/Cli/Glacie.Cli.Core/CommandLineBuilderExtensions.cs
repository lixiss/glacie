using System;
using System.Collections.Generic;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.Text;

using Glacie.Cli.Adapters;

namespace Glacie.Cli
{
    public static class CommandLineBuilderExtensions
    {
        public static CommandLineBuilder UseCli(this CommandLineBuilder builder,
            Glacie.CommandLine.UI.Terminal? terminal)
        {
            builder
                // Options below similar to .UseDefaults(), but slightly altered.
                .UseVersionOption()
                .UseHelp()
                .UseEnvironmentVariableDirective()
                .UseParseDirective()
                //.UseDebugDirective()
                //.UseSuggestDirective()
                //.RegisterWithDotnetSuggest()
                .UseTypoCorrections()
                .UseParseErrorReporting()
                .UseExceptionHandler(ExceptionHandler)
                .CancelOnProcessTermination()
                ;

            if (terminal != null)
            {
                builder.ConfigureConsole((bc) => new SysCmdLineConsoleAdapter(terminal));
                builder.UseMiddleware(async (context, next) =>
                {
                    context.BindingContext.AddService<Glacie.CommandLine.UI.Terminal>((s) => terminal);
                    await next(context);
                }, MiddlewareOrder.Configuration);
            }

            return builder;
        }

        private static void ExceptionHandler(Exception exception, InvocationContext context)
        {
            context.ResultCode = 1;

            if (!System.Console.IsOutputRedirected)
            {
                System.Console.ResetColor();
                System.Console.ForegroundColor = ConsoleColor.Red;
            }

            if (exception is System.Reflection.TargetInvocationException tiex)
            {
                exception = tiex.InnerException ?? exception;
            }

            string errorString;
            string? extendedErrorString = null;
            if (exception is CliErrorException cliErrorEx)
            {
                var message = cliErrorEx.Message;
                if (message != null)
                {
                    errorString = $"error: {cliErrorEx.Message}\n";

                    if (cliErrorEx.InnerException != null)
                    {
                        extendedErrorString = cliErrorEx.InnerException.ToString();
                    }
                }
                else
                {
                    errorString = $"error: {cliErrorEx}\n";
                }
            }
            else
            {
                errorString = $"fatal: {exception}\n";
            }
            context.Console.Error.Write(errorString);
            if (!string.IsNullOrEmpty(extendedErrorString))
            {
                context.Console.Error.Write(extendedErrorString);
            }

            if (!System.Console.IsOutputRedirected)
            {
                System.Console.ResetColor();
            }
        }
    }
}
