# Unity Project Setup (PLEASE READ)

## CI checks

Pull requests and pushes to `dev` and `main` expose `GUI general conformance` and separate `GUI platform compatibility (Windows)`, `GUI platform compatibility (Ubuntu)`, and `GUI platform compatibility (macOS)` contexts, matching the C# CLI names with CLI changed to GUI. The platform contexts are direct jobs without a reusable-workflow prefix.

The jobs use the exact committed wrapper gitlink and pinned C# fixture. They run the application's production conformance forwarding methods and shared wrapper command/stack source on .NET 9, then verify package build/publish propagation and the wrapper's production transport selector. This is not Unity engine, scene/UI, Player/Editor, Mono/IL2CPP, or hardware coverage. Windows BLE is load-only. The updated wrapper supports macOS Mono routing and macOS-only plugin importers; Ubuntu Unity BLE and macOS IL2CPP remain explicitly unsupported. OS Serial runtime selection applies to the .NET probe, not proof of Unity Serial support. Licensed Unity tests remain separate.

The wrapper gitlink records the application's runtime dependency. Immutable reusable-workflow references select validation tooling; they do not replace that gitlink. The GUI must never silently substitute an uncommitted or newer branch checkout for the recorded dependency. Adopt an updated wrapper through the reviewed application phase only after the integration libraries accept the same final Core candidate.

Before you clone the repo, follow these instructions:

1. Install GitHub Desktop, which also installs Git LFS: https://desktop.github.com (if you are using git via command line, install Git LFS: https://git-lfs.com)
2. (for GitHub Desktop) Goto the repo website, click the green Code button, and select “Open with GitHub Desktop” and clone the template project to your local machine
(for git command line) Go to GitHub account settings and authorize your SSH key for CWRU SSO URL (https://docs.github.com/en/enterprise-cloud@latest/authentication/authenticating-with-saml-single-sign-on/authorizing-an-ssh-key-for-use-with-saml-single-sign-on) and then clone the final project template

## 5. Modify your GLOBAL .gitconfig
Not everyone may have the same path to UnityYAMLMerge (i.e. different operating systems or install locations). Because of this, we suggest you modify your local config to define the "unityyamlmerge" merge tool that this repository points to. To do this:

i. Reveal hidden files on your operating system
- Windows 10: Open the File Explorer application and select View > Options > Change folder and search options and then select the View tab and, in Advanced settings, select Show hidden files, folders, and drives and OK
- Windows 11: Open the File Explorer application and select View > Show > Hidden Items
- MacOS: Click on Finder and press the keyboard combination Shift + Cmd + . (period key)

ii. Open the `.git/config` file by navigating to your local repo folder, open the hidden `.git` folder, then open the config file (note: this is different from the `.gitconfig` file in the repo’s root folder)

iii. Identify your version of unity (e.g. `2022.3.19f1`). This will replace the word `VERSION` in the paths commented below depending on your OS. Then, add the following text to the bottom of the file, replacing `<path to UnityYAMLMerge>` with the one specific to your OS and Unity version. For instance, in Unity version 2022.3.19f1 on Windows, the last line should be `cmd = 'C:\\Program Files\\Unity\\Hub\\Editor\\2022.3.19f1\\Editor\\Data\\Tools\\UnityYAMLMerge.exe' merge -p "$BASE" "$REMOTE" "$LOCAL" "$MERGED"`
```bash
[mergetool "unityyamlmerge"]
    trustExitCode = false
    #Replace <path to UnityYAMLMerge> in the next line with the following default locations (may be different depending on your Unity installation location)
    # Installs using the Unity Hub (Default):
    # Win: C:\\Program Files\\Unity\\Hub\\Editor\\VERSION\\Editor\\Data\\Tools\\UnityYAMLMerge.exe
    # MacOS: /Applications/Unity/Hub/Editor/VERSION/Unity.app/Contents/Tools/UnityYAMLMerge
    # Linux: /home/USERNAME/Unity/Hub/Editor/VERSION/Editor/Data/Tools/UnityYAMLMerge
    cmd = '<path to UnityYAMLMerge>' merge -p "$BASE" "$REMOTE" "$LOCAL" "$MERGED"
```
iv. Change the Git Config settings to use your local Git config in the menu bar Repository > Repository Settings > Git Config > Use a local Git config > Save.

## 6. Add pre-commit and post-merge scripts
Download the scripts from the link below and paste them into the hidden folder `<your_repo>/.git/hooks/`
- Pre-commit: https://github.com/NYUGameCenter/Unity-Git-Config/blob/master/pre-commit
- Post-commit: https://github.com/NYUGameCenter/Unity-Git-Config/blob/master/post-merge
On MacOS and Linux, the pre-commit and post-merge files need to be made executable. This involves using terminal (Terminal.app on MacOS) to modify the file permissions for these two files using the following commands.
```bash
> cd <local_repo_folder>/.git/hooks
> chmod 755 pre-commit
> chmod 755 post-merge
```
## 7. Open the Project in Unity
This project was created using `2021.3.0f1`. When you open this project in Unity, Unity may say that it needs to upgrade the project. Given this is a bare-bones project, this is a safe action and you may allow Unity to continue. Overall, your entire team should be using the **same version of Unity**.

## 8. Start creating your new project and commit/push changes to the team remote repo

## Video tutorials
1. Github setup: https://youtu.be/iWniKp_O21o?feature=shared
2. Merge conflict resolution: https://youtu.be/vJIjV-GlGFg?feature=shared

## Starting a new project or adding version control to an old one?
If you are starting a new project you can start working now, but if you are adding version control to an old one follow these steps:
1. Paste your project files into this repository on your local machine. 
2. Open the project on the target Unity version, so all the files are modified as necessary. 
3. If you had Unity Version Control (formerly Collab or Plastic SCM), disable it now.
4. Save your project and close Unity.
5. Delete left over files from Unity Version Control like x.meta.private or y.meta.private.meta (some files may be hidden so enable see hidden files)
6. Commit and push.
7. If you are having issues with .meta files check and modify your gitignore or make sure there are no empty folders.

## 9. Cloning the Repository with Submodules (Terminal)
```bash
git clone --recursive git@github.com:cwru-non-academic/SimpleWSSStimConsole.git
```

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
