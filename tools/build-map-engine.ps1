param([string]$Python='python')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output=Join-Path $repo 'src/AionMeter.App/obj/map-engine'
& $Python -m pip install --disable-pip-version-check -r (Join-Path $PSScriptRoot 'map-engine/requirements.txt')
if ($LASTEXITCODE) { throw 'Map engine build dependencies failed' }
& $Python (Join-Path $PSScriptRoot 'map-engine/check_presence.py') --data (Join-Path $repo 'data/map')
if ($LASTEXITCODE) { throw 'Map heading regression checks failed' }
& $Python (Join-Path $PSScriptRoot 'map-engine/check_capture.py')
if ($LASTEXITCODE) { throw 'Map capture scheduling checks failed' }
& $Python (Join-Path $PSScriptRoot 'map-engine/check_game_window.py')
if ($LASTEXITCODE) { throw 'Map game-window selection checks failed' }
& $Python -m PyInstaller --noconfirm --clean --onedir --console --name CatScratch.MapEngine --exclude-module tkinter --exclude-module matplotlib --exclude-module pandas --exclude-module scipy --exclude-module pytest --distpath "$output/dist" --workpath "$output/build" --specpath "$output" (Join-Path $PSScriptRoot 'map-engine/worker.py')
if ($LASTEXITCODE) { throw 'Map engine build failed' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'map-engine/licenses') -Destination "$output/dist/CatScratch.MapEngine/licenses" -Recurse -Force
$engine=Join-Path $output 'dist/CatScratch.MapEngine/CatScratch.MapEngine.exe'
& $engine --data (Join-Path $repo 'data/map') --verify
if ($LASTEXITCODE) { throw 'Bundled map engine could not start' }
