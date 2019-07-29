This PowerShell Exporter lets you export Prometheus Gauge Metrics from the result of a PowerShell cmdlet.

**WARNING this is still a PoC; things will change for sure**


# Usage

The cmdlets are defined inside `metrics.psm1` and are loaded at the application startup, e.g.:

```powershell
# count the number tcp connections grouped by their remote address, port, and state.
# NB this must return PowerShellExporter.Metric objects.
function Get-TcpConnectionsMetrics {
    Get-NetTCPConnection `
        | Where-Object {$_.RemotePort -ne 0} `
        | Where-Object {$_.LocalAddress -ne '127.0.0.1' -and $_.LocalAddress -ne '::1'} `
        | Group-Object -Property 'RemoteAddress','RemotePort','State' `
        | ForEach-Object {
            [PowerShellExporter.Metric]::new(
                # metric value
                $_.Count,
                # metric labels
                @{
                    'remote_address' = $_.Group[0].RemoteAddress
                    'remote_port' = $_.Group[0].RemotePort
                    'state' = $_.Group[0].State
                })
        }
}
```

The metrics are defined inside `metrics.yml` and are evaluated at every prometheus scrape, e.g.:

```yml
metrics:
  # sample promql:
  #   sum(pse_tcp_connections) by (state)
  #   sum(pse_tcp_connections) by (remote_address)
  - name: pse_tcp_connections
    cmdlet: Get-TcpConnectionsMetrics
```

Prometheus can be configured with something alike:

```yml
global:
  scrape_interval:     15s # Set the scrape interval to every 15 seconds. Default is every 1 minute.
  evaluation_interval: 15s # Evaluate rules every 15 seconds. The default is every 1 minute.
  # scrape_timeout is set to the global default (10s).
...
scrape_configs:
  ...
  - job_name: pse
    static_configs:
      - targets:
        - localhost:9360
```

This exporter can be installed as a Windows service with something alike:

```powershell
.\PowerShellExporter help         # show help.
.\PowerShellExporter install      # install with default settings.
Start-Service PowerShellExporter  # start the service.
```

**NB** you can modify the default listening url `http://localhost:9360/metrics` with the `-url` command line argument, e.g., `-url http://localhost:9360/pse/metrics`.

**NB** you need to make sure this exporter metrics are not taking too long to complete by observing the scrape durations with the promql `scrape_duration_seconds{job="pse"}`.


# Build

Type `make` and use what ends-up in the `dist` sub-directory.

If the PowerShell `System.Management.Automation.dll` assembly is not found, you need to get its location with the `[PSObject].Assembly.Location` snippet and modify the `PowerShellExporter.csproj` `<HintPath>`.
