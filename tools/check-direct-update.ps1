param([Parameter(Mandatory)][string]$Payload)
$ErrorActionPreference = 'Stop'
$Payload = [IO.Path]::GetFullPath($Payload)
$case = Join-Path $env:TEMP ('catscratch-check-' + [guid]::NewGuid().ToString('N'))
$target = Join-Path $case 'installed'
$stage = Join-Path (Join-Path $env:TEMP 'CatScratch-update') ('stage-' + [guid]::NewGuid().ToString('N'))
$source = Join-Path $stage 'payload'
New-Item -ItemType Directory -Path $target, $source -Force | Out-Null
Copy-Item "$Payload/*" $target -Recurse
Copy-Item "$Payload/*" $source -Recurse
New-Item -ItemType Directory -Path "$target/userdata" | Out-Null
[IO.File]::WriteAllText("$target/userdata/update-preserve.txt", 'keep-me')
[IO.File]::WriteAllText("$source/update-proof.txt", 'new-version')
$result = Join-Path $case 'shutdown.txt'
$archive = Join-Path $case 'update.zip'
Compress-Archive -Path "$source/*" -DestinationPath $archive
$start = New-Object Diagnostics.ProcessStartInfo
$start.FileName = "$target/CatScratch.exe"
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
foreach ($arg in @('--check-update-start', $archive, '--update-check', $result)) { $start.ArgumentList.Add($arg) }
$process = [Diagnostics.Process]::Start($start)
if (-not $process.WaitForExit(30000)) { Stop-Process -Id $process.Id; throw 'Direct update timed out' }
if ($process.ExitCode) { throw 'Direct update failed' }
$helperId = [int][IO.File]::ReadAllText($result + '.helperpid')
$helper = Get-Process -Id $helperId -ErrorAction SilentlyContinue
if ($helper -and -not $helper.WaitForExit(30000)) { Stop-Process -Id $helperId; throw 'Update helper timed out' }
if ($helper -and $helper.ExitCode) { throw 'Update helper failed' }
if ([IO.File]::ReadAllText("$target/userdata/update-preserve.txt") -ne 'keep-me') { throw 'Userdata changed' }
if ([IO.File]::ReadAllText("$target/update-proof.txt") -ne 'new-version') { throw 'New package not applied' }
if (-not (Test-Path "$target/.catscratch-files.json")) { throw 'Managed file manifest missing' }
if ((Get-FileHash "$target/CatScratch.exe").Hash -ne (Get-FileHash "$source/CatScratch.exe").Hash) { throw 'Updated executable differs' }
$shutdown = [IO.File]::ReadAllText($result)
if ($shutdown -notmatch 'Windows remaining: 0; pinned card retained: True') { throw 'Updated app did not close cleanly' }
Write-Output "Direct update passed: files replaced, userdata preserved, restarted app closed fully. $shutdown"
