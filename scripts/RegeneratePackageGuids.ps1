# Regenerates primary asset GUIDs in com.jis.sdkads.* package .meta files.
# Run from repo root: powershell -File scripts/RegeneratePackageGuids.ps1

$root = Join-Path $PSScriptRoot "..\Packages"
$count = 0

Get-ChildItem -Path $root -Filter "*.meta" -Recurse |
    Where-Object { $_.FullName -match "com\.jis\.sdkads" } |
    ForEach-Object {
        $lines = Get-Content $_.FullName
        $changed = $false
        for ($i = 0; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -match "^guid: [a-f0-9]{32}$") {
                $lines[$i] = "guid: " + ([guid]::NewGuid().ToString("N"))
                $changed = $true
                break
            }
        }
        if ($changed) {
            Set-Content -Path $_.FullName -Value $lines -Encoding UTF8
            $count++
        }
    }

Write-Host "Updated $count .meta files under Packages/com.jis.sdkads.*"
