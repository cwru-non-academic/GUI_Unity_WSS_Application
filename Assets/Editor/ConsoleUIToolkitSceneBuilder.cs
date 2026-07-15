using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;

public static class ConsoleUIToolkitSceneBuilder
{
    private const string SourceScene = "Assets/Scenes/Console.unity";
    private const string TargetScene = "Assets/Scenes/ConsoleUIToolkit.unity";
    private const string UxmlPath = "Assets/UI/ConsoleUIToolkit.uxml";
    private const string PanelSettingsPath = "Assets/UI/ConsolePanelSettings.asset";
    private static bool smokeStarted;
    private static bool smokeFailed;
    private static int smokeStage;
    private static double stageStartedAt;
    private static ConsoleUIToolkitController smokeController;
    private static int profileFailureCallCount;
    private static bool previousEnterPlayModeOptionsEnabled;
    private static EnterPlayModeOptions previousEnterPlayModeOptions;

    [InitializeOnLoadMethod]
    private static void BuildOnceAfterImport()
    {
        if (Application.isBatchMode || AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScene) != null) return;
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            try { Build(); }
            catch (System.Exception exception) { Debug.LogException(exception); }
        };
    }

    [MenuItem("WSS/Build UI Toolkit Console Scene")]
    public static void Build()
    {
        AssetDatabase.Refresh();
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScene) != null) AssetDatabase.DeleteAsset(TargetScene);
        if (!AssetDatabase.CopyAsset(SourceScene, TargetScene))
            throw new System.InvalidOperationException("Could not copy the source scene to " + TargetScene);
        var scene = EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Additive);
        var stimulation = FindInScene<Stimulation>(scene);
        var amplitude = FindInScene<testStim>(scene);
        var minMax = FindInScene<MinMax>(scene);
        var waveform = FindInScene<WaveMaker>(scene);
        var eventReader = FindInScene<fileReader>(scene);
        var responseRecorder = FindInScene<AB_ResponseRecorder>(scene);
        var crabSerial = FindInScene<CrabSerialCNTR>(scene);

        if (stimulation == null || amplitude == null || minMax == null || waveform == null || eventReader == null || responseRecorder == null || crabSerial == null)
            throw new MissingReferenceException("The source Console scene is missing one or more required WSS controllers.");

        EnsureActiveHierarchy(amplitude.transform);
        EnsureActiveHierarchy(minMax.transform);
        EnsureActiveHierarchy(waveform.transform);
        EnsureActiveHierarchy(stimulation.transform);

        foreach (var canvas in FindAllInScene<Canvas>(scene)) canvas.enabled = false;
        foreach (var raycaster in FindAllInScene<GraphicRaycaster>(scene)) raycaster.enabled = false;
        foreach (var eventSystem in FindAllInScene<EventSystem>(scene)) eventSystem.enabled = false;

        var profile = FindInScene<PulseWidthProfileController>(scene);
        if (profile == null)
        {
            var profileObject = new GameObject("Pulse Width Profile");
            SceneManager.MoveGameObjectToScene(profileObject, scene);
            profile = profileObject.AddComponent<PulseWidthProfileController>();
        }
        Assign(profile, "stimulation", stimulation);
        Assign(amplitude, "pulseWidthProfiles", profile);
        Assign(minMax, "pulseWidthProfiles", profile);

        var existing = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "UI Toolkit Console");
        if (existing != null) Object.DestroyImmediate(existing);
        var uiObject = new GameObject("UI Toolkit Console");
        SceneManager.MoveGameObjectToScene(uiObject, scene);
        var commandQueue = uiObject.AddComponent<DeviceCommandQueue>();
        var document = uiObject.AddComponent<UIDocument>();
        document.panelSettings = LoadOrCreatePanelSettings();
        document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
        document.sortingOrder = 100;
        var controller = uiObject.AddComponent<ConsoleUIToolkitController>();
        Assign(controller, "document", document);
        Assign(controller, "stimulation", stimulation);
        Assign(controller, "amplitudeController", amplitude);
        Assign(controller, "minMaxController", minMax);
        Assign(controller, "waveformController", waveform);
        Assign(controller, "eventReader", eventReader);
        Assign(controller, "responseRecorder", responseRecorder);
        Assign(controller, "crabSerial", crabSerial);
        Assign(controller, "pulseWidthProfiles", profile);
        Assign(controller, "commandQueue", commandQueue);
        Assign(profile, "commandQueue", commandQueue);

        amplitude.enabled = true;
        minMax.enabled = false;
        waveform.enabled = false;
        eventReader.enabled = true;

        if (!EditorSceneManager.SaveScene(scene, TargetScene))
            throw new System.InvalidOperationException("Unity could not save " + TargetScene);

        AddToBuildSettings();
        AssetDatabase.SaveAssets();
        EditorSceneManager.CloseScene(scene, true);
        Debug.Log("Created UI Toolkit console scene at " + TargetScene);
    }

    [MenuItem("WSS/Validate UI Toolkit Console Scene")]
    public static void Validate()
    {
        AssetDatabase.Refresh();
        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScene);
        Require(sceneAsset != null, "Generated scene is missing.");
        var scene = EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Additive);
        try
        {
            var document = FindInScene<UIDocument>(scene);
            var controller = FindInScene<ConsoleUIToolkitController>(scene);
            var profile = FindInScene<PulseWidthProfileController>(scene);
            Require(document != null, "UIDocument is missing.");
            Require(controller != null, "ConsoleUIToolkitController is missing.");
            Require(profile != null, "PulseWidthProfileController is missing.");
            Require(document.visualTreeAsset != null, "UIDocument has no UXML asset.");
            Require(document.panelSettings != null, "UIDocument has no PanelSettings asset.");
            Require(FindAllInScene<Canvas>(scene).All(canvas => !canvas.enabled), "A legacy Canvas is still enabled.");
            Require(FindAllInScene<GraphicRaycaster>(scene).All(raycaster => !raycaster.enabled), "A legacy GraphicRaycaster is still enabled.");

            ValidateReference(controller, "document");
            ValidateReference(controller, "stimulation");
            ValidateReference(controller, "amplitudeController");
            ValidateReference(controller, "minMaxController");
            ValidateReference(controller, "waveformController");
            ValidateReference(controller, "eventReader");
            ValidateReference(controller, "responseRecorder");
            ValidateReference(controller, "crabSerial");
            ValidateReference(controller, "pulseWidthProfiles");
            ValidateReference(controller, "commandQueue");
            ValidateReference(FindInScene<testStim>(scene), "pulseWidthProfiles");
            ValidateReference(FindInScene<MinMax>(scene), "pulseWidthProfiles");
            ValidateReference(profile, "stimulation");
            ValidateReference(profile, "commandQueue");

            var tree = document.visualTreeAsset.CloneTree();
            string[] requiredElements =
            {
                "app", "ready-label", "max-pulse-dropdown", "ratio-dropdown", "amplitude-panel",
                "minmax-panel", "waveform-panel", "wave-plot", "start-button", "stop-button", "zero-button",
                "load-event-list-button", "response-a-button", "response-b-button", "stim-state",
                "system-state-label", "system-command-label", "sidebar-scroll"
            };
            foreach (string elementName in requiredElements)
                Require(tree.Q(elementName) != null, "UXML element is missing: " + elementName);
            Require(EditorBuildSettings.scenes.Any(item => item.enabled && item.path == TargetScene), "Generated scene is not enabled in Build Settings.");
            Debug.Log("UI TOOLKIT CONSOLE VALIDATION PASSED");
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    public static void RunPlayModeSmokeTest()
    {
        Validate();
        smokeStarted = true;
        smokeFailed = false;
        smokeStage = 0;
        smokeController = null;
        stageStartedAt = EditorApplication.timeSinceStartup;
        Application.logMessageReceived += OnSmokeLog;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += OnSmokeUpdate;
        var scene = EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Single);
        var stimulation = FindInScene<Stimulation>(scene);
        var stimulationSettings = new SerializedObject(stimulation);
        stimulationSettings.FindProperty("testMode").boolValue = true;
        stimulationSettings.ApplyModifiedPropertiesWithoutUndo();
        previousEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        previousEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = previousEnterPlayModeOptions | EnterPlayModeOptions.DisableDomainReload;
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            stageStartedAt = EditorApplication.timeSinceStartup;
        }
        else if (state == PlayModeStateChange.EnteredEditMode && smokeStarted)
        {
            smokeStarted = false;
            Application.logMessageReceived -= OnSmokeLog;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.update -= OnSmokeUpdate;
            EditorSettings.enterPlayModeOptionsEnabled = previousEnterPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptions = previousEnterPlayModeOptions;
            Debug.Log(smokeFailed ? "UI TOOLKIT PLAY MODE SMOKE TEST FAILED" : "UI TOOLKIT PLAY MODE SMOKE TEST PASSED");
            EditorApplication.Exit(smokeFailed ? 1 : 0);
        }
    }

    private static void OnSmokeUpdate()
    {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup - stageStartedAt < 0.5) return;
        try
        {
            if (smokeController == null) smokeController = Object.FindObjectOfType<ConsoleUIToolkitController>();
            Require(smokeController != null, "Runtime UI controller was not created.");
            var document = smokeController.GetComponent<UIDocument>();
            Require(document != null && document.rootVisualElement != null, "Runtime visual tree was not created.");
            var root = document.rootVisualElement;
            Require(root.Q<DropdownField>("max-pulse-dropdown") != null, "Maximum pulse-width dropdown did not instantiate.");
            Require(root.Q<DropdownField>("ratio-dropdown") != null, "Ratio dropdown did not instantiate.");
            Require(root.Q<DropdownField>("max-pulse-dropdown").enabledSelf, "Maximum pulse-width dropdown is unexpectedly disabled.");
            Require(root.Q<DropdownField>("ratio-dropdown").enabledSelf, "Ratio dropdown is unexpectedly disabled.");

            if (smokeStage == 0)
            {
                Require(!root.Q("amplitude-panel").ClassListContains("hidden"), "Amplitude panel is not initially visible.");
                var queue = smokeController.GetComponent<DeviceCommandQueue>();
                Require(queue != null && queue.PendingCount == 0, "Device command queue did not start idle.");
                var focusedStartButton = root.Q<UnityEngine.UIElements.Button>("start-button");
                focusedStartButton.Focus();
                var keyDown = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Space });
                focusedStartButton.SendEvent(keyDown);
                Require(keyDown.isDefaultPrevented, "Focused Start button did not reserve Space for momentary output.");
                keyDown.Dispose();
                var keyUp = KeyUpEvent.GetPooled(new Event { type = EventType.KeyUp, keyCode = KeyCode.Space });
                focusedStartButton.SendEvent(keyUp);
                keyUp.Dispose();
                Require(queue.PendingCount == 0, "Focused Start button consumed Space and queued a slow command.");

                var waveFileField = new TextField();
                root.panel.visualTree.Add(waveFileField);
                waveFileField.Focus();
                var textSpace = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Space });
                waveFileField.SendEvent(textSpace);
                Require(!textSpace.isDefaultPrevented, "Space was incorrectly suppressed inside a text field.");
                textSpace.Dispose();
                waveFileField.RemoveFromHierarchy();

                var popupProbe = new VisualElement { focusable = true };
                popupProbe.style.position = Position.Absolute;
                root.panel.visualTree.Add(popupProbe);
                var popupSpace = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Space });
                popupProbe.SendEvent(popupSpace);
                Require(popupSpace.isDefaultPrevented, "Panel-level Space interception did not cover popup content.");
                popupSpace.Dispose();
                popupProbe.RemoveFromHierarchy();

                float inactiveHeight = root.Q("stim-state").resolvedStyle.height;
                smokeController.BeginMomentaryOutput();
                smokeController.RefreshNow();
                Require(root.Q("stim-state").ClassListContains("stim-state--active"), "Momentary output indicator did not turn green/active.");
                Require(root.Q<Label>("stim-state-label").text == "STIMULATION ACTIVE", "Momentary output label did not activate.");
                Require(Mathf.Abs(root.Q("stim-state").resolvedStyle.height - inactiveHeight) < 0.1f,
                    "Active stimulation indicator changed layout height.");
                Require(inactiveHeight > 0f && inactiveHeight <= 64f,
                    $"Stimulation indicator is not compact after panel scaling ({inactiveHeight:0.0}px).");
                Require(queue.PendingCount == 0, "Momentary Space behavior incorrectly entered the slow command queue.");
                smokeController.EndMomentaryOutput();
                smokeController.RefreshNow();
                Require(!root.Q("stim-state").ClassListContains("stim-state--active"), "Momentary output indicator did not turn off after release.");
                smokeController.SelectMinMaxMode();
                smokeStage = 1;
                stageStartedAt = EditorApplication.timeSinceStartup;
            }
            else if (smokeStage == 1)
            {
                Require(!root.Q("minmax-panel").ClassListContains("hidden"), "Min/max mode did not activate.");
                smokeController.SelectWaveformMode();
                smokeStage = 2;
                stageStartedAt = EditorApplication.timeSinceStartup;
            }
            else
            {
                if (smokeStage == 2)
                {
                    Require(!root.Q("waveform-panel").ClassListContains("hidden"), "Waveform mode did not activate.");
                    smokeController.SelectAmplitudeMode();
                    var profile = Object.FindObjectOfType<PulseWidthProfileController>();
                    Require(profile != null, "Pulse-width profile controller is missing in Play Mode.");
                    profile.RequestMaximumPulseWidth(500);
                    smokeStage = 3;
                    stageStartedAt = EditorApplication.timeSinceStartup;
                }
                else if (smokeStage == 3)
                {
                    var profile = Object.FindObjectOfType<PulseWidthProfileController>();
                    if (profile.State == PulseWidthProfileController.ProfileState.Pending)
                    {
                        Require(EditorApplication.timeSinceStartup - stageStartedAt < 10.0, "Pulse-width profile update timed out.");
                        return;
                    }
                    Require(profile.State != PulseWidthProfileController.ProfileState.Failed &&
                            profile.State != PulseWidthProfileController.ProfileState.Unknown,
                        "Pulse-width profile failed: " + profile.LastError);
                    Require(profile.ActiveMaximumPulseWidth == 500 && profile.ActiveRatio == 2,
                        "Pulse-width profile did not commit the compatible 500 us / 1:2 values.");
                    profileFailureCallCount = 0;
                    profile.SetRatioUpdateHandlerForTesting((eventId, ratio) =>
                    {
                        profileFailureCallCount++;
                        return eventId == 1 ? Task.FromResult("ACK") : CanceledTask<string>();
                    });
                    smokeController.BeginMomentaryOutput();
                    profile.RequestMaximumPulseWidth(1000);
                    smokeStage = 4;
                    stageStartedAt = EditorApplication.timeSinceStartup;
                }
                else if (smokeStage == 4)
                {
                    var profile = Object.FindObjectOfType<PulseWidthProfileController>();
                    if (profile.State == PulseWidthProfileController.ProfileState.Pending) return;
                    Require(profile.State == PulseWidthProfileController.ProfileState.Unknown, "Partial profile failure did not mark hardware state unknown.");
                    Require(profile.AcknowledgedEventIds.SequenceEqual(new[] { 1 }), "Partial profile failure did not retain acknowledged event IDs.");
                    Require(profile.FailedEventId == 2, "Partial profile failure did not identify event 2.");
                    Require(profileFailureCallCount == 2, "Canceled profile command was automatically retried.");
                    Require(profile.LastError.Contains("2-second"), "Canceled profile command did not explain the DLL acknowledgement window.");
                    smokeController.RefreshNow();
                    Require(!root.Q("stim-state").ClassListContains("stim-state--active"), "Profile failure did not immediately zero output.");
                    Require(root.Q<Label>("profile-status").text.Contains("HARDWARE PROFILE UNKNOWN"), "Unknown profile state was not shown in the UI.");

                    profileFailureCallCount = 0;
                    profile.SetRatioUpdateHandlerForTesting((eventId, ratio) =>
                    {
                        profileFailureCallCount++;
                        return CanceledTask<string>();
                    });
                    profile.RequestMaximumPulseWidth(1000);
                    smokeStage = 5;
                    stageStartedAt = EditorApplication.timeSinceStartup;
                }
                else if (smokeStage == 5)
                {
                    var profile = Object.FindObjectOfType<PulseWidthProfileController>();
                    if (profile.State == PulseWidthProfileController.ProfileState.Pending) return;
                    Require(profile.State == PulseWidthProfileController.ProfileState.Unknown, "First-event timeout did not mark hardware state unknown.");
                    Require(profile.AcknowledgedEventIds.Count == 0 && profile.FailedEventId == 1, "First-event timeout diagnostics were incorrect.");
                    Require(profileFailureCallCount == 1, "First-event timeout was automatically retried.");

                    profileFailureCallCount = 0;
                    profile.SetRatioUpdateHandlerForTesting((eventId, ratio) =>
                    {
                        profileFailureCallCount++;
                        return Task.FromResult("NACK: rejected");
                    });
                    profile.RequestMaximumPulseWidth(1000);
                    smokeStage = 6;
                    stageStartedAt = EditorApplication.timeSinceStartup;
                }
                else
                {
                    var profile = Object.FindObjectOfType<PulseWidthProfileController>();
                    if (profile.State == PulseWidthProfileController.ProfileState.Pending) return;
                    Require(profile.State == PulseWidthProfileController.ProfileState.Unknown, "NACK did not mark hardware state unknown.");
                    Require(profile.FailedEventId == 1 && profile.LastError.Contains("NACK"), "NACK diagnostics were incorrect.");
                    Require(profileFailureCallCount == 1, "NACK profile command was automatically retried.");
                    profile.SetRatioUpdateHandlerForTesting(null);
                    EditorApplication.ExitPlaymode();
                }
            }
        }
        catch (System.Exception exception)
        {
            smokeFailed = true;
            Debug.LogException(exception);
            EditorApplication.ExitPlaymode();
        }
    }

    private static void OnSmokeLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error && condition.StartsWith("Pulse-width profile state unknown:")) return;
        if (type == LogType.Exception || type == LogType.Assert || type == LogType.Error) smokeFailed = true;
    }

    private static Task<T> CanceledTask<T>()
    {
        var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        return Task.FromCanceled<T>(cancellation.Token);
    }

    private static PanelSettings LoadOrCreatePanelSettings()
    {
        var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
        if (settings != null) return settings;
        settings = ScriptableObject.CreateInstance<PanelSettings>();
        settings.name = "Console Panel Settings";
        settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        settings.referenceResolution = new Vector2Int(1440, 900);
        settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
        settings.match = 0.5f;
        AssetDatabase.CreateAsset(settings, PanelSettingsPath);
        return settings;
    }

    private static T FindInScene<T>(Scene scene) where T : Component =>
        Resources.FindObjectsOfTypeAll<T>().FirstOrDefault(component => component.gameObject.scene == scene);

    private static T[] FindAllInScene<T>(Scene scene) where T : Component =>
        Resources.FindObjectsOfTypeAll<T>().Where(component => component.gameObject.scene == scene).ToArray();

    private static void EnsureActiveHierarchy(Transform transform)
    {
        while (transform != null)
        {
            transform.gameObject.SetActive(true);
            transform = transform.parent;
        }
    }

    private static void Assign(Object target, string propertyName, Object value)
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(propertyName);
        if (property == null) throw new System.MissingFieldException(target.GetType().Name, propertyName);
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ValidateReference(Object target, string propertyName)
    {
        Require(target != null, "Reference owner is missing for " + propertyName);
        var property = new SerializedObject(target).FindProperty(propertyName);
        Require(property != null && property.objectReferenceValue != null,
            target.GetType().Name + "." + propertyName + " is not assigned.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new System.InvalidOperationException(message);
    }

    private static void AddToBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.All(scene => scene.path != TargetScene)) scenes.Add(new EditorBuildSettingsScene(TargetScene, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
