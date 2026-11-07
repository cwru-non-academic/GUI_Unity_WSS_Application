param(
 [Parameter(Mandatory)][string]$WrapperRoot,
 [Parameter(Mandatory)][string]$ToolRoot,
 [Parameter(Mandatory)][string]$OutputRoot
)
$ErrorActionPreference = 'Stop'
# Called only after the existing platform runner has validated exact source/tooling.
# Reuse its trusted installed-payload metadata and projection validation; never infer
# modern applicability from a branch name, file existence, or a test compile.
. (Join-Path $ToolRoot '.github/scripts/UnityContract.ps1')
. (Join-Path $ToolRoot '.github/scripts/UnityProfiles.ps1')
$profile = Resolve-UnityExecutionProfile $WrapperRoot 'committed' '' ''
if ($profile.profile -cnotin @('legacy-v1','layered-v1')) { throw 'Unknown installed profile.' }
if (Test-Path -LiteralPath $OutputRoot) { throw 'OutputRoot must be new.' }
New-Item -ItemType Directory -Path $OutputRoot | Out-Null
if ($profile.profile -ceq 'legacy-v1') {
 @{ profile='legacy-v1'; status='not-applicable'; modern_acceptance=$false; reason='GUI source-boundary cases require the modern HostLifecycle source and layered payload.' } | ConvertTo-Json | Set-Content (Join-Path $OutputRoot 'applicability.json')
 Write-Output 'NOT APPLICABLE: verified legacy ordinary profile; no modern GUI source-boundary acceptance.'
 return
}
$harness = Join-Path $OutputRoot 'harness'
& (Join-Path $PSScriptRoot 'Prepare.ps1') -WrapperRoot $WrapperRoot -Destination $harness
$payload = Join-Path $WrapperRoot 'Plugins/WSS_Core_Interface'
dotnet test (Join-Path $harness 'GuiSourceBoundary.Tests.csproj') -c Release "-p:WSS_ARTIFACT_DIR=$payload" "-p:WSS_TRANSPORT_ARTIFACT_DIR=$payload" --filter 'TestCategory=GuiSourceBoundary' --logger 'trx;LogFileName=gui-source-boundary.trx' --results-directory $OutputRoot
if ($LASTEXITCODE -ne 0) { throw 'GUI source-boundary tests failed.' }
[xml]$result = Get-Content (Join-Path $OutputRoot 'gui-source-boundary.trx') -Raw
$counts = $result.TestRun.ResultSummary.Counters
if ($counts.total -ne '5' -or $counts.executed -ne '5' -or $counts.passed -ne '5' -or $counts.failed -ne '0' -or $counts.notExecuted -ne '0') { throw 'Require exactly five discovered, executed, passing GUI cases without skips.' }
$expected = @(
 'CalibrationEditsFlowThroughInputFloatToCoreDefaultAmplitudeAndPulseWidth',
 'UnlinkedInputDoesNotChangeAppliedOutputAndLowLinkedInputZerosIt',
 'RadioResetReplacesCoreButRetainsAppliedCalibrationDespiteMalformedNonCoreFiles',
 'ConsoleSceneResetRadioBinding', 'ConsoleSceneResetAmpBinding'
)
$actual = @($result.TestRun.Results.UnitTestResult | ForEach-Object { [string]$_.testName })
if ($actual.Count -ne 5 -or @(Compare-Object ($expected | Sort-Object) ($actual | Sort-Object)).Count -ne 0) { throw 'Unexpected GUI test inventory.' }
@{ profile='layered-v1'; status='passed'; execution='source-boundary with engine shim and emulator'; discovered=5; executed=5; passed=5; failed=0; skipped=0 } | ConvertTo-Json | Set-Content (Join-Path $OutputRoot 'applicability.json')
