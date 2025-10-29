#!/usr/bin/env pwsh

[CmdletBinding()]
param(
  [string]$ReleaseTag,
  [string]$Repository,
  [int]$PrNumber = 0,
  [switch]$KeepDownloadedBinary
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
  throw 'setup.ps1 currently supports Windows hosts only.'
}

if (-not $PSBoundParameters.ContainsKey('ReleaseTag') -and [string]::IsNullOrWhiteSpace($ReleaseTag)) {
  $ReleaseTag = $env:ONBOARD_RELEASE_TAG
}

$script:PreserveDownloadedBinary = $KeepDownloadedBinary.IsPresent
if (-not $script:PreserveDownloadedBinary -and -not [string]::IsNullOrWhiteSpace($env:ONBOARD_KEEP_DOWNLOADED_BINARY)) {
  if ($env:ONBOARD_KEEP_DOWNLOADED_BINARY -match '^(?i:1|true|yes|on)$') {
    $script:PreserveDownloadedBinary = $true
  }
}

function Set-ExecutionPolicyBypass {
  try {
    $currentPolicy = Get-ExecutionPolicy -Scope Process
  } catch {
    return
  }

  if ($currentPolicy -in @('Bypass', 'Unrestricted')) {
    return
  }

  try {
    Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force | Out-Null
  } catch {
    Write-Warning 'Unable to relax the execution policy automatically. Re-run with "powershell -ExecutionPolicy Bypass -File .\setup.ps1" if you encounter policy errors.'
  }
}

Set-ExecutionPolicyBypass

$assetName = 'Onboard-win-x64.exe'
$script:DownloadPath = $null

function Resolve-RepositorySlug {
  param([string]$Explicit)

  $candidate = $Explicit
  if ([string]::IsNullOrWhiteSpace($candidate)) {
    $candidate = $env:ONBOARD_REPOSITORY
  }
  if ([string]::IsNullOrWhiteSpace($candidate)) {
    $candidate = $env:GITHUB_REPOSITORY
  }
  if ([string]::IsNullOrWhiteSpace($candidate)) {
    $candidate = 'kevinaud/onboard-v2'
    Write-Warning "Defaulting repository slug to '$candidate'. Override with -Repository if needed."
  }

  if ($candidate -notmatch '^[^/]+/[^/]+$') {
    throw "Repository slug '$candidate' is invalid. Expected format 'owner/repo'."
  }

  return $candidate
}

function New-HttpHeaders {
  $headers = @{ 'User-Agent' = 'OnboardSetupScript' }
  if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_TOKEN)) {
    $headers['Authorization'] = "Bearer $($env:GITHUB_TOKEN)"
  }
  return $headers
}

function Get-ReleaseMetadata {
  param(
    [string]$RepoSlug,
    [string]$Tag
  )

  $baseUri = "https://api.github.com/repos/$RepoSlug/releases"
  $endpoint = if ([string]::IsNullOrWhiteSpace($Tag)) {
    "$baseUri/latest"
  } else {
    "$baseUri/tags/$Tag"
  }

  Write-Verbose "Fetching release metadata from $endpoint"
  $release = Invoke-RestMethod -Uri $endpoint -Headers (New-HttpHeaders) -ErrorAction Stop
  return $release
}

function Get-AssetDownloadUrl {
  param(
    [object]$Release,
    [string]$Name
  )

  if (-not $Release) {
    throw 'Release metadata was not provided.'
  }

  $asset = $Release.assets | Where-Object { $_.name -eq $Name } | Select-Object -First 1
  if (-not $asset) {
    $available = ($Release.assets | Select-Object -ExpandProperty name)
    if ($available) {
      throw "Asset '$Name' not found in release. Available assets: $($available -join ', ')."
    }

    throw "Asset '$Name' not found in release."
  }

  return [string]$asset.browser_download_url
}

function Get-PRArtifactUrl {
  param(
    [string]$RepoSlug,
    [int]$PRNumber
  )

  Write-Verbose "Fetching PR #$PRNumber workflow runs..."

  # Get the latest workflow run for this PR
  $runsUri = "https://api.github.com/repos/$RepoSlug/actions/runs?event=pull_request&status=completed"
  $runs = Invoke-RestMethod -Uri $runsUri -Headers (New-HttpHeaders)

  $prRun = $runs.workflow_runs | Where-Object {
    $_.pull_requests -and
    ($_.pull_requests | Where-Object { $_.number -eq $PRNumber })
  } | Select-Object -First 1

  if (-not $prRun) {
    throw "No completed workflow run found for PR #$PRNumber. Has CI completed?"
  }

  Write-Verbose "Found workflow run: $($prRun.html_url)"

  # Get artifacts for this run
  $artifactsUri = $prRun.artifacts_url
  $artifacts = Invoke-RestMethod -Uri $artifactsUri -Headers (New-HttpHeaders)

  $artifact = $artifacts.artifacts | Where-Object {
    $_.name -eq "onboard-pr-$PRNumber-win-x64"
  } | Select-Object -First 1

  if (-not $artifact) {
    $available = ($artifacts.artifacts | Select-Object -ExpandProperty name) -join ', '
    throw "PR test artifact 'onboard-pr-$PRNumber-win-x64' not found. Available artifacts: $available"
  }

  return $artifact.archive_download_url
}

function Get-DownloadPath {
  $base = [System.IO.Path]::GetTempFileName()
  $target = [System.IO.Path]::ChangeExtension($base, '.exe')
  try {
    if (Test-Path -Path $base) {
      Remove-Item -Path $base -Force
    }
  } catch {
    # best effort cleanup
  }

  return $target
}

function Get-AssetBinary {
  param(
    [string]$Url,
    [string]$Destination
  )

  Write-Verbose "Downloading $Url to $Destination"
  Invoke-WebRequest -Uri $Url -OutFile $Destination -Headers (New-HttpHeaders) -UseBasicParsing
}

function Get-PRArtifactBinary {
  param(
    [string]$ArtifactUrl,
    [string]$Destination
  )

  Write-Verbose "Downloading PR artifact to $Destination"

  # Download the artifact zip
  $zipPath = [System.IO.Path]::ChangeExtension($Destination, '.zip')
  Invoke-WebRequest -Uri $ArtifactUrl -OutFile $zipPath -Headers (New-HttpHeaders) -UseBasicParsing

  # Extract the binary
  $extractDir = [System.IO.Path]::GetDirectoryName($Destination)
  Expand-Archive -Path $zipPath -DestinationPath $extractDir -Force

  # Find the extracted binary
  $extractedBinary = Get-ChildItem -Path $extractDir -Filter "Onboard-win-x64.exe" -Recurse | Select-Object -First 1
  if (-not $extractedBinary) {
    throw "Could not find Onboard-win-x64.exe in extracted artifact"
  }

  Move-Item -Path $extractedBinary.FullName -Destination $Destination -Force
  Remove-Item -Path $zipPath -Force
}

function Invoke-Binary {
  param(
    [string]$Path,
    [object[]]$Arguments
  )

  Write-Host "Launching $Path" -ForegroundColor Cyan
  & $Path @Arguments
  $exitCode = $LASTEXITCODE
  if ($exitCode -ne 0) {
    throw "Onboard binary exited with code $exitCode."
  }
}

try {
  # Ensure TLS 1.2+ for GitHub API calls
  [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor [System.Net.SecurityProtocolType]::Tls12

  $repoSlug = Resolve-RepositorySlug -Explicit $Repository
  
  $script:DownloadPath = Get-DownloadPath
  
  if ($PrNumber -gt 0) {
    # PR testing mode - download from GitHub Actions artifacts
    Write-Host "Testing PR #$PrNumber" -ForegroundColor Yellow
    $artifactUrl = Get-PRArtifactUrl -RepoSlug $repoSlug -PRNumber $PrNumber
    Get-PRArtifactBinary -ArtifactUrl $artifactUrl -Destination $script:DownloadPath
  } else {
    # Normal release mode
    $release = Get-ReleaseMetadata -RepoSlug $repoSlug -Tag $ReleaseTag
    $downloadUrl = Get-AssetDownloadUrl -Release $release -Name $assetName
    Get-AssetBinary -Url $downloadUrl -Destination $script:DownloadPath
  }

  Invoke-Binary -Path $script:DownloadPath -Arguments $args
} finally {
  if (-not $script:PreserveDownloadedBinary -and $script:DownloadPath -and (Test-Path -Path $script:DownloadPath)) {
    try {
      Remove-Item -Path $script:DownloadPath -Force
    } catch {
      Write-Warning "Failed to delete temporary file $script:DownloadPath: $($_.Exception.Message)"
    }
  } elseif ($script:PreserveDownloadedBinary -and $script:DownloadPath) {
    Write-Host "Binary preserved at $script:DownloadPath" -ForegroundColor Yellow
  }
}
