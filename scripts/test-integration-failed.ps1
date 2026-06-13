param(
    [string]$Project = "src/backend/ECommerce.Tests/ECommerce.Tests.csproj",
    [string]$RunSettings = "src/backend/ECommerce.Tests/.runsettings",
    [string]$ResultsDirectory = "src/backend/ECommerce.Tests/TestResults",
    [string]$TrxPath = "",
    [switch]$NoBuild = $true,
    [switch]$ListOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-LatestTrxPath {
    param([string]$Directory)

    if (-not (Test-Path $Directory)) {
        throw "Results directory not found: $Directory"
    }

    $latest = Get-ChildItem -Path $Directory -Recurse -Filter *.trx |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($null -eq $latest) {
        throw "No .trx file found under $Directory"
    }

    return $latest.FullName
}

function Get-FailedIntegrationTests {
    param([string]$Path)

    [xml]$trx = Get-Content -Path $Path -Raw

    $ns = New-Object System.Xml.XmlNamespaceManager($trx.NameTable)
    $ns.AddNamespace("t", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")

    $testDefinitionById = @{}
    $testDefinitions = $trx.SelectNodes("//t:TestDefinitions/t:UnitTest", $ns)
    foreach ($definition in $testDefinitions) {
        $testId = $definition.id
        $testMethod = $definition.SelectSingleNode("t:TestMethod", $ns)

        if ($null -ne $testMethod) {
            $className = $testMethod.className
            $methodName = $testMethod.name
            if (-not [string]::IsNullOrWhiteSpace($className) -and -not [string]::IsNullOrWhiteSpace($methodName)) {
                $testDefinitionById[$testId] = "$className.$methodName"
            }
        }
    }

    $failedResults = $trx.SelectNodes("//t:UnitTestResult[@outcome='Failed']", $ns)
    $failedIntegrationTests = New-Object System.Collections.Generic.HashSet[string]

    foreach ($result in $failedResults) {
        $testId = $result.testId
        $fullName = $null

        if ($testDefinitionById.ContainsKey($testId)) {
            $fullName = $testDefinitionById[$testId]
        }

        if ([string]::IsNullOrWhiteSpace($fullName)) {
            continue
        }

        if ($fullName.StartsWith("ECommerce.Tests.Integration.", [System.StringComparison]::Ordinal)) {
            [void]$failedIntegrationTests.Add($fullName)
        }
    }

    return @($failedIntegrationTests) | Sort-Object
}

if ([string]::IsNullOrWhiteSpace($TrxPath)) {
    $TrxPath = Get-LatestTrxPath -Directory $ResultsDirectory
}

if (-not (Test-Path $TrxPath)) {
    throw "TRX file not found: $TrxPath"
}

$failedTests = Get-FailedIntegrationTests -Path $TrxPath

if ($null -eq $failedTests) {
    $failedTests = @()
}
elseif ($failedTests -isnot [System.Array]) {
    $failedTests = @($failedTests)
}

if ($failedTests.Count -eq 0) {
    Write-Host "No failed integration tests found in: $TrxPath"
    exit 0
}

Write-Host "Found $($failedTests.Count) failed integration test(s) in: $TrxPath"
foreach ($test in $failedTests) {
    Write-Host " - $test"
}

if ($ListOnly) {
    exit 0
}

$filter = ($failedTests | ForEach-Object { "FullyQualifiedName=$_" }) -join "|"

$dotnetArgs = @(
    "test"
    $Project
    "--filter"
    $filter
    "--settings"
    $RunSettings
    "--results-directory"
    $ResultsDirectory
    "--logger"
    "trx;LogFileName=integration-rerun.trx"
    "--logger"
    "console;verbosity=minimal"
)

if ($NoBuild) {
    $dotnetArgs += "--no-build"
}

Write-Host ""
Write-Host "Running failed integration tests only..."
Write-Host "dotnet $($dotnetArgs -join ' ')"
Write-Host ""

& dotnet @dotnetArgs
$exitCode = $LASTEXITCODE

if ($exitCode -ne 0) {
    throw "Failed-tests rerun exited with code $exitCode"
}
