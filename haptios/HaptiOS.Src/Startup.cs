using System;
using System.Threading.Tasks;
using HaptiOS.Src.Config;
using HaptiOS.Src.DroneControl;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ninject;
using Ninject.Extensions.Factory;

namespace HaptiOS.Src
{
    public class Startup
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private IKernel Kernel { get; set; }
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime appLifetime)
        {
            Logger.Info("Configure()");

            Kernel = this.RegisterApplicationComponents(app);

            appLifetime.ApplicationStarted.Register(OnStarted);
            appLifetime.ApplicationStopping.Register(OnStopping);

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                appLifetime.StopApplication();
                // Don't terminate the process immediately, wait for the Main thread to exit gracefully.
                eventArgs.Cancel = true;
            };
        }

        private void OnStarted()
        {
            var config = Kernel.Get<IConfiguration>();
            HaptiosConfig.Instance.Load(config);

            var autoStartDrone = config.GetValue<bool>("flight.controller:autostart");
            Logger.Info("Autostart Drones: " + autoStartDrone);

            var droneManager = Kernel.Get<IDroneManager>();
            if (autoStartDrone)
            {
                Logger.Info("Starting DroneManager");
                droneManager.Start();
            }
            else Logger.Info("Drone autostart disabled");
        }


        private void OnStopping()
        {
            var droneStopTask = Task.Run(() =>
            {
                try
                {
                    //I added this back in - Marvin
                    var droneManager = Kernel.Get<IDroneManager>();
                    droneManager.Stop();
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Could not stop drone controller; cause: {0}", e.Message);
                }
            });
            Task[] tasks = { droneStopTask };
            Task.WaitAll(tasks);
        }

        private IKernel RegisterApplicationComponents(IApplicationBuilder app)
        {
            // IKernelConfiguration config = new KernelConfiguration();
            var kernel = new StandardKernel(new HaptiosModule());
            var funcModule = new FuncModule();
            if (!kernel.HasModule(funcModule.Name))
            {
                kernel.Load(funcModule);
            }
            /*
             * Use dependency injection provided by Ninject! With
             * asp.net core there are some ... issues.
             * https://github.com/ninject/Ninject/wiki/
             * https://dev.to/cwetanow/wiring-up-ninject-with-aspnet-core-20-3hp
             */

            // This is where our bindings are configurated
            // kernel.Bind<ITestService>().To<TestService>().InScope(RequestScope);
            // see modules on kernel creation

            kernel.Bind<IConfiguration>().ToMethod((context) => app.ApplicationServices.GetService<IConfiguration>()); ;

            Logger.Info("Kernel: " + kernel);
            return kernel;
        }


    }
}