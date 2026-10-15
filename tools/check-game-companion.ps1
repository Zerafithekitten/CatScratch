param([string]$Dotnet = 'dotnet')
$ErrorActionPreference = 'Stop'
$case = Join-Path $env:TEMP ('catscratch-game-check-' + [guid]::NewGuid().ToString('N'))
$game = $null
$watch = $null
try {
    New-Item -ItemType Directory -Path $case -Force | Out-Null
    Set-Content -LiteralPath "$case/Probe.csproj" -Value '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>WinExe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup></Project>'
    Set-Content -LiteralPath "$case/Program.cs" -Value 'if (args.Contains("--game-session")) System.IO.File.AppendAllText(System.IO.Path.Combine(AppContext.BaseDirectory,"starts.txt"),"started\n"); else System.Threading.Thread.Sleep(60000);'
    & $Dotnet build "$case/Probe.csproj" -c Release -o "$case/bin" --ignore-failed-sources
    if ($LASTEXITCODE) { throw 'Companion probe build failed' }
    Copy-Item -LiteralPath "$case/bin/Probe.exe" -Destination "$case/bin/GameProbe.exe"
    Copy-Item -LiteralPath "$case/bin/Probe.exe" -Destination "$case/bin/MeterProbe.exe"
    $source = Get-Content -LiteralPath "$PSScriptRoot/../src/AionMeter.App/Services/GameLifetime.cs" -Raw
    $match = [regex]::Match($source, '(?s)internal const string Watcher = """\s*\r?\n(.*?)\r?\n\s*""";')
    if (!$match.Success) { throw 'Watcher source not found' }
    $probe = @'
Add-Type -TypeDefinition 'using System; using System.Runtime.InteropServices; public static class ProbeConsole { [DllImport("kernel32.dll")] public static extern IntPtr GetConsoleWindow(); }'
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'watch-state.json'), (@{Pid=$PID; Console=[ProbeConsole]::GetConsoleWindow().ToInt64()} | ConvertTo-Json))
'@
    Set-Content -LiteralPath "$case/watch.ps1" -Value ($probe + "`n" + ($match.Groups[1].Value -replace '(?m)^ {8}','')) -Encoding utf8
    @{ Game = "$case/bin/GameProbe.exe"; Meter = "$case/bin/MeterProbe.exe" } | ConvertTo-Json | Set-Content -LiteralPath "$case/watch-quiet.json" -Encoding utf8
    $launcherOutput=[IO.Path]::GetFullPath("$case/bin/WatcherLauncher.exe")
    $launcherSource=[IO.Path]::GetFullPath("$PSScriptRoot/../src/AionMeter.App/Launcher/GameWatcherLauncher.cs")
    & "$env:SystemRoot/Microsoft.NET/Framework64/v4.0.30319/csc.exe" /nologo /target:winexe /optimize+ ("/out:"+$launcherOutput) $launcherSource
    if ($LASTEXITCODE) { throw 'Quiet launcher build failed' }
    $launcher = Start-Process -FilePath "$case/bin/WatcherLauncher.exe" -ArgumentList ("`"$case/watch.ps1`"") -WindowStyle Hidden -PassThru
    if (!$launcher.WaitForExit(10000)) { throw 'Quiet launcher did not exit promptly' }
    $launcher.Refresh()
    if ($launcher.ExitCode) { throw 'Quiet launcher failed' }
    $launcher.Dispose()
    $limit=[DateTime]::UtcNow.AddSeconds(30)
    while (!(Test-Path -LiteralPath "$case/watch-state.json") -and [DateTime]::UtcNow -lt $limit) { Start-Sleep -Milliseconds 100 }
    $state=Get-Content -LiteralPath "$case/watch-state.json" -Raw | ConvertFrom-Json
    if ($state.Console -ne 0) { throw 'Watcher allocated a console window' }
    $watch=Get-Process -Id $state.Pid
    $game = Start-Process -FilePath "$case/bin/GameProbe.exe" -WindowStyle Hidden -PassThru
    function Wait-Starts([int]$Expected) {
        $limit = [DateTime]::UtcNow.AddSeconds(30)
        do {
            if (Test-Path -LiteralPath "$case/bin/starts.txt") {
                $count = @(Get-Content -LiteralPath "$case/bin/starts.txt").Count
                if ($count -eq $Expected) { return }
                if ($count -gt $Expected) { throw 'Watcher restarted a manually closed meter during the same game session' }
            }
            Start-Sleep -Milliseconds 250
        } while ([DateTime]::UtcNow -lt $limit)
        Write-Output "Game exited: $($game.HasExited); watcher exited: $($watch.HasExited)"
        Get-Process -Name GameProbe,MeterProbe,CatScratch -ErrorAction SilentlyContinue | Select-Object ProcessName,SessionId,Path | Format-Table | Out-String | Write-Output
        if (Test-Path -LiteralPath "$case/watch-error.txt") { Get-Content -LiteralPath "$case/watch-error.txt" | Write-Output }
        throw "Watcher did not launch for game session $Expected"
    }
    Wait-Starts 1
    $again=Start-Process -FilePath "$case/bin/WatcherLauncher.exe" -ArgumentList ("`"$case/watch.ps1`"") -WindowStyle Hidden -PassThru
    if (!$again.WaitForExit(10000)) { throw 'Repeated quiet launcher did not finish' }
    $again.Dispose()
    Start-Sleep -Seconds 3
    if (@(Get-Content -LiteralPath "$case/bin/starts.txt").Count -ne 1) { throw 'Watcher repeated a launch during the same session' }
    $repeat=Get-Content -LiteralPath "$case/watch-state.json" -Raw | ConvertFrom-Json
    if ($repeat.Pid -eq $watch.Id) { throw 'Repeated launch did not reach the singleton check' }
    $duplicate=Get-Process -Id $repeat.Pid -ErrorAction SilentlyContinue
    if ($duplicate) {
        try { if(!$duplicate.WaitForExit(5000)) { throw 'Duplicate watcher remained running' } }
        finally { $duplicate.Dispose() }
    }
    $game.Kill(); $game.WaitForExit(); $game.Dispose(); $game = $null
    Start-Sleep -Seconds 3
    $game = Start-Process -FilePath "$case/bin/GameProbe.exe" -WindowStyle Hidden -PassThru
    Wait-Starts 2
    Remove-Item -LiteralPath "$case/watch-quiet.json"
    if (!$watch.WaitForExit(6000)) { throw 'Disabled watcher did not stop' }
    Write-Output 'Game companion passed: no console allocated, exact executable detection, one launch per session, a new game session launches again, disabling stops the watcher. No startup task was installed.'
} finally {
    foreach ($process in @($game, $watch)) {
        if ($null -ne $process) {
            if (!$process.HasExited) { $process.Kill(); $process.WaitForExit() }
            $process.Dispose()
        }
    }
    $resolved = [IO.Path]::GetFullPath($case).TrimEnd('\')
    $parent = [IO.Path]::GetFullPath($env:TEMP).TrimEnd('\')
    if ([IO.Path]::GetDirectoryName($resolved) -ne $parent -or [IO.Path]::GetFileName($resolved) -notmatch '^catscratch-game-check-[0-9a-f]{32}$') { throw 'Invalid companion test cleanup path' }
    if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}
