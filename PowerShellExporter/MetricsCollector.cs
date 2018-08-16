using Prometheus;
using Prometheus.Advanced;
using Serilog;
using System;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace PowerShellExporter
{
    class MetricsCollector : IOnDemandCollector, IDisposable
    {
        private readonly MetricsConfig _config;
        private readonly string _metricsPowerShellModulePath;
        private ICollectorRegistry _registry;
        private Runspace _rs;

        public MetricsCollector(MetricsConfig config, string metricsPowerShellModulePath)
        {
            _config = config;
            _metricsPowerShellModulePath = metricsPowerShellModulePath;
        }

        public void Dispose()
        {
            if (_rs != null)
            {
                _rs.Dispose();
            }
        }

        public void RegisterMetrics(ICollectorRegistry registry)
        {
            _registry = registry;

            var iss = InitialSessionState.CreateDefault();
            iss.ImportPSModule(new[] { _metricsPowerShellModulePath });
            iss.Types.Add(new SessionStateTypeEntry(new TypeData(typeof(Metric)), false));

            _rs = RunspaceFactory.CreateRunspace(iss);

            // NB if this errors out with something alike:
            //      runspace cannot be loaded because running scripts is disabled on this system. For more information, see about_Execution_Policies
            //    you need to change the PowerShell (64-bit and 32-bit) execution policies with:
            //      PowerShell.exe -Command Set-ExecutionPolicy Unrestricted
            //      c:\windows\syswow64\WindowsPowerShell\v1.0\PowerShell.exe -Command Set-ExecutionPolicy Unrestricted
            _rs.Open();

            Log.Information("PowerShell v{PowerShellVersion}", _rs.Version);
        }

        public void UpdateMetrics()
        {
            foreach (var m in _config.Metrics)
            {
                Log.Debug("Executing {Cmdlet}", m.Cmdlet);

                try
                {
                    using (var ps = PowerShell.Create())
                    {
                        ps.Runspace = _rs;
                        ps.AddCommand(m.Cmdlet);

                        var results = ps.Invoke<Metric>();

                        if (ps.HadErrors)
                        {
                            foreach (var e in ps.Streams.Error)
                            {
                                Log.Error("{Cmdlet} error {Error}", m.Cmdlet, e);
                            }
                        }
                        else
                        {
                            foreach (var r in results)
                            {
                                var labels = r.Labels.Keys.Cast<string>().ToArray();
                                var labelValues = r.Labels.Values.Cast<object>().Select(v => v.ToString()).ToArray();

                                var gauge = Metrics.WithCustomRegistry(_registry).CreateGauge(m.Name, m.Help, labels);
                                gauge.Labels(labelValues).Set(r.Value);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, "Unhandled exception invoking {Cmdlet}", m.Cmdlet);
                }
            }
        }
    }
}
