using Glacie.Hosting;

namespace Glacie.GX2.Lab
{
    public class Usage1
    {
        public static void Run()
        {
            // IHost - ILoggerFactory
            //   resolve ILogger<TCategory>
            using var host = Host
                .CreateBuilder()
                .ConfigureServices((ctx, services) =>
                {
                    // AddLogging
                })
                .Build();
        }
    }
}
