param(
  [string]$SiteName = "BC Fixed Asset",
  [string]$AppPoolName = "BCFixedAssetPool",
  [string]$PhysicalPath = "C:\inetpub\BCFixedAsset",
  [int]$Port = 8080
)
$ErrorActionPreference = "Stop"
Import-Module WebAdministration
if (-not (Test-Path "IIS:\AppPools\$AppPoolName")) { New-WebAppPool -Name $AppPoolName | Out-Null }
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name managedRuntimeVersion -Value "v4.0"
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name managedPipelineMode -Value "Integrated"
if (-not (Test-Path $PhysicalPath)) { New-Item -ItemType Directory -Path $PhysicalPath | Out-Null }
if (-not (Test-Path "IIS:\Sites\$SiteName")) { New-Website -Name $SiteName -Port $Port -PhysicalPath $PhysicalPath -ApplicationPool $AppPoolName | Out-Null }
else { Set-ItemProperty "IIS:\Sites\$SiteName" -Name physicalPath -Value $PhysicalPath; Set-ItemProperty "IIS:\Sites\$SiteName" -Name applicationPool -Value $AppPoolName }
Write-Host "IIS site configured. Publish the Release output to $PhysicalPath and configure HTTPS before production use."
