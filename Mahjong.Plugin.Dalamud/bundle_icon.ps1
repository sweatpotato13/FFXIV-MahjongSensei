# Patch the DalamudPackager-built latest.zip with the icon and license notices.
# Invoked from the csproj BundleIcon target.
param(
  [Parameter(Mandatory=$true)][string]$ZipPath,
  [Parameter(Mandatory=$true)][string]$IconPath
)

$ErrorActionPreference = 'Stop'
$repoDir = Split-Path -Parent $PSScriptRoot
$files = [ordered]@{
  'Images/Icon.png' = $IconPath
  'LICENSE.md' = Join-Path $repoDir 'LICENSE.md'
  'NOTICE' = Join-Path $repoDir 'NOTICE'
}

if (-not (Test-Path $ZipPath)) {
  Write-Error "zip not found: $ZipPath"
  exit 1
}
foreach ($path in $files.Values) {
  if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
    throw "Required package file not found: $path"
  }
}

Add-Type -AssemblyName System.IO.Compression.FileSystem

$zip = [IO.Compression.ZipFile]::Open($ZipPath, 'Update')
try {
  foreach ($entry in $files.GetEnumerator()) {
    # Normalize old Windows separators and casing; re-runs must not duplicate entries.
    $existing = @($zip.Entries | Where-Object { $_.FullName.Replace('\', '/') -ieq $entry.Key })
    foreach ($e in $existing) { $e.Delete() }
    [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $entry.Value, $entry.Key)
  }
  Write-Host "Bundled icon, LICENSE.md and NOTICE into $ZipPath"
} finally {
  $zip.Dispose()
}
