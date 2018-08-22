using Microsoft.Win32;
using Prometheus;
using Prometheus.Advanced;
using Serilog;
using System;
using System.IO;
using System.Reflection;
using Topshelf;

namespace PowerShellExporter
{
    class PowerShellExporterService
    {
        private MetricServer _metricServer;

        public PowerShellExporterService(Uri url, string metricsConfigPath, string metricsPowerShellModulePath)
        {
            var metricsConfig = MetricsConfig.Load(metricsConfigPath);

            DefaultCollectorRegistry.Instance.Clear();
            DefaultCollectorRegistry.Instance.RegisterOnDemandCollectors(new MetricsCollector(metricsConfig, metricsPowerShellModulePath));

            _metricServer = new MetricServer(
                hostname: url.Host,
                port: url.Port,
                url: url.AbsolutePath.Trim('/') + '/',
                useHttps: url.Scheme == "https");
        }

        public void Start()
        {
            _metricServer.Start();
        }

        public void Stop()
        {
            _metricServer.Stop();
        }
    }

    class Program
    {
        static int Main(string[] args)
        {
            ConfigureSerilog();

            var exitCode = HostFactory.Run(hc =>
            {
                hc.UseSerilog();

                var url = new Uri("http://localhost:9360/metrics"); // see https://github.com/prometheus/prometheus/wiki/Default-port-allocations
                var metricsConfigPath = @".\metrics.yml";

                hc.AddCommandLineDefinition("url", value => url = new Uri(value));
                hc.AddCommandLineDefinition("metrics", value => metricsConfigPath = value);

                hc.AfterInstall((ihs) =>
                {
                    // add service parameters.
                    using (var services = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services"))
                    using (var service = services.OpenSubKey(ihs.ServiceName, true))
                    {
                        service.SetValue(
                            "ImagePath",
                            string.Format(
                                "{0} -url \"{1}\" -metrics \"{2}\"",
                                service.GetValue("ImagePath"),
                                url,
                                Path.GetFullPath(metricsConfigPath)));
                    }
                });

                hc.Service<PowerShellExporterService>(sc =>
                {
                    sc.ConstructUsing(settings =>
                        {
                            Log.Information("{product} v{version}",
                                Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product,
                                Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>().Version);
                            Log.Information("Configuration url: {url}", url);
                            Log.Information("Configuration metrics: {metrics}", metricsConfigPath);

                            return new PowerShellExporterService(url, metricsConfigPath, metricsConfigPath.Replace(".yml", ".psm1"));
                        });
                    sc.WhenStarted(service => service.Start());
                    sc.WhenStopped(service => service.Stop());
                });

                hc.EnableServiceRecovery(rc =>
                {
                    rc.RestartService(1); // first failure: restart after 1 minute
                    rc.RestartService(1); // second failure: restart after 1 minute
                    rc.RestartService(1); // subsequent failures: restart after 1 minute
                });

                hc.StartAutomatically();

                hc.RunAsLocalSystem();

                hc.SetDescription(Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyDescriptionAttribute>().Description);
                hc.SetDisplayName(Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>().Title);
                hc.SetServiceName(Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product);
            });

            return (int)Convert.ChangeType(exitCode, exitCode.GetTypeCode());
        }

        private static void ConfigureSerilog()
        {
            if (!Environment.UserInteractive)
            {
                Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            }

            var configuration = new LoggerConfiguration();

            if (Environment.UserInteractive)
            {
                configuration = configuration
                    .MinimumLevel.Debug()
                    .WriteTo.Console();
            }
            else
            {
                configuration = configuration
                    .ReadFrom.AppSettings();
            }

            Log.Logger = configuration.CreateLogger();

            if (Environment.UserInteractive)
            {
                Log.Information("Running in UserInteractive mode");
            }
        }
    }
}
