[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [ValidateSet('Link', 'Copy')]
    [string]$Mode = 'Link',
    [string]$SkillsRoot = (Join-Path $HOME '.agents/skills')
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$source = (Resolve-Path -LiteralPath (Join-Path $repoRoot 'skills/unity-editor-design')).Path
$destination = Join-Path $SkillsRoot 'unity-editor-design'

if (Test-Path -LiteralPath $destination) {
    $existing = Get-Item -LiteralPath $destination -Force
    if ($Mode -eq 'Link' -and $existing.LinkType -in @('Junction', 'SymbolicLink')) {
        $target = @($existing.Target)[0]
        if ($target -and [IO.Path]::GetFullPath($target).TrimEnd('\', '/') -eq [IO.Path]::GetFullPath($source).TrimEnd('\', '/')) {
            Write-Output "Already installed: $destination -> $source"
            return
        }
    }
    throw "Destination already exists. Inspect and back it up before replacing it: $destination"
}

if ($PSCmdlet.ShouldProcess($destination, "Install user skill using $Mode")) {
    New-Item -ItemType Directory -Path $SkillsRoot -Force | Out-Null
    if ($Mode -eq 'Copy') {
        Copy-Item -LiteralPath $source -Destination $destination -Recurse
    }
    elseif ($env:OS -eq 'Windows_NT') {
        New-Item -ItemType Junction -Path $destination -Target $source | Out-Null
    }
    else {
        New-Item -ItemType SymbolicLink -Path $destination -Target $source | Out-Null
    }
    if (-not (Test-Path -LiteralPath (Join-Path $destination 'SKILL.md'))) {
        throw "Installation did not produce a readable SKILL.md: $destination"
    }
    Write-Output "Installed: $destination"
    if ($Mode -eq 'Link') {
        Write-Output "Source: $source"
        Write-Output 'Updating this repository also updates the installed skill.'
    }
    Write-Output 'Start a new agent session to refresh skill discovery.'
}
