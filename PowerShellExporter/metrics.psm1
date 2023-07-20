function Get-TcpConnectionsMetrics {
    Get-NetTCPConnection `
        | Where-Object {$_.RemotePort -ne 0} `
        | Where-Object {$_.LocalAddress -ne '127.0.0.1' -and $_.LocalAddress -ne '::1'} `
        | Group-Object -Property 'RemoteAddress','RemotePort','State','LocalPort' `
        | ForEach-Object {
            [PowerShellExporter.Metric]::new($_.Count, @{
                'remote_address' = $_.Group[0].RemoteAddress
                'remote_port' = $_.Group[0].RemotePort
                'state' = $_.Group[0].State
				'local_port' = $_.Group[0].LocalPort
            })
        }
}
