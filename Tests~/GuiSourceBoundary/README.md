# GUI source-boundary regression fixture

This additive suite compiles actual `Assets/Scripts/MinMax.cs` and the application's exact pinned wrapper production source against the real packaged Core. It reuses the unchanged wrapper `Tests~/HostLifecycle/HostReloadTests.cs` setup, emulator, pump and cleanup via a reflection bridge. `Prepare.ps1` derives the project from the wrapper's existing project, retains its package/reference/source list, links the GUI tests and source, and extends only three exact engine-shim declarations. UI adapters store values only; unsupported coroutine scheduling throws rather than pretending to implement Unity.

Five application-owned NUnit cases cover:
- Real increase/decrease/reset calibration, including amplitude rollover, followed by inputFloat -> StimWithMode -> Core output (PW and defaultPA).
- Linked/unlinked input and the <=0.01 zero-input branch.
- The real resetRadio boundary replacing the owner and retaining applied calibration, verified by subsequent normal input/output despite malformed calibration/model files on disk.
- Console.unity resetRadio and MinMax.resetAmp callbacks, target component IDs and script GUIDs.

An additive step in each existing GUI ordinary platform job reuses its exact wrapper/tooling checkouts, installed SDK and verified package environment. Run.ps1 revalidates the installed projection and metadata through the pinned tooling. Modern layered-v1 runs only `TestCategory=GuiSourceBoundary`, requiring five discovered/executed/passing tests without skips; verified legacy-v1 is explicitly not applicable and is not modern acceptance; invalid or unknown evidence fails closed. Windows, Ubuntu and both existing macOS consumer jobs discover the same five cases. It does not change the canonical conformance inventories or runners. The reused wrapper cases remain available in the generated project, but are not counted as application tests. A modern HostLifecycle-capable wrapper is required; do not transplant this suite onto MAIN without the actual modern wrapper adoption.

Prepare outside the checkout with PowerShell:

    ./Tests~/GuiSourceBoundary/Prepare.ps1 -WrapperRoot <exact-wrapper-checkout> -Destination <new-directory>

Compile only (safe without running the WSS payload):

    dotnet build <new-directory>/GuiSourceBoundary.Tests.csproj -c Release -p:WSS_ARTIFACT_DIR=<wrapper>/Plugins/WSS_Core_Interface -p:WSS_TRANSPORT_ARTIFACT_DIR=<wrapper>/Plugins/WSS_Core_Interface

Hosted runtime proof must run `dotnet test` with those same properties and `--filter TestCategory=GuiSourceBoundary`. Local compilation is not behavioral acceptance. This is source-boundary/shim coverage, not Unity Editor/Play, rendered UI, Unity serialization runtime, native transport or hardware proof. There is no invented explicit/default-path reload UI.

## Separate manual Editor observation (not fixed here)

The application's `conformance/Prepare-UnityHarness.ps1` copy list omits modern `WssReloadState.cs`. Wrapper `Invoke-UnityConformance.ps1` calls it only in its selected-test branch; baseline uses `tools/UnityConformance/Prepare-Harness.ps1`. At GUI tooling pin `597eead0a22ba48488c16f82d37853f8b983c6f8`, no workflow/script caller of `Invoke-UnityConformance.ps1` was found, and `.github/UNITY_AUTOMATION.md` identifies this as a manual licensed-Editor route. This is a latent selected manual-harness preparation gap, not a demonstrated active CI failure. Proposed separate fix: include the actual wrapper `WssReloadState.cs` in that selected copy list, then verify generated source completeness and separately obtain a real licensed Editor run. No Editor runner or copy-list changes are included here.
