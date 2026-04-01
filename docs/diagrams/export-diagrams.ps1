param(
    [ValidateSet("svg", "png")]
    [string]$Format = "svg"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$sourceDir = Join-Path $repoRoot "docs\diagrams\src"
$outputDir = Join-Path $repoRoot "docs\diagrams\out"
$plantUmlJar = Join-Path $repoRoot "docs\tools\plantuml.jar"

if (-not (Test-Path $sourceDir)) {
    throw "Diagram source directory not found: $sourceDir"
}

New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
New-Item -ItemType Directory -Force -Path (Split-Path $plantUmlJar) | Out-Null

if (-not (Test-Path $plantUmlJar)) {
    Write-Host "Downloading PlantUML..."
    Invoke-WebRequest -Uri "https://github.com/plantuml/plantuml/releases/latest/download/plantuml.jar" -OutFile $plantUmlJar
}

$diagramFiles = @(Get-ChildItem -LiteralPath $sourceDir -File |
    Where-Object { $_.Extension -in @('.puml', '.pu', '.plantuml', '.wsd') })

if ($diagramFiles.Count -eq 0) {
    throw "No diagram files found in $sourceDir"
}

$formatArg = "-t$Format"

foreach ($diagramFile in $diagramFiles) {
    Write-Host "Rendering $($diagramFile.Name) to $Format..."
    & java -jar $plantUmlJar $formatArg -o $outputDir $diagramFile.FullName
    if ($LASTEXITCODE -ne 0) {
        throw "PlantUML export failed for $($diagramFile.FullName)"
    }
}

Write-Host "Done. Output written to $outputDir"