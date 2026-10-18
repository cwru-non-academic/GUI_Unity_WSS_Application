# GUI conformance automation

**GUI CI** and **WSS GUI preflight** emit **GUI general conformance**. Automated
acceptance runs the same ten canonical cases and eighteen unchanged assertion
expressions in ordinary .NET9, without starting or activating Unity.

The actual path is canonical fixture -> .NET scheduler -> shared production
`WssConformanceController` forwarding methods -> shared production `Stimulation`
commands/mappings -> shared Core/Params/Model stack -> `EmulatedWssTransport` ->
existing Core observation/evaluation framework. There are no UnityEngine stubs,
new expected values or independent case suite. This diagnostic entry point does
not exercise the UI managers; their direct Stimulation command implementation is
the same shared source tested here.

`WssConformanceController.cs` contains the shared forwarding/disposal guard code.
`WssConformanceController.Unity.cs` supplies the existing inactive GameObject host;
`tools/UnityConformance/WssConformanceController.DotNet.cs` supplies the .NET host.
The original constructor API and Unity source GUID remain intact.

The GUI's exact mode160000 wrapper gitlink selects its .NET tooling and production
source; the pinned C# fixture commit/hash selects the unchanged cases. Normal CI
uses public read-only checkouts and requires no App secret or persistent local
runner. Test projects/output are generated outside Assets and source checkouts.
Successful hosted evidence explicitly says `execution: dotnet`,
`unity_executed: false`; it cannot stand in for real Editor evidence.

The manual `tools/UnityConformance/Prepare-Harness.ps1` still prepares the real
Unity2022.3.62f3/UTF1.1.33 EditMode host, including both new shared source fragments
and the Unity lifetime fragment. The earlier actual Editor run passed ten cases;
a fresh manual import/run after the extraction remains outstanding. .NET CI does
not establish Unity importer/assembly resolution, MonoBehaviour/serialization,
scene/UI/input/rendering, player packaging or physical BLE/Serial behavior.

Development and dependency promotion target **dev**. `baseline_ready` remains
false for future candidate preflight/promotion approval. Candidate mode retains
producer/artifact/projection/source/fixture hashes and fresh candidate-only DLLs;
the full propagation flow remains gated on baseline approval and existing approved
App configuration. Main merges, default-branch publication, secrets, permissions,
runner installation and shipping releases were not performed.

See the wrapper `.github/UNITY_AUTOMATION.md` for exact source/evidence contracts,
check names, promotion prerequisites and manual Editor invocation.
