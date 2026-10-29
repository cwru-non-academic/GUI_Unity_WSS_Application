# WSS Unity GUI

A Unity desktop application for configuring and controlling the Wearable Stimulation System. It uses the [Unity integration library](https://github.com/cwru-non-academic/Unity_WSS_Integration_Lib#readme).

## Prerequisites and checkout

Use Unity Hub with **Unity 2022.3.62f3** (see [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt)), Git and Git LFS. The project includes its Unity package dependencies in [Packages/manifest.json](Packages/manifest.json).

```bash
git clone --recurse-submodules https://github.com/cwru-non-academic/GUI_Unity_WSS_Application.git
cd GUI_Unity_WSS_Application
git lfs pull
```

The recorded submodule URL uses SSH. If you use HTTPS without an SSH key, clone without `--recurse-submodules`, enter the repository, and initialize the public submodule with a command-local URL override:

```bash
git -c "url.https://github.com/.insteadOf=git@github.com:" submodule update --init --recursive
```

For an existing clone, run `git submodule update --init --recursive` (or the HTTPS command above) before opening Unity. Add this repository's root folder in Unity Hub, then open it with the pinned editor. Allow package resolution and script import to finish. Keep the wrapper's bundled DLLs and `.meta` files together; do not add duplicate Core assemblies.

## Run without hardware

1. Open **Assets/Scenes/Console.unity**. This is the application's startup scene.
2. Before pressing Play, find the scene's **Stimulation** component and set **Transport Type** to **Test**. The Test transport simulates WSS without opening serial or BLE connections.
3. Disable the scene's **portScannerMUIP** components for this run so the serial-selection UI does not deactivate or reselect the stimulation component. Ensure the GameObject containing **Stimulation** is active.
4. Set **Test Mode** on any **CrabSerialCNTR** component to true, or keep that component disabled. This is a separate optional serial-input source and has its own transport setting.
5. Press **Play** and check the Unity Console for initialization errors. The UI reports readiness after setup. Stop Play mode to end the session.

The scene's saved device settings are not a simulator preset; set both the WSS and optional input-source settings before Play.

## Configuration and controls

The app reads configuration from [Assets/StreamingAssets](Assets/StreamingAssets):

- `stimConfig.json`: WSS unit count, firmware and Core settings.
- `stimParams.json`: channel limits, defaults and amplitude mode.
- `modelConfig.json`: model/controller settings.
- `data/`: input sequences used by the file-driven workflow.

Start with these examples and keep a separate configuration set for each experiment. See the [shared configuration reference](https://cwru-non-academic.github.io/WSS_Documentation/) for field meanings and setup behavior.

In the simulator, explore the scene's on-screen controls first. The keyboard handlers in `MinMax` use **Space** for a test input, **Up/Down** to adjust stimulation, **Left/Right** to select the bound, and **Escape** or **F8** to stop. Releasing Space or an adjustment key zeroes the corresponding input. File input and external serial input are separate workflows selected by the application UI.

## Run with a device

Stop Play mode, review the configuration, and change the **Stimulation** transport to **Serial** or **BLE**. Choose **Auto** or provide a manual **Device Name** (serial port, BLE advertised name, or `id:<identifier>`). Only enable the optional Crab serial input when that input source is intentionally in use.

Enabling `Stimulation` begins connection and device setup. Confirm the application reports readiness before operating its controls. End the session by stopping Play mode or closing the built application; the wrapper handles shutdown through Unity's lifecycle.

For a desktop build, use **File > Build Settings**, include **Assets/Scenes/Console.unity**, select the desktop target and build. Retain the generated application data directory, including `StreamingAssets`, alongside the executable. Use the [shared platform guidance](https://cwru-non-academic.github.io/WSS_Documentation/) for host transport requirements.

## Learn more

- [WSS documentation: architecture, configuration, platform support and API](https://cwru-non-academic.github.io/WSS_Documentation/)
- [Unity integration: add WSS to another Unity project](https://github.com/cwru-non-academic/Unity_WSS_Integration_Lib#readme)
- [Core source and releases](https://github.com/cwru-non-academic/WSSCore)
