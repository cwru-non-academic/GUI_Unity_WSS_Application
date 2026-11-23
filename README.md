# WSS Unity GUI Application

The Unity Console scene controls the Wearable Stimulation System (WSS).

## First stimulation in the Unity Editor

### Install and open the Console scene

Install Git, Git LFS, Unity Hub, and Unity Editor **2022.3.62f3**, the version in [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt). Use the same editor version when reopening the project. The integration submodule uses an SSH URL. Set up GitHub SSH access and authorize the key for organization SSO if your account requires it.

```bash
git lfs install
git clone --branch dev --recurse-submodules https://github.com/cwru-non-academic/GUI_Unity_WSS_Application.git
cd GUI_Unity_WSS_Application
git lfs pull
```

For a clone without dependencies, run `git submodule update --init --recursive` followed by `git lfs pull`. If Unity reports missing scripts or DLLs, check those commands completed and the `Assets/SubModules/WSSInterfacingModule` checkout is populated.

In Unity Hub, add the cloned folder as an existing project. Open it in the pinned editor and let the packages finish importing. In Unity's **Project** window, open **Assets → Scenes → Console**. Check **Window → General → Console** for compilation errors before entering Play mode.

### Prepare the profile and select a transport

Follow the [hardware overview](https://cwru-non-academic.github.io/WSS_Documentation/hardwareDocs/wsshardware.html) for the physical setup and the [bench verification handoff](https://github.com/cwru-non-academic/WSS_Documentation/blob/dev/apiDocs/conceptual/start-here-using-an-application.md#bench-verification) for the approved profile and measurement procedure. Use a bench setup with no person connected.

The Editor reads `stimConfig.json`, `stimParams.json`, and `modelConfig.json` from `Assets/StreamingAssets/`. Save a copy of the shipped files outside that directory before applying your bench profile. In `stimConfig.json`, match the device count, firmware, and receiver address. The checked-in file uses H03 and the historical first receiver address `0x81` when `wssTargets` is absent. Supply `wssTargets` if your profile uses a different receiver address. Keep the channel calibration and model files together with the Core settings.

1. In the **Hierarchy**, select **Stim**. In its **Stimulation** component, set **Transport Type** to **Serial**. Clear **Auto** and enter the device port in **Device Name**. On Windows, use the COM number from **Device Manager → Ports (COM & LPT)**. If you use the in-game serial port picker, select the WSS port rather than the separate accessory port.
2. For BLE, use **BLE** in the same Inspector. Clear **Auto** and enter `name:` followed by the exact advertised NUS device name. Use `id:` and the local identifier if names are shared. Enable Bluetooth and grant access to the application if prompted. The wrapper offers BLE on Windows and macOS Mono. The [desktop BLE notes](#desktop-ble-preparation) cover the macOS requirements and validation limits.
3. For a rehearsal without a device, choose **Test** in the Inspector before entering Play mode. Restore the previous scene settings when done.

### Confirm output and stop

1. Click **Play** and focus the **Game** tab. Wait for **Setting Up...** to change to **Ready** followed by the model name. If the device still needs to be started, use **Start Stim**. Setup can start streaming automatically.
2. In the **AmpControl** panel, the left and right arrows select a logical
   channel. Confirm the panel shows the channel specified by the approved bench
   procedure. AmpControl sends direct requests outside the normalized
   calibration in `stimParams.json`. Use the exact action and limits specified
   by your approved bench procedure for this panel. The **Up Arrow** adjusts
   and sends output while held, and releasing it clears the request. Use it
   only if the procedure calls for it. The **Down Arrow** can wrap from zero
   pulse width to the panel's maximum, so it is excluded from this walkthrough.
   Confirm measured output and check that the other channels remain idle.
3. Click **Stop Stim** or, with the **Game** tab focused and **AmpControl** active, press **Esc**. Verify output has ceased, then leave Play mode.

If setup stays at **Setting Up...**, check the Unity Console for device, port, and setup errors. Compare the active firmware and receiver settings with your bench profile. The Game view's labels report application state. The shared bench procedure establishes measured device output.

### Standalone Player

To build the desktop Player, open **File → Build Settings**, select the desktop target, check that `Assets/Scenes/Console.unity` is enabled, and select **Build**. Set and verify the transport in the scene before building. The Player uses the `StreamingAssets` directory packaged with that build. Use the built Player's log for troubleshooting. The Editor and Player need separate validation on the intended OS and transport.

## Desktop BLE preparation

The existing `WindowsBleTransportSmokeTest.VerifyInstallation` entry point keeps its serialized type name and also supports macOS Mono. It registers twice, constructs/disposes without connecting, and verifies the native ABI without scanning or writing. It has no automatic Start/OnEnable behavior. The disposable `tools/UnityConformance/Prepare-Harness.ps1` project includes canonical command tests, real PluginImporter checks, and this explicit load-only diagnostic; it never loads the application's scenes or StreamingAssets configuration.

Use the corresponding reviewed wrapper feature revision when preparing that disposable project. The recorded application gitlink remains authoritative for ordinary builds; any update requires reviewed application adoption, never silent replacement with a branch checkout. The optional fake BLE lifetime host is test-only and remains compatible with the existing default emulator host.

For an actual Mac Player with the updated wrapper, select Mono, macOS 11 or newer, a compatible arm64/x86_64 architecture, and an appropriate Bluetooth Usage Description in [Player Settings](https://docs.unity3d.com/2022.3/Documentation/Manual/PlayerSettings-macOS.html). The existing project's deployment target and usage description are intentionally not rewritten. Keep operator configuration, including a custom stimulation target such as `0x80`, unchanged. Windows tests, fake transports and .NET macOS ABI tests do not establish native macOS Unity Player, IL2CPP or physical BLE operation.
Dev release acceptance uses `release-receipt.yml` with the exact Core producer run
and frozen plan ID. It emits a read-only receipt after the human-reviewed bot
merge, paired preflights, candidate-byte/projection checks and fresh final-merge
CI. GUI acceptance additionally verifies the actual wrapper gitlink and its
post-merge preflight. Main supplies registered trusted tooling; stable releases
remain disabled. Coverage describes the .NET harness, not Unity Editor/Player,
IL2CPP or physical BLE validation.

## Contributor setup

UnityYAMLMerge and optional Git hooks help when contributing scene or prefab
changes. Git LFS and the pinned integration submodule are the Git prerequisites
for opening the application. For team merge setup, see [Unity Smart Merge](https://docs.unity3d.com/2022.3/Documentation/Manual/SmartMerge.html).

## CI checks

Pull requests and pushes to `dev` and `main` expose `GUI general conformance` and separate `GUI platform compatibility (Windows)`, `GUI platform compatibility (Ubuntu)`, and `GUI platform compatibility (macOS)` contexts, matching the C# CLI names with CLI changed to GUI. The platform contexts are direct jobs without a reusable-workflow prefix.

The jobs use the exact committed wrapper gitlink and pinned C# fixture. They run the application's production conformance forwarding methods and shared wrapper command/stack source on .NET 9, then verify package build/publish propagation and the wrapper's production transport selector. This is not Unity engine, scene/UI, Player/Editor, Mono/IL2CPP, or hardware coverage. Windows BLE is load-only. The updated wrapper supports macOS Mono routing and macOS-only plugin importers; Ubuntu Unity BLE and macOS IL2CPP remain explicitly unsupported. OS Serial runtime selection applies to the .NET probe, not proof of Unity Serial support. Licensed Unity tests remain separate.

The wrapper gitlink records the application's runtime dependency. Immutable reusable-workflow references select validation tooling; they do not replace that gitlink. The GUI must never silently substitute an uncommitted or newer branch checkout for the recorded dependency. Adopt an updated wrapper through the reviewed application phase only after the integration libraries accept the same final Core candidate.
