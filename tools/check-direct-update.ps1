param([Parameter(Mandatory)][string]$Payload)
$ErrorActionPreference = 'Stop'
$Payload = [IO.Path]::GetFullPath($Payload)
$case = Join-Path $env:TEMP ('catscratch-check-' + [guid]::NewGuid().ToString('N'))
$target = Join-Path $case 'installed'
$stage = Join-Path (Join-Path $env:TEMP 'CatScratch-update') ('stage-' + [guid]::NewGuid().ToString('N'))
$source = Join-Path $stage 'payload'
New-Item -ItemType Directory -Path $target, $source -Force | Out-Null
$programFiles = Get-ChildItem -LiteralPath $Payload | Where-Object { $_.Name -notin @('userdata', 'captures') }
$programFiles | Copy-Item -Destination $target -Recurse
$programFiles | Copy-Item -Destination $source -Recurse
New-Item -ItemType Directory -Path "$target/userdata" -Force | Out-Null
[IO.File]::WriteAllText("$target/userdata/update-preserve.txt", 'keep-me')
[IO.File]::WriteAllText("$source/update-proof.txt", 'new-version')
$result = Join-Path $case 'shutdown.txt'
$archive = Join-Path $target 'userdata/temp/updates/CatScratch-win-x64.zip'
New-Item -ItemType Directory -Path (Split-Path $archive) -Force | Out-Null
Compress-Archive -Path "$source/*" -DestinationPath $archive
$start = New-Object Diagnostics.ProcessStartInfo
$start.FileName = "$target/CatScratch.exe"
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
foreach ($arg in @('--check-update-start', $archive, '--update-check', $result)) { $start.ArgumentList.Add($arg) }
$process = [Diagnostics.Process]::Start($start)
if (-not $process.WaitForExit(30000)) { Stop-Process -Id $process.Id; throw 'Direct update timed out' }
if ($process.ExitCode) { throw 'Direct update failed' }
$actualStage = [IO.File]::ReadAllText($result + '.stage')
$expectedRoot = [IO.Path]::GetFullPath("$target/userdata/temp/updates") + [IO.Path]::DirectorySeparatorChar
if (-not $actualStage.StartsWith($expectedRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Update staging escaped installed folder' }
$helperId = [int][IO.File]::ReadAllText($result + '.helperpid')
$helper = Get-Process -Id $helperId -ErrorAction SilentlyContinue
if ($helper -and -not $helper.WaitForExit(30000)) { Stop-Process -Id $helperId; throw 'Update helper timed out' }
if ($helper -and $helper.ExitCode) { throw 'Update helper failed' }
$cleanerId = [int][IO.File]::ReadAllText($result + '.cleanerpid')
$cleaner = Get-Process -Id $cleanerId -ErrorAction SilentlyContinue
if ($cleaner -and -not $cleaner.WaitForExit(30000)) { throw 'Update cleanup timed out' }
if ($cleaner -and $cleaner.ExitCode) { throw 'Update cleanup failed' }
if (Test-Path -LiteralPath $actualStage) { throw 'Old update staging and backup were retained after success' }
if (Test-Path -LiteralPath $archive) { throw 'Successful update download was retained' }
if ([IO.File]::ReadAllText("$target/userdata/update-preserve.txt") -ne 'keep-me') { throw 'Userdata changed' }
if ([IO.File]::ReadAllText("$target/update-proof.txt") -ne 'new-version') { throw 'New package not applied' }
if (-not (Test-Path "$target/.catscratch-files.json")) { throw 'Managed file manifest missing' }
if ((Get-FileHash "$target/CatScratch.exe").Hash -ne (Get-FileHash "$source/CatScratch.exe").Hash) { throw 'Updated executable differs' }
$shutdown = [IO.File]::ReadAllText($result)
if ($shutdown -notmatch 'Windows remaining: 0; pinned card retained: True') { throw 'Updated app did not close cleanly' }
# Remove only this run's generated diagnostic installations after every assertion passed.
foreach ($cleanup in @(@{Path=$case; Parent=$env:TEMP; Pattern='^catscratch-check-[0-9a-f]{32}$'},
                       @{Path=$stage; Parent=(Join-Path $env:TEMP 'CatScratch-update'); Pattern='^stage-[0-9a-f]{32}$'})) {
    $resolved = [IO.Path]::GetFullPath($cleanup.Path).TrimEnd([IO.Path]::DirectorySeparatorChar)
    $expectedParent = [IO.Path]::GetFullPath($cleanup.Parent).TrimEnd([IO.Path]::DirectorySeparatorChar)
    if ([IO.Path]::GetDirectoryName($resolved) -ne $expectedParent -or [IO.Path]::GetFileName($resolved) -notmatch $cleanup.Pattern) { throw 'Invalid diagnostic cleanup path' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
Write-Output "Direct update passed: old package and backups removed, userdata preserved, restarted app closed fully; diagnostic folders removed. $shutdown"
