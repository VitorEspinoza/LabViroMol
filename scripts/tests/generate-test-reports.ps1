[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "artifacts/test-reports",
    [switch]$SkipRestore,
    [switch]$SkipBuild,
    [switch]$KeepExistingOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-RepoRoot {
    $scriptRoot = Split-Path -Parent $PSCommandPath
    return [System.IO.Path]::GetFullPath((Join-Path $scriptRoot "..\.."))
}

function Get-RelativePath {
    param(
        [Parameter(Mandatory = $true)][string]$BasePath,
        [Parameter(Mandatory = $true)][string]$TargetPath
    )

    $baseUri = [System.Uri]((Resolve-Path $BasePath).Path.TrimEnd('\') + '\')
    $targetUri = [System.Uri](Resolve-Path $TargetPath).Path
    return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString()).Replace('/', '/')
}

function ConvertTo-SafeName {
    param([Parameter(Mandatory = $true)][string]$Value)

    return ($Value -replace '[^A-Za-z0-9\.-]+', '-').Trim('-')
}

function Format-Duration {
    param([TimeSpan]$Duration)

    if ($Duration.TotalSeconds -lt 1) {
        return "{0:N0} ms" -f $Duration.TotalMilliseconds
    }

    return "{0:hh\:mm\:ss\.fff}" -f $Duration
}

function Get-ResultTotals {
    param([Parameter(Mandatory = $true)][array]$Results)

    $totalTests = 0
    $executedTests = 0
    $passedTests = 0
    $failedTests = 0
    $skippedTests = 0
    $durationMilliseconds = 0.0

    foreach ($result in $Results) {
        $totalTests += $result.Summary.Total
        $executedTests += $result.Summary.Executed
        $passedTests += $result.Summary.Passed
        $failedTests += $result.Summary.Failed
        $skippedTests += $result.Summary.Skipped
        $durationMilliseconds += $result.Summary.Duration.TotalMilliseconds
    }

    return [PSCustomObject]@{
        Total = $totalTests
        Executed = $executedTests
        Passed = $passedTests
        Failed = $failedTests
        Skipped = $skippedTests
        Duration = [TimeSpan]::FromMilliseconds($durationMilliseconds)
    }
}

function Get-TestProjects {
    param([Parameter(Mandatory = $true)][string]$Root)

    return Get-ChildItem -Path $Root -Filter *.csproj -Recurse |
        Sort-Object FullName
}

function Get-TrxSummary {
    param([Parameter(Mandatory = $true)][string]$TrxPath)

    [xml]$trx = Get-Content -Raw $TrxPath

    $countersNode = Select-Xml -Xml $trx -XPath "//*[local-name()='ResultSummary']/*[local-name()='Counters']" |
        Select-Object -ExpandProperty Node -First 1
    $timesNode = Select-Xml -Xml $trx -XPath "//*[local-name()='Times']" |
        Select-Object -ExpandProperty Node -First 1
    $runInfoNode = Select-Xml -Xml $trx -XPath "//*[local-name()='TestRun']" |
        Select-Object -ExpandProperty Node -First 1
    $resultNodes = @(Select-Xml -Xml $trx -XPath "//*[local-name()='Results']/*[local-name()='UnitTestResult']" |
        Select-Object -ExpandProperty Node)

    $duration = [TimeSpan]::Zero
    if ($timesNode -and $timesNode.start -and $timesNode.finish) {
        $start = [datetimeoffset]::Parse($timesNode.start)
        $finish = [datetimeoffset]::Parse($timesNode.finish)
        $duration = $finish - $start
    }

    $failedTests = foreach ($node in $resultNodes | Where-Object { $_.outcome -eq "Failed" }) {
        $messageNode = Select-Xml -Xml $node.OwnerDocument -XPath "//*[local-name()='UnitTestResult' and @testId='$($node.testId)']/*[local-name()='Output']/*[local-name()='ErrorInfo']/*[local-name()='Message']" |
            Select-Object -ExpandProperty Node -First 1

        [PSCustomObject]@{
            Name = $node.testName
            Duration = if ($node.duration) { [TimeSpan]::Parse($node.duration) } else { [TimeSpan]::Zero }
            Message = if ($messageNode) { ($messageNode.InnerText -replace '\s+', ' ').Trim() } else { "" }
        }
    }

    return [PSCustomObject]@{
        Name = if ($runInfoNode.name) { $runInfoNode.name } else { [System.IO.Path]::GetFileNameWithoutExtension($TrxPath) }
        Total = [int]$countersNode.total
        Executed = [int]$countersNode.executed
        Passed = [int]$countersNode.passed
        Failed = [int]$countersNode.failed
        Skipped = [int]$countersNode.notExecuted
        Duration = [TimeSpan]$duration
        FailedTests = $failedTests
    }
}

function Invoke-TestProject {
    param(
        [Parameter(Mandatory = $true)][string]$ProjectPath,
        [Parameter(Mandatory = $true)][string]$Category,
        [Parameter(Mandatory = $true)][string]$ResultsRoot,
        [string]$Configuration = "Release",
        [string]$CoverageSettingsPath,
        [switch]$CollectCoverage,
        [switch]$NoBuild
    )

    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($ProjectPath)
    $resultDir = Join-Path $ResultsRoot (ConvertTo-SafeName $projectName)
    New-Item -ItemType Directory -Force -Path $resultDir | Out-Null

    $arguments = @(
        "test",
        $ProjectPath,
        "-c", $Configuration,
        "--logger", "trx;LogFileName=results.trx",
        "--results-directory", $resultDir
    )

    if ($NoBuild) {
        $arguments += "--no-build"
    }

    if ($CollectCoverage) {
        $arguments += @("--collect", "XPlat Code Coverage", "--settings", $CoverageSettingsPath)
    }

    Write-Host "Running $Category tests: $projectName"
    & dotnet @arguments | Out-Host
    $exitCode = $LASTEXITCODE

    $trxPath = Join-Path $resultDir "results.trx"
    if (-not (Test-Path $trxPath)) {
        throw "TRX file was not produced for $ProjectPath"
    }

    $summary = Get-TrxSummary -TrxPath $trxPath
    $coverageCandidates = @(Get-ChildItem -Path $resultDir -Filter coverage.cobertura.xml -Recurse -File -ErrorAction SilentlyContinue)
    $coverageFiles = @(
        $coverageCandidates |
            Where-Object { $_.Directory.Name -match '^[0-9a-fA-F-]{36}$' } |
            Select-Object -ExpandProperty FullName
    )

    if (-not $coverageFiles) {
        $coverageFiles = @(
            $coverageCandidates |
                Where-Object { $_.FullName -notmatch '\\In\\' } |
                Select-Object -ExpandProperty FullName -Unique
        )
    }

    return [PSCustomObject]@{
        Category = $Category
        ProjectName = $projectName
        ProjectPath = $ProjectPath
        ResultDir = $resultDir
        TrxPath = $trxPath
        CoverageFiles = $coverageFiles
        ExitCode = $exitCode
        Summary = $summary
    }
}

function Write-ExecutionMarkdownReport {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Title,
        [Parameter(Mandatory = $true)][array]$Results
    )

    $totals = Get-ResultTotals -Results $Results

    $builder = New-Object System.Text.StringBuilder
    [void]$builder.AppendLine("# $Title")
    [void]$builder.AppendLine()
    [void]$builder.AppendLine("| Metric | Value |")
    [void]$builder.AppendLine("| --- | ---: |")
    [void]$builder.AppendLine("| Total tests | $($totals.Total) |")
    [void]$builder.AppendLine("| Executed | $($totals.Executed) |")
    [void]$builder.AppendLine("| Passed | $($totals.Passed) |")
    [void]$builder.AppendLine("| Failed | $($totals.Failed) |")
    [void]$builder.AppendLine("| Skipped | $($totals.Skipped) |")
    [void]$builder.AppendLine("| Duration | $(Format-Duration $totals.Duration) |")
    [void]$builder.AppendLine()

    foreach ($group in $Results | Group-Object Category) {
        [void]$builder.AppendLine("## $($group.Name)")
        [void]$builder.AppendLine()
        [void]$builder.AppendLine("| Project | Total | Passed | Failed | Skipped | Duration | TRX |")
        [void]$builder.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | --- |")

        foreach ($result in $group.Group) {
            $reportRoot = Split-Path -Parent $Path
            $trxRelative = Get-RelativePath -BasePath $reportRoot -TargetPath $result.TrxPath
            [void]$builder.AppendLine("| $($result.ProjectName) | $($result.Summary.Total) | $($result.Summary.Passed) | $($result.Summary.Failed) | $($result.Summary.Skipped) | $(Format-Duration $result.Summary.Duration) | [$([System.IO.Path]::GetFileName($result.TrxPath))]($trxRelative) |")
        }

        [void]$builder.AppendLine()
    }

    $failedProjects = $Results | Where-Object { $_.Summary.Failed -gt 0 }
    if ($failedProjects) {
        [void]$builder.AppendLine("## Failed tests")
        [void]$builder.AppendLine()

        foreach ($project in $failedProjects) {
            [void]$builder.AppendLine("### $($project.ProjectName)")
            [void]$builder.AppendLine()
            foreach ($failedTest in $project.Summary.FailedTests) {
                $message = if ($failedTest.Message) { " - $($failedTest.Message)" } else { "" }
                [void]$builder.AppendLine("- $($failedTest.Name)$message")
            }
            [void]$builder.AppendLine()
        }
    }

    [System.IO.File]::WriteAllText($Path, $builder.ToString())
}

function Write-ExecutionHtmlReport {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Title,
        [Parameter(Mandatory = $true)][array]$Results
    )

    $totals = Get-ResultTotals -Results $Results

    $rows = New-Object System.Text.StringBuilder
    foreach ($group in $Results | Group-Object Category) {
        [void]$rows.AppendLine("<tr class=""group-row""><td colspan=""7"">$($group.Name)</td></tr>")
        foreach ($result in $group.Group) {
            $reportRoot = Split-Path -Parent $Path
            $trxRelative = Get-RelativePath -BasePath $reportRoot -TargetPath $result.TrxPath
            $projectName = [System.Net.WebUtility]::HtmlEncode($result.ProjectName)
            [void]$rows.AppendLine("<tr><td>$projectName</td><td>$($result.Summary.Total)</td><td>$($result.Summary.Passed)</td><td>$($result.Summary.Failed)</td><td>$($result.Summary.Skipped)</td><td>$(Format-Duration $result.Summary.Duration)</td><td><a href=""$trxRelative"">TRX</a></td></tr>")
        }
    }

    $failedSections = New-Object System.Text.StringBuilder
    foreach ($project in $Results | Where-Object { $_.Summary.Failed -gt 0 }) {
        [void]$failedSections.AppendLine("<section><h2>$($project.ProjectName)</h2><ul>")
        foreach ($failedTest in $project.Summary.FailedTests) {
            $message = [System.Net.WebUtility]::HtmlEncode($failedTest.Message)
            $name = [System.Net.WebUtility]::HtmlEncode($failedTest.Name)
            if ($message) {
                [void]$failedSections.AppendLine("<li><strong>$name</strong><br /><code>$message</code></li>")
            }
            else {
                [void]$failedSections.AppendLine("<li><strong>$name</strong></li>")
            }
        }
        [void]$failedSections.AppendLine("</ul></section>")
    }

    $titleEncoded = [System.Net.WebUtility]::HtmlEncode($Title)
    $html = @"
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <title>$titleEncoded</title>
  <style>
    body { font-family: Segoe UI, Arial, sans-serif; margin: 2rem; color: #1f2937; }
    h1, h2 { margin-bottom: 0.5rem; }
    table { border-collapse: collapse; width: 100%; margin-top: 1rem; }
    th, td { border: 1px solid #d1d5db; padding: 0.6rem; text-align: left; }
    th { background: #f3f4f6; }
    .group-row td { background: #e5e7eb; font-weight: 600; }
    .summary { display: grid; grid-template-columns: repeat(6, minmax(120px, 1fr)); gap: 0.75rem; margin: 1.5rem 0; }
    .card { background: #f9fafb; border: 1px solid #e5e7eb; padding: 0.75rem; border-radius: 0.5rem; }
    code { white-space: pre-wrap; }
  </style>
</head>
<body>
  <h1>$titleEncoded</h1>
  <div class="summary">
    <div class="card"><strong>Total</strong><div>$($totals.Total)</div></div>
    <div class="card"><strong>Executed</strong><div>$($totals.Executed)</div></div>
    <div class="card"><strong>Passed</strong><div>$($totals.Passed)</div></div>
    <div class="card"><strong>Failed</strong><div>$($totals.Failed)</div></div>
    <div class="card"><strong>Skipped</strong><div>$($totals.Skipped)</div></div>
    <div class="card"><strong>Duration</strong><div>$(Format-Duration $totals.Duration)</div></div>
  </div>
  <table>
    <thead>
      <tr>
        <th>Project</th>
        <th>Total</th>
        <th>Passed</th>
        <th>Failed</th>
        <th>Skipped</th>
        <th>Duration</th>
        <th>TRX</th>
      </tr>
    </thead>
    <tbody>
$rows
    </tbody>
  </table>
  $failedSections
</body>
</html>
"@

    [System.IO.File]::WriteAllText($Path, $html)
}

function Write-CoverageSummary {
    param(
        [Parameter(Mandatory = $true)][string]$CoverageXmlPath,
        [Parameter(Mandatory = $true)][string]$SummaryMarkdownPath
    )

    [xml]$coverage = Get-Content -Raw $CoverageXmlPath
    $root = $coverage.coverage

    $lineRate = [double]$root.'line-rate'
    $branchRate = [double]$root.'branch-rate'
    $linesCovered = [int]$root.'lines-covered'
    $linesValid = [int]$root.'lines-valid'
    $branchesCovered = [int]$root.'branches-covered'
    $branchesValid = [int]$root.'branches-valid'

    $content = @"
# Coverage Summary

| Metric | Value |
| --- | ---: |
| Line coverage | $([math]::Round($lineRate * 100, 2))% |
| Branch coverage | $([math]::Round($branchRate * 100, 2))% |
| Covered lines | $linesCovered / $linesValid |
| Covered branches | $branchesCovered / $branchesValid |
"@

    [System.IO.File]::WriteAllText($SummaryMarkdownPath, $content.Trim() + [Environment]::NewLine)
}

$repoRoot = Get-RepoRoot
$absoluteOutputDir = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $OutputDir))
$architectureRoot = Join-Path $absoluteOutputDir "architecture"
$functionalRoot = Join-Path $absoluteOutputDir "functional"
$coverageRoot = Join-Path $absoluteOutputDir "coverage"
$coverageSettingsPath = Join-Path $repoRoot "tests\coverage.runsettings"
$solutionPath = Join-Path $repoRoot "LabViroMol.sln"
$architectureProject = Join-Path $repoRoot "tests\ArchitectureTests\LabViroMol.ArchitectureTests\LabViroMol.ArchitectureTests.csproj"

if (-not $KeepExistingOutput -and (Test-Path $absoluteOutputDir)) {
    Remove-Item -Recurse -Force $absoluteOutputDir
}

New-Item -ItemType Directory -Force -Path $architectureRoot, $functionalRoot, $coverageRoot | Out-Null

Push-Location $repoRoot
try {
    if (-not $SkipRestore) {
        & dotnet restore $solutionPath
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet restore failed."
        }

        & dotnet tool restore
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet tool restore failed."
        }
    }

    if (-not $SkipBuild) {
        & dotnet build $solutionPath -c $Configuration --no-restore
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet build failed."
        }
    }

    $architectureResults = ,(
        Invoke-TestProject -ProjectPath $architectureProject `
            -Category "Architecture" `
            -ResultsRoot $architectureRoot `
            -Configuration $Configuration `
            -NoBuild:$SkipBuild
    )

    Copy-Item -Path $architectureResults[0].TrxPath -Destination (Join-Path $architectureRoot "results.trx") -Force
    Write-ExecutionMarkdownReport -Path (Join-Path $architectureRoot "summary.md") -Title "Architecture test report" -Results $architectureResults
    Write-ExecutionHtmlReport -Path (Join-Path $architectureRoot "index.html") -Title "Architecture test report" -Results $architectureResults

    $unitProjects = Get-TestProjects -Root (Join-Path $repoRoot "tests\UnitTests")
    $integrationProjects = Get-TestProjects -Root (Join-Path $repoRoot "tests\IntegrationTests")
    $functionalResults = @()

    foreach ($project in $unitProjects) {
        $functionalResults += Invoke-TestProject -ProjectPath $project.FullName `
            -Category "Unit" `
            -ResultsRoot (Join-Path $functionalRoot "raw") `
            -Configuration $Configuration `
            -CoverageSettingsPath $coverageSettingsPath `
            -CollectCoverage `
            -NoBuild:$SkipBuild
    }

    foreach ($project in $integrationProjects) {
        $functionalResults += Invoke-TestProject -ProjectPath $project.FullName `
            -Category "Integration" `
            -ResultsRoot (Join-Path $functionalRoot "raw") `
            -Configuration $Configuration `
            -CoverageSettingsPath $coverageSettingsPath `
            -CollectCoverage `
            -NoBuild:$SkipBuild
    }

    Write-ExecutionMarkdownReport -Path (Join-Path $functionalRoot "summary.md") -Title "Unit and integration test report" -Results $functionalResults
    Write-ExecutionHtmlReport -Path (Join-Path $functionalRoot "index.html") -Title "Unit and integration test report" -Results $functionalResults

    $coverageFiles = @($functionalResults | ForEach-Object { $_.CoverageFiles } | Where-Object { $_ })
    if (-not $coverageFiles) {
        throw "No coverage files were generated for unit and integration tests."
    }

    $reportsArgument = "-reports:" + ($coverageFiles -join ';')
    $targetArgument = "-targetdir:" + $coverageRoot
    & dotnet tool run reportgenerator -- $reportsArgument $targetArgument "-reporttypes:Html;Cobertura" | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "reportgenerator failed."
    }

    $mergedCoveragePath = Join-Path $coverageRoot "Cobertura.xml"
    if (-not (Test-Path $mergedCoveragePath)) {
        throw "Merged Cobertura report was not produced."
    }

    Write-CoverageSummary -CoverageXmlPath $mergedCoveragePath -SummaryMarkdownPath (Join-Path $coverageRoot "Summary.md")

    $architectureFailed = $architectureResults | Where-Object { $_.ExitCode -ne 0 }
    $functionalFailed = $functionalResults | Where-Object { $_.ExitCode -ne 0 }
    if ($architectureFailed -or $functionalFailed) {
        exit 1
    }
}
finally {
    Pop-Location
}
