# WSS Unity GUI application

Unity application for the WSS stimulation console. The application uses the
[Unity WSS integration library](https://github.com/cwru-non-academic/Unity_WSS_Integration_Lib)
as a submodule. This README contains the repository's setup, validation and
maintenance documentation.

## Open the project

1. Install Git, [Git LFS](https://git-lfs.com), Unity Hub and **Unity 2022.3.62f3**.
   [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt) records the Editor
   version and revision used by the project.
2. Authorize your GitHub SSH key for the organization if required by
   [SSO](https://docs.github.com/en/enterprise-cloud@latest/authentication/authenticating-with-saml-single-sign-on/authorizing-an-ssh-key-for-use-with-saml-single-sign-on),
   then clone the application and its recorded submodules:

   ```bash
   git lfs install
   git clone --recurse-submodules git@github.com:cwru-non-academic/GUI_Unity_WSS_Application.git
   cd GUI_Unity_WSS_Application
   git lfs pull
   git submodule foreach --recursive git lfs pull
   ```

   For an existing checkout, run `git submodule update --init --recursive` and
   fetch LFS content in both the application and submodule. GitHub Desktop users
   should also verify that the submodule and its LFS files are present.
3. Add the repository directory in Unity Hub and open it with the recorded
   Editor version. The build scene is [Console.unity](Assets/Scenes/Console.unity).
   Keep the team on the same Editor version.

Operator settings and waveform/data files live in
[Assets/StreamingAssets](Assets/StreamingAssets). Preserve the selected device,
transport and stimulation configuration, including custom targets such as `0x80`.
The disposable conformance harness below exercises the emulator without loading
application scenes or this operator configuration.

<details>
<summary>Optional Git merge configuration</summary>

Configure UnityYAMLMerge in this checkout's `.git/config` using the path for your
installed Editor. The tracked `.gitconfig` selects the merge tool but does not
configure each developer's executable path.

```ini
[mergetool "unityyamlmerge"]
    trustExitCode = false
    cmd = '<path to UnityYAMLMerge>' merge -p "$BASE" "$REMOTE" "$LOCAL" "$MERGED"
```

Default Unity Hub locations are:

- Windows: `C:\Program Files\Unity\Hub\Editor\VERSION\Editor\Data\Tools\UnityYAMLMerge.exe`
- macOS: `/Applications/Unity/Hub/Editor/VERSION/Unity.app/Contents/Tools/UnityYAMLMerge`
- Linux: `/home/USERNAME/Unity/Hub/Editor/VERSION/Editor/Data/Tools/UnityYAMLMerge`

If using GitHub Desktop, select the local Git config in repository settings where
available. Optional [pre-commit](https://github.com/NYUGameCenter/Unity-Git-Config/blob/master/pre-commit)
and [post-merge](https://github.com/NYUGameCenter/Unity-Git-Config/blob/master/post-merge)
hooks go in `.git/hooks`; on macOS/Linux run `chmod 755 pre-commit post-merge` there.
Additional tutorials: [GitHub setup](https://youtu.be/iWniKp_O21o?feature=shared)
and [merge conflict resolution](https://youtu.be/vJIjV-GlGFg?feature=shared).

</details>

## Validation and CI

Pull requests and pushes to `dev` and `main` expose these checks:

- `GUI general conformance`
- `GUI platform compatibility (Windows)`
- `GUI platform compatibility (Ubuntu)`
- `GUI platform compatibility (macOS)`

Platform contexts are direct jobs without a reusable-workflow prefix. CI uses the
exact committed wrapper gitlink and pinned C# fixture. It runs the application's
production conformance forwarding methods and shared wrapper command/stack source
on .NET 9, then verifies package build/publish propagation and the wrapper's
production transport selector. Hosted results record `execution: dotnet` and
`unity_executed: false`.

The command path is canonical fixture → .NET scheduler → shared production
`WssConformanceController` forwarding methods → shared production `Stimulation`
commands/mappings → Core/Params/Model stack → `EmulatedWssTransport` → Core
observation/evaluation framework. CI uses the same ten canonical cases and eighteen
assertion expressions, without independent expected values or UnityEngine stubs.
The UI managers use the shared Stimulation implementation, but this diagnostic
entry point does not exercise those managers.

`WssConformanceController.cs` holds forwarding/disposal guards;
`WssConformanceController.Unity.cs` supplies the inactive GameObject host, and
`tools/UnityConformance/WssConformanceController.DotNet.cs` supplies the .NET host.
The constructor API and Unity source GUID remain intact. Generated test projects
and outputs stay outside Assets and source checkouts. Normal committed-state CI
uses public read-only checkouts without an App secret or persistent local runner.
Release receipt safety contracts also run in GUI CI.

This coverage does not establish Unity engine, importer/assembly resolution,
MonoBehaviour/serialization, scene/UI/input/rendering, Player/Editor, Mono/IL2CPP,
or physical BLE/Serial behavior. Windows BLE is load-only. The reviewed wrapper
supports macOS Mono routing and macOS-only plugin importers; the application's
recorded wrapper must contain that support before it can be used. Ubuntu Unity BLE
and macOS IL2CPP are unsupported. OS Serial runtime selection in the .NET probe
does not establish Unity Serial support. Licensed Unity tests remain separate.

<details>
<summary>Manual Unity EditMode conformance harness</summary>

[Prepare-Harness.ps1](tools/UnityConformance/Prepare-Harness.ps1) adapts the existing
C# fixture into a disposable EditMode project. It never enters Play mode, loads
the Console scene, or opens a device. Its call chain is:

`existing fixture → UnityConformanceAdapter → application WssConformanceController → Stimulation → Core/Params/Model → EmulatedWssTransport → shared IWssConformance`

Supply a C# integration checkout containing
`tests/Wss.CSharpImplementation.Tests/StimulationControllerConformanceTests.cs`.
The script verifies its accepted SHA-256 before transforming it. Core owns the
scenarios, fixture values and evaluator in
`CoreModule/Testing/{Scenarios/WssBehaviorScenarios.cs,IWssConformance.cs,WssConformance.cs}`.
Runtime-edit and stop assertions retain original message-history and post-stop
observations. The catalog comprises initialization, direct analog, six normalized
inputs, runtime Event ratio edit, and stop.

Unity Test Framework 1.1.33 compatibility transformations retain all 18 original
`Assert.That` expressions:

- C# 9 namespace and explicit using declarations.
- Newtonsoft serialization of the same objects instead of System.Text.Json.
- Controller factory selection of the Unity/application adapter.
- Existing async bodies become helpers awaited by EditMode `IEnumerator` entry
  points; normalized IDs use the canonical catalog through `ValueSource`.
- Polling yields an Editor update on the main thread instead of `Task.Delay(1)`,
  retaining finite poll counts.
- `Assert.Multiple` becomes ordered, fail-fast assertions because pinned NUnit
  lacks grouped assertions; values and individual assertions remain unchanged.

Use a new destination outside the application checkout:

```powershell
./tools/UnityConformance/Prepare-Harness.ps1 `
  -CSharpIntegrationRoot '<local CSharp_WSS_Integration_Lib checkout>' `
  -Destination '<new disposable local directory>'
```

The generated `conformance-inputs.json` records source/dependency hashes.
Preparation fails if the canonical fixture hash changes; review compatibility
transformations before accepting a replacement hash. Preparation does not start
Unity or run tests. Execute with a locally installed, Hub-activated Editor:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe' `
  -batchmode -nographics -projectPath '<prepared directory>' `
  -runTests -testPlatform EditMode `
  -testResults '<results.xml>' -logFile '<unity.log>'
```

Do not add `-quit`. Require valid XML with all ten catalog cases passing; missing
XML or absent cases is not a pass. Use an outer process timeout in automation.
The harness includes shared source fragments, the Unity lifetime fragment, real
PluginImporter checks and an explicit load-only BLE diagnostic. Manual Editor
evidence remains separate from ordinary .NET CI.

</details>

<details>
<summary>Desktop BLE diagnostic and macOS Player preparation</summary>

`WindowsBleTransportSmokeTest.VerifyInstallation` preserves its serialized type
name and also supports macOS Mono. It verifies registration, repeated disconnected
construction/disposal, unique WSS assembly identities and the native ABI without
scanning, connecting or writing. It has no automatic Start/OnEnable behavior.
The optional fake BLE lifetime host is test-only and remains compatible with the
default emulator host.

For a macOS Player using a wrapper containing the reviewed macOS support, select
Mono, macOS 11 or newer, a compatible arm64/x86_64 architecture and an appropriate
Bluetooth Usage Description in
[Player Settings](https://docs.unity3d.com/2022.3/Documentation/Manual/PlayerSettings-macOS.html).
The project's deployment target and usage description remain operator-controlled.
Windows tests, fake transports and .NET macOS ABI tests do not establish native
macOS Unity Player, IL2CPP or physical BLE operation.

</details>

<details>
<summary>Maintainer reference: dependency pins, promotion and release receipts</summary>

The wrapper gitlink in `Assets/SubModules/WSSInterfacingModule` selects runtime
source and payload. Immutable reusable-workflow references select validation
tooling; updating those references does not update the runtime dependency.
Ordinary builds and CI use the recorded gitlink. Adopt a new wrapper through
reviewed application promotion after integration libraries accept the same final
Core candidate.

Exact identifiers have distinct purposes:

- The wrapper gitlink fixes the runtime revision used by a checkout.
- Reusable-workflow commit pins fix the reviewed validation implementation.
- `.github/unity-ci.json` fixes the canonical C# fixture commit and SHA-256, Editor
  revision, test framework and .NET SDK. The fixture hash is also enforced by the
  manual harness script to reject unreviewed fixture transformations.
- [dependency-projection.json](tools/UnityConformance/dependency-projection.json)
  records the original accepted Core producer commit/run/artifact, archive digest
  and 15 projected DLL hashes. It is a provenance record, not evidence that a new
  candidate or the current application runtime passed Unity validation.
- Candidate inputs, generated manifests and release receipts identify the exact
  source, fixture, producer and bytes whose evidence is being evaluated.

Operational values remain in Git metadata, workflows, configuration and provenance
records rather than being duplicated in setup instructions.

Development and dependency promotion target `dev`. `baseline_ready` is true in
the checked-in configuration following approval of the .NET path; .NET results
remain separate from Unity Editor evidence. Candidate mode retains
producer/artifact/projection/source/fixture hashes and uses fresh candidate-only
DLLs. The propagation flow requires default-branch workflow publication and the
approved App configuration.

After human review and merge of the wrapper dependency, a fresh GUI candidate
preflight tests that exact merged wrapper and current GUI source. Only then can
the GUI-only write token create the gitlink-only PR. Exact reruns reuse the PR;
conflicts stop without force-pushing. Human merges remain required.

`release-receipt.yml` accepts the exact Core producer run and frozen plan ID. It
emits a read-only dev acceptance receipt after checking the human-reviewed bot
merge, paired preflights, candidate-byte/projection checks and fresh final-merge
CI. GUI acceptance additionally checks the actual wrapper gitlink and its
post-merge preflight. Main supplies trusted tooling; stable releases remain
disabled. Receipt coverage describes the .NET harness. Consult the wrapper's
current README for its shared validation and promotion contracts.

</details>

<details>
<summary>Historical Unity verification and projection provenance</summary>

The following record describes the earlier projection and manual run, not fresh
validation of current source. Evidence and backup locations are retained for
investigation of that migration.

The original universal payload remained in the reference CLI integration checkout
outside Unity Assets. Windows managed/native assets moved to the wrapper; displaced
application copies were preserved externally. Newtonsoft uses UPM 3.2.1 and Vectors
uses Unity's framework, with corresponding payload importers disabled. Other
projected dependencies, including root net462 Ports, require actual Unity/Mono
runtime validation. The original projection covered Windows x64; no net8 runtime
trees or Linux backend were imported.

On 18 October 2026:

- Application source and both disposable harness assemblies compiled with installed
  Unity 2022.3.62f3 compiler/references and updated DLL paths.
- All 18 original assertions matched byte-for-byte; all 15 projected DLL hashes
  matched the projection record, and Git whitespace checks passed.
- An initial sandbox launch could not open Owner's `CurlRequestCache.db`. The
  approved Owner-context launch succeeded without ACL changes or cache clearing.
- Actual Unity import exposed missing Core/JSON references in the test asmdef;
  explicit precompiled references corrected it without changing cases or assertions.
- Actual Unity 2022.3.62f3 EditMode execution at 14:06 America/New_York discovered,
  executed and passed ten cases, with zero failed, skipped or inconclusive results;
  duration 5.997 seconds and Editor exit code 0. Recorded evidence was
  `results-run2.xml` and `unity-run2.log` in
  `%TEMP%/wss-unity-conformance-20261018-local-v4`.
- Approved restoration of `System.Security.AccessControl.dll.meta`,
  `System.Security.Permissions.dll.meta` and `System.Security.Principal.Windows.dll.meta`
  restored committed importer settings/GUIDs. Their diffs were empty and all 15
  projected binary hashes remained unchanged; no Git trust/ACL change occurred.

That Editor result predates shared-source extraction and does not validate the
split-source revision. No newer actual Editor result is recorded here.
Original touched sources and displaced DLLs were preserved at
`C:\Users\Owner\AppData\Local\Temp\wss-unity-before-4a2433b2da284714b7fc39ed92962cfb`.
Temporary evidence and backups are not durable release artifacts. Emulator
execution does not establish UI managers, Player packaging, BLE/Serial operation
or device safety; manager checks require separate manual validation.

</details>

## Third-party material

Vendor assets retain their bundled notices and documentation: Modern UI Pack's
`Read Me.txt` includes attribution and vendor links; TextMesh Pro includes its
original user guide, Liberation Sans OFL license and EmojiOne attribution.
Their Unity `.meta` files are preserved. TextMesh Pro line-breaking text files
are runtime resources, and `ProjectVersion.txt` is Editor metadata.
