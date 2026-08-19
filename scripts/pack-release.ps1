param(
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$solutionPath = Join-Path $projectRoot 'CharacterArchive.slnx'
$projectPath = Join-Path $projectRoot 'CharacterArchive\CharacterArchive.csproj'
$outputDirectory = Join-Path $projectRoot 'CharacterArchive\bin\Release'
$generatedManifest = Join-Path $outputDirectory 'CharacterArchive.json'
$assemblyPath = Join-Path $outputDirectory 'CharacterArchive.dll'

if (-not $NoBuild) {
    dotnet restore $solutionPath --locked-mode
    if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }
    dotnet build $solutionPath -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }
    dotnet run --project (Join-Path $projectRoot 'CharacterArchive.Tests\CharacterArchive.Tests.csproj') -c Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Core tests failed.' }
}

foreach ($requiredPath in @($projectPath, $assemblyPath, $generatedManifest, (Join-Path $projectRoot 'LICENSE'), (Join-Path $projectRoot 'THIRD_PARTY_NOTICES.md'))) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "Required release file not found: $requiredPath"
    }
}

$manifest = Get-Content -LiteralPath $generatedManifest -Raw -Encoding UTF8 | ConvertFrom-Json
$metadataPath = Join-Path $projectRoot 'distribution\CharacterArchive.metadata.json'
$metadata = Get-Content -LiteralPath $metadataPath -Raw -Encoding UTF8 | ConvertFrom-Json
$internalName = [string]$manifest.InternalName
$version = [string]$manifest.AssemblyVersion
if ($internalName -ne 'CharacterArchive' -or [string]::IsNullOrWhiteSpace($version)) {
    throw 'Generated manifest metadata is invalid.'
}
if ($manifest.Author -ne 'Roxyz0501' -or $metadata.Author -ne 'Roxyz0501' -or
    $metadata.InternalName -ne $internalName -or $metadata.AssemblyVersion -ne $version -or
    $metadata.ReleaseArtifact -ne "$internalName-$version.zip") {
    throw 'Manifest and shared-repository metadata are out of sync.'
}

$artifactsDirectory = Join-Path $projectRoot 'artifacts'
New-Item -ItemType Directory -Path $artifactsDirectory -Force | Out-Null
$zipPath = Join-Path $artifactsDirectory "$internalName-$version.zip"
if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

$tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$stagingDirectory = Join-Path $tempRoot ("CharacterArchive-package-" + [Guid]::NewGuid().ToString('N'))
$stagingDirectory = [System.IO.Path]::GetFullPath($stagingDirectory)
if (-not $stagingDirectory.StartsWith($tempRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Unsafe staging path: $stagingDirectory"
}

try {
    New-Item -ItemType Directory -Path $stagingDirectory | Out-Null
    Copy-Item -LiteralPath $assemblyPath -Destination (Join-Path $stagingDirectory 'CharacterArchive.dll')
    Copy-Item -LiteralPath $generatedManifest -Destination (Join-Path $stagingDirectory 'CharacterArchive.json')
    Copy-Item -LiteralPath (Join-Path $projectRoot 'LICENSE') -Destination (Join-Path $stagingDirectory 'LICENSE')
    Copy-Item -LiteralPath (Join-Path $projectRoot 'THIRD_PARTY_NOTICES.md') -Destination (Join-Path $stagingDirectory 'THIRD_PARTY_NOTICES.md')
    Compress-Archive -Path (Join-Path $stagingDirectory '*') -DestinationPath $zipPath -CompressionLevel Optimal
}
finally {
    if (Test-Path -LiteralPath $stagingDirectory) {
        Remove-Item -LiteralPath $stagingDirectory -Recurse -Force
    }
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $actualEntries = @($archive.Entries | ForEach-Object { $_.FullName } | Sort-Object)
    $expectedEntries = @('CharacterArchive.dll', 'CharacterArchive.json', 'LICENSE', 'THIRD_PARTY_NOTICES.md') | Sort-Object
    if (($actualEntries -join '|') -ne ($expectedEntries -join '|')) {
        throw "Unexpected ZIP contents: $($actualEntries -join ', ')"
    }
}
finally {
    $archive.Dispose()
}

Write-Output $zipPath
