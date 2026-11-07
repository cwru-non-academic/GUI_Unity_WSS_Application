param([Parameter(Mandatory)][string]$WrapperRoot, [Parameter(Mandatory)][string]$Destination)
$ErrorActionPreference = 'Stop'
$wrapper = (Resolve-Path -LiteralPath $WrapperRoot).Path
$gui = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
if (Test-Path -LiteralPath $Destination) { throw 'Destination must be new.' }
# Reuse the pinned host fixture, project dependencies and engine shim. Never copy production logic.
$fixture = Join-Path $wrapper 'Tests~/HostLifecycle'
$shim = Get-Content -LiteralPath (Join-Path $fixture 'UnityEngineShim.cs') -Raw
$replacements = [ordered]@{
 'public class MonoBehaviour { public bool isActiveAndEnabled { get; set; } = true; }' = 'public class MonoBehaviour { public bool isActiveAndEnabled { get; set; } = true; protected void StartCoroutine(System.Collections.IEnumerator routine) { throw new NotSupportedException("Coroutine scheduling is outside this source-boundary fixture."); } }'
 'public enum KeyCode { A }' = 'public enum KeyCode { A, F8, Escape, Space, UpArrow, DownArrow, LeftArrow, RightArrow }'
 'public static bool GetKeyDown(KeyCode key) { bool result = NextA; NextA = false; return result; }' = 'public static bool GetKeyDown(KeyCode key) { if (key != KeyCode.A) return false; bool result = NextA; NextA = false; return result; } public static bool GetKeyUp(KeyCode key) => false;'
}
foreach ($entry in $replacements.GetEnumerator()) {
 if (($shim.Split(@($entry.Key), [StringSplitOptions]::None)).Count -ne 2) { throw "Host shim contract changed: $($entry.Key)" }
 $shim = $shim.Replace($entry.Key, $entry.Value)
}
[xml]$project = Get-Content -LiteralPath (Join-Path $fixture 'Wss.Unity.HostLifecycle.Tests.csproj') -Raw
foreach ($compile in @($project.Project.ItemGroup.Compile)) {
 $compile.Include = [IO.Path]::GetFullPath((Join-Path $fixture $compile.Include))
}
$group = $project.CreateElement('ItemGroup')
foreach ($path in @((Join-Path $fixture 'HostReloadTests.cs'), (Join-Path $gui 'Assets/Scripts/MinMax.cs'), (Join-Path $PSScriptRoot 'GuiBoundaryTests.cs'), (Join-Path $PSScriptRoot 'UiAdapters.cs'))) {
 $node = $project.CreateElement('Compile'); $node.SetAttribute('Include', $path); $null=$group.AppendChild($node)
}
foreach ($pair in @(@('Assets/Scenes/Console.unity','Console.unity'), @('Assets/Scripts/MinMax.cs.meta','MinMax.cs.meta'))) {
 $node=$project.CreateElement('None'); $node.SetAttribute('Include',(Join-Path $gui $pair[0])); $node.SetAttribute('Link',$pair[1]); $node.SetAttribute('CopyToOutputDirectory','PreserveNewest'); $null=$group.AppendChild($node)
}
$node=$project.CreateElement('None'); $node.SetAttribute('Include',(Join-Path $wrapper 'UnityImplementation/Stimulation.cs.meta')); $node.SetAttribute('Link','Stimulation.cs.meta'); $node.SetAttribute('CopyToOutputDirectory','PreserveNewest'); $null=$group.AppendChild($node)
$null=$project.Project.AppendChild($group)
New-Item -ItemType Directory -Path $Destination | Out-Null
$project.Save((Join-Path $Destination 'GuiSourceBoundary.Tests.csproj'))
[IO.File]::WriteAllText((Join-Path $Destination 'UnityEngineShim.cs'),$shim)
Write-Output (Join-Path $Destination 'GuiSourceBoundary.Tests.csproj')
