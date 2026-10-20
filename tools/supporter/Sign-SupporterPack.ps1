param(
 [Parameter(Mandatory=$true)][string]$SourceDirectory,
 [Parameter(Mandatory=$true)][string]$KeyPath,
 [Parameter(Mandatory=$true)][string]$OutputPath
)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.IO.Compression
$root=[IO.Path]::GetFullPath($SourceDirectory).TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar
$manifestPath=Join-Path $root 'manifest.json'
$manifest=[IO.File]::ReadAllBytes($manifestPath)
if($manifest.Length -gt 32000){throw 'Manifest too large'}
$data=[Text.Encoding]::UTF8.GetString($manifest)|ConvertFrom-Json
if($data.Format -ne 1){throw 'Unsupported package format'}
$key=[Security.Cryptography.ECDsa]::Create()
try{
 $key.ImportFromPem([IO.File]::ReadAllText([IO.Path]::GetFullPath($KeyPath)))
 $signature=$key.SignData($manifest,[Security.Cryptography.HashAlgorithmName]::SHA256)
}finally{$key.Dispose()}
$files=@{}
foreach($sound in $data.Sounds){
 if($sound.Path -cnotmatch '^sounds/[a-z0-9-]{1,48}\.wav$'){throw 'Invalid audio path'}
 $path=[IO.Path]::GetFullPath((Join-Path $root $sound.Path))
 if(-not $path.StartsWith($root,[StringComparison]::OrdinalIgnoreCase)){throw 'Audio path escapes package directory'}
 $bytes=[IO.File]::ReadAllBytes($path)
 if($bytes.Length -gt 4000000 -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $sound.Sha256){throw 'Audio hash or size mismatch'}
 $files[$sound.Path]=$bytes
}
if($null -ne $data.Mascot){
 $mascot=$data.Mascot
 if($mascot.Path -cnotmatch '^mascots/[a-z0-9-]{1,48}\.png$'){throw 'Invalid mascot path'}
 $path=[IO.Path]::GetFullPath((Join-Path $root $mascot.Path))
 if(-not $path.StartsWith($root,[StringComparison]::OrdinalIgnoreCase)){throw 'Mascot path escapes package directory'}
 $bytes=[IO.File]::ReadAllBytes($path)
 if($bytes.Length -gt 4000000 -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $mascot.Sha256){throw 'Mascot hash or size mismatch'}
 $files[$mascot.Path]=$bytes
}
$out=[IO.Path]::GetFullPath($OutputPath)
if(Test-Path -LiteralPath $out){throw 'Output already exists; choose a new version or filename'}
$stream=[IO.File]::Create($out)
try{
 $zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create,$true)
 try{
  $files['manifest.json']=$manifest;$files['manifest.sig']=$signature
  foreach($name in $files.Keys){$entry=$zip.CreateEntry($name,[IO.Compression.CompressionLevel]::Optimal);$write=$entry.Open();try{$write.Write($files[$name],0,$files[$name].Length)}finally{$write.Dispose()}}
 }finally{$zip.Dispose()}
}finally{$stream.Dispose()}
Write-Output "Created $out"
