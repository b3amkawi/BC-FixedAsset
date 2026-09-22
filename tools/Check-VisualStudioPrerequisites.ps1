[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'

if (-not (Test-Path $vswhere)) {
    throw 'Visual Studio Installer was not found. Install Visual Studio 2026 Community, Professional or Enterprise.'
}

$installPath = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Workload.NetWeb Microsoft.Net.Component.4.8.TargetingPack -property installationPath
if ([string]::IsNullOrWhiteSpace($installPath)) {
    throw "Required components are missing. In Visual Studio Installer, add 'ASP.NET and web development' and the '.NET Framework 4.8 targeting pack'."
}

$webTargets = Get-ChildItem -Path (Join-Path $installPath 'MSBuild\Microsoft\VisualStudio') -Filter Microsoft.WebApplication.targets -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $webTargets) {
    throw "Microsoft.WebApplication.targets was not found under $installPath. Repair the ASP.NET and web development workload."
}

Write-Host "Visual Studio prerequisites are ready: $installPath" -ForegroundColor Green
Write-Host "Web Application targets: $($webTargets.FullName)" -ForegroundColor Green
