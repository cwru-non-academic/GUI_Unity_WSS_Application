param(
    [Parameter(Mandatory)][string]$CSharpIntegrationRoot,
    [Parameter(Mandatory)][string]$Destination,
    [string]$WrapperRoot = '',
    [string]$ProjectionRoot = ''
)
$ErrorActionPreference = 'Stop'
$appRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$target = [IO.Path]::GetFullPath($Destination)
if (Test-Path -LiteralPath $target) { throw 'Harness destination must be new.' }
if ($target.StartsWith($appRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Create the disposable harness outside the application checkout.'
}
$fixturePath = Join-Path $CSharpIntegrationRoot 'tests/Wss.CSharpImplementation.Tests/StimulationControllerConformanceTests.cs'
$fixtureHash = (Get-FileHash -LiteralPath $fixturePath -Algorithm SHA256).Hash
if ($fixtureHash -ne '1CDFB9448A1776011EADC9AEF8356362216032013A4F268F50242746C50A0B44') {
    throw 'Canonical fixture changed. Review compatibility transformations before updating the accepted hash.'
}
$fixture = [IO.File]::ReadAllText($fixturePath)
# Compatibility only: retain every case, expected value, wait, observation and assertion.
$fixture = $fixture.Replace('using System.Text.Json;', 'using Newtonsoft.Json;')
$fixture = $fixture.Replace('JsonSerializer.Serialize(', 'JsonConvert.SerializeObject(')
# Unity's pinned NUnit lacks Assert.Multiple. Keep every assertion and its order;
# only grouped failure reporting becomes fail-fast (no new assertion implementation).
$multiple = '(?ms)Assert\.Multiple\(\(\) =>(?<body>\s*\{.*?)^(?<indent>\s*)\}\);'
if ([regex]::Matches($fixture, $multiple).Count -ne 3) { throw 'Expected exactly three grouped assertion blocks.' }
$fixture = [regex]::Replace($fixture, $multiple, 'new Action(() =>${body}${indent}}).Invoke();')
# UTF 1.1.33 cannot run async Task tests directly. Keep their bodies as private
# operations and expose IEnumerator EditMode adapters with canonical ValueSource inputs.
$caseMethod = '(?m)^    \[Test\]\r?\n    public async Task (?<name>\w+)\(\)'
if ([regex]::Matches($fixture, $caseMethod).Count -ne 4) { throw 'Expected four non-parameterized canonical cases.' }
$fixture = [regex]::Replace($fixture, $caseMethod, {
    param($match)
    $caseName = $match.Groups['name'].Value
    "    [UnityEngine.TestTools.UnityTest]`n    public System.Collections.IEnumerator $caseName() => UnityConformanceAdapter.Wait(${caseName}Async());`n`n    private async Task ${caseName}Async()"
})
$normalizedMethod = '[TestCaseSource(nameof(NormalizedScenarios))]' + "`r`n" + '    public async Task NormalizedScenario(WssNormalizedStimulationScenario scenario)'
if (!$fixture.Contains($normalizedMethod)) {
    $normalizedMethod = $normalizedMethod.Replace("`r`n", "`n")
}
if (!$fixture.Contains($normalizedMethod)) { throw 'Expected the canonical normalized-case method.' }
$fixture = $fixture.Replace($normalizedMethod, @'
[UnityEngine.TestTools.UnityTest]
    public System.Collections.IEnumerator NormalizedScenario(
        [ValueSource(nameof(UnityNormalizedScenarioIds))] string scenarioId)
        => UnityConformanceAdapter.Wait(NormalizedScenarioAsync(
            WssBehaviorScenarios.NormalizedStimulation.Single(scenario => scenario.Id == scenarioId)));

    private static IEnumerable<string> UnityNormalizedScenarioIds
        => WssBehaviorScenarios.NormalizedStimulation.Select(scenario => scenario.Id);

    private async Task NormalizedScenarioAsync(WssNormalizedStimulationScenario scenario)
'@)
$fixture = $fixture.Replace('await Task.Delay(1);', 'await UnityConformanceAdapter.NextUpdate();')
$fixture = $fixture.Replace('namespace Wss.CSharpImplementation.Tests;', "namespace Wss.CSharpImplementation.Tests`n{") + "`n}`n"
$fixture = "#nullable enable`nusing System;`nusing System.Collections.Generic;`nusing System.IO;`nusing System.Linq;`nusing System.Threading.Tasks;`nusing StimulationController = UnityConformanceAdapter;`n" + $fixture
$factory = '(?s)private static StimulationController CreateController\(string fixtureDirectory\) =>\s*new\(new StimulationOptions\s*\{\s*ConfigPath = fixtureDirectory,\s*Transport = StimulationTransportKind.Conformance,\s*MaxSetupTries = 1,\s*TickIntervalMs = 1\s*\}\);'
if ([regex]::Matches($fixture, $factory).Count -ne 1) { throw 'Expected exactly one canonical controller factory.' }
$fixture = [regex]::Replace($fixture, $factory, 'private static StimulationController CreateController(string fixtureDirectory) => new(fixtureDirectory);')
$runtime = Join-Path $target 'Assets/WssRuntime'
$tests = Join-Path $target 'Assets/Tests/Editor'
foreach ($directory in @($runtime, $tests, (Join-Path $target 'Packages'), (Join-Path $target 'ProjectSettings'))) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
}
$wrapper = if ($WrapperRoot) { [IO.Path]::GetFullPath($WrapperRoot) } else { Join-Path $appRoot 'Assets/SubModules/WSSInterfacingModule' }
foreach ($name in @('Stimulation.cs','Stimulation.Commands.cs','Stimulation.Stack.cs','StimualtionParams.cs','StimulationBasic.cs','WssTransportSupport.cs')) {
    Copy-Item -LiteralPath (Join-Path $wrapper "UnityImplementation/$name") -Destination $runtime
}
Copy-Item -LiteralPath (Join-Path $appRoot 'Assets/Scripts/Diagnostics/WssConformanceController.cs') -Destination $runtime
Copy-Item -LiteralPath (Join-Path $appRoot 'Assets/Scripts/Diagnostics/WssConformanceController.Unity.cs') -Destination $runtime
if ($ProjectionRoot) {
    # Candidate mode never copies committed DLLs: only the verified fresh projection.
    $plugins = Join-Path $runtime 'Plugins/WSS_Core_Interface'
    New-Item -ItemType Directory -Path $plugins -Force | Out-Null
    Copy-Item -Path (Join-Path $ProjectionRoot '*') -Destination $plugins -Recurse
} else {
    Copy-Item -LiteralPath (Join-Path $wrapper 'Plugins') -Destination $runtime -Recurse
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'UnityConformanceAdapter.cs') -Destination $tests
[IO.File]::WriteAllText((Join-Path $tests 'StimulationControllerConformanceTests.cs'), $fixture)
@{ name='Wss.Conformance.Runtime'; references=@(); autoReferenced=$true } | ConvertTo-Json | Set-Content (Join-Path $runtime 'Wss.Conformance.Runtime.asmdef')
@{ name='Wss.Conformance.Editor'; references=@('Wss.Conformance.Runtime','UnityEngine.TestRunner','UnityEditor.TestRunner'); includePlatforms=@('Editor'); overrideReferences=$true; precompiledReferences=@('nunit.framework.dll','WSS_Core_Interface.dll','Newtonsoft.Json.dll'); autoReferenced=$false } | ConvertTo-Json | Set-Content (Join-Path $tests 'Wss.Conformance.Editor.asmdef')
@{ dependencies=@{ 'com.unity.test-framework'='1.1.33'; 'com.unity.nuget.newtonsoft-json'='3.2.1' } } | ConvertTo-Json | Set-Content (Join-Path $target 'Packages/manifest.json')
Copy-Item -LiteralPath (Join-Path $appRoot 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $target 'ProjectSettings')
$sourceFiles = @($fixturePath, (Join-Path $PSScriptRoot 'UnityConformanceAdapter.cs'), (Join-Path $appRoot 'Assets/Scripts/Diagnostics/WssConformanceController.cs')) + @(Get-ChildItem -LiteralPath $runtime -Recurse -File | Select-Object -ExpandProperty FullName)
@{ canonicalFixture=$fixturePath; canonicalFixtureSha256=$fixtureHash; editor='2022.3.62f3'; testFramework='1.1.33'; execution='EditMode only; not executed by preparation'; files=@($sourceFiles | ForEach-Object { @{ path=$_; sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash } }) } | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $target 'conformance-inputs.json')
Write-Output "Prepared $target. No Unity process or test was started."
