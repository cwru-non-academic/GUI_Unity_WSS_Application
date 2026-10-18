# Shared Core conformance inside Unity

This is an invocation adapter for the existing C# integration fixture, not a new
application test suite. It prepares a disposable **EditMode** project. It never
enters Play mode, loads the Console scene, or opens a device. The call chain is:

`existing fixture → UnityConformanceAdapter → application WssConformanceController → Stimulation → Core/Params/Model → EmulatedWssTransport → shared IWssConformance`

The fixture comes from the C# integration checkout supplied to preparation:
`tests/Wss.CSharpImplementation.Tests/StimulationControllerConformanceTests.cs`.
Its accepted SHA-256 is pinned in `Prepare-Harness.ps1`. Core owns the scenarios,
fixture values and stimulation/initialization evaluator in
`CoreModule/Testing/{Scenarios/WssBehaviorScenarios.cs,IWssConformance.cs,WssConformance.cs}`.
Runtime-edit and stop assertions remain in the original fixture, including their
message-history and post-stop observations. No expected values are copied here.

The existing catalog comprises initialization, direct analog, six normalized
inputs, runtime Event ratio edit, and stop. The generated source retains all 18
original `Assert.That` expressions. UTF 1.1.33 needs these compatibility changes:

- C# 9 namespace and explicit using declarations.
- Newtonsoft serialization of the same fixture objects instead of System.Text.Json.
- The controller factory selects the Unity/application adapter.
- Existing async bodies become helpers awaited by EditMode `IEnumerator` entry
  points. Normalized IDs come from the canonical catalog through `ValueSource`.
- Polling yields an Editor update on the main thread instead of `Task.Delay(1)`;
  existing finite poll counts remain unchanged.
- `Assert.Multiple` becomes ordered, fail-fast assertions because Unity's pinned
  NUnit lacks grouped assertion support. Values and individual assertions remain.

Prepare from a local PowerShell session. Use a **new destination outside the app**:

```powershell
./tools/UnityConformance/Prepare-Harness.ps1 `
  -CSharpIntegrationRoot '<local CSharp_WSS_Integration_Lib checkout>' `
  -Destination '<new disposable local directory>'
```

The generated `conformance-inputs.json` records source/dependency hashes. Review
changes if the pinned canonical fixture hash no longer matches; preparation fails
instead of silently accepting a different fixture.

For local execution, use the installed, Hub-activated Editor:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe' `
  -batchmode -nographics -projectPath '<prepared directory>' `
  -runTests -testPlatform EditMode `
  -testResults '<results.xml>' -logFile '<unity.log>'
```

Do not add `-quit` to the test invocation. A successful run must produce valid
results with all ten catalog cases passing; missing XML or absent cases is not a
pass. Use an outer process timeout in automation. Dev-targeted workflow definitions
are now prepared in both Unity repositories, but remain explicitly gated: no supported
unattended GitHub-hosted Unity Personal activation route was verified. GitHub-hosted
CI is permitted by the user; the local-only restriction applies to Codex development.
No runner or alternative approval handoff has been selected. See
`../../.github/UNITY_AUTOMATION.md` and the wrapper's `.github/UNITY_AUTOMATION.md`.

`dependency-projection.json` records the selected accepted universal payload and
15 projected DLL hashes. The full original payload remains in the reference CLI
integration checkout outside Unity Assets. Windows managed/native assets now live
with the wrapper; obsolete app copies were preserved in the external backup noted
below. Newtonsoft uses UPM 3.2.1, and Vectors uses Unity's framework; the corresponding
payload copies have disabled importers. Other projected dependencies, including
root net462 Ports, still require real Unity/Mono runtime validation. No net8 runtime
trees or Linux backend were imported. Only Windows x64 is projected here.

## Verification on 18 October 2026

- Full application source compilation passed using installed Unity 2022.3.62f3
  compiler/references and the existing app compilation inputs with updated DLL paths.
- Both disposable harness assemblies compiled with the same compiler/references.
- All 18 original assertion expressions matched byte-for-byte; all 15 projected
  DLL hashes matched the projection record. Git whitespace checks passed.
- Initial sandbox invocation failed before import because it could not open
  Owner's `CurlRequestCache.db`. After explicit user approval, the normal reviewed
  Owner-context launch succeeded without changing ACLs or clearing caches.
- Actual Unity import exposed missing Core/JSON references in the test asmdef.
  Explicit precompiled references corrected that configuration; no cases or
  assertions changed.
- **Actual Unity 2022.3.62f3 EditMode execution passed on 18 October 2026 at
  14:06 America/New_York:** 10 discovered, 10 executed, 10 passed, zero failed,
  skipped or inconclusive; duration 5.997 seconds; Editor exit code 0.
  Evidence is `results-run2.xml` and `unity-run2.log` in the disposable v4 harness
  (`%TEMP%/wss-unity-conformance-20261018-local-v4`). This is execution inside the
  real Unity Editor through the application conformance entry point and wrapper.
- With explicit user approval, Git in Owner context restored only
  `System.Security.AccessControl.dll.meta`, `System.Security.Permissions.dll.meta`
  and `System.Security.Principal.Windows.dll.meta` to their committed importer
  settings/GUIDs. Their diff is empty. The corresponding DLL hashes and all 15
  projected binary hashes remain unchanged. No Git trust/ACL changes were made.

Original touched sources/displaced DLLs were preserved at
`C:\Users\Owner\AppData\Local\Temp\wss-unity-before-4a2433b2da284714b7fc39ed92962cfb`.
These temporary backups are not a publication or durable release artifact.

The application conformance entry point is intentionally separate from UI
managers. Editor emulator execution cannot establish GUI input/rendering,
player packaging, BLE/Serial communication, or device safety.
Manual manager checks remain with the user. The scoped dev publication is separate
from hosted enablement. No hardware, stimulation, merges, releases, runners,
licenses or Git trust settings were changed.
