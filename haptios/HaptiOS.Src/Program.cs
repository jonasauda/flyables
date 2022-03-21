using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace HaptiOS.Src
{
    public class Program
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static void Main(string[] args)
        {
            var host = BuildWebHost(args);

            using (host)
            {
                host.Start();
                Logger.Info("Use Ctrl-C to shutdown the host...");
                host.WaitForShutdown();
            }
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());

                    // application specific configuration
                    // personal settings should be set inside the Development.json
                    config.AddJsonFile("haptios.config.json", optional : false, reloadOnChange : true);
                    config.AddJsonFile("haptios.config.Development.json", optional : true, reloadOnChange : true);
                })
                .UseStartup<Startup>()
                .Build();
        }
    }
}