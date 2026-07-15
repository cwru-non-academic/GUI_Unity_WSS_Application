using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class ConsoleUIToolkitController : MonoBehaviour
{
    private enum ConsoleMode { Amplitude, MinMax, Waveform }

    [SerializeField] private UIDocument document;
    [SerializeField] private Stimulation stimulation;
    [SerializeField] private testStim amplitudeController;
    [SerializeField] private MinMax minMaxController;
    [SerializeField] private WaveMaker waveformController;
    [SerializeField] private fileReader eventReader;
    [SerializeField] private AB_ResponseRecorder responseRecorder;
    [SerializeField] private CrabSerialCNTR crabSerial;
    [SerializeField] private PulseWidthProfileController pulseWidthProfiles;
    [SerializeField] private DeviceCommandQueue commandQueue;

    private readonly List<VisualElement> waveDots = new List<VisualElement>();
    private VisualElement root;
    private VisualElement amplitudePanel;
    private VisualElement minMaxPanel;
    private VisualElement waveformPanel;
    private VisualElement eventControls;
    private VisualElement wavePlot;
    private Label readyLabel;
    private Label profileStatus;
    private Label toast;
    private DropdownField maximumDropdown;
    private DropdownField ratioDropdown;
    private DropdownField inputDropdown;
    private DropdownField portDropdown;
    private DropdownField pointDropdown;
    private VisualElement panelKeyboardRoot;
    private ConsoleMode mode;
    private bool suppressProfileCallbacks;
    private bool momentaryOutputHeld;
    private bool spaceOwnedByTextField;

    private static readonly string[] ShapeNames =
    {
        "Square", "Rising ramp", "Falling ramp", "Triangle", "Sine", "Gaussian",
        "Exponential rise", "Exponential fall", "Biphasic", "Burst", "Trapezoid", "Custom"
    };

    private void OnEnable()
    {
        if (document == null) document = GetComponent<UIDocument>();
        if (commandQueue == null) commandQueue = GetComponent<DeviceCommandQueue>();
        root = document != null ? document.rootVisualElement : null;
        if (root == null)
        {
            Debug.LogError("ConsoleUIToolkitController requires a UIDocument with ConsoleUIToolkit.uxml.", this);
            enabled = false;
            return;
        }

        CacheElements();
        amplitudeController.SetUIToolkitInputManaged(true);
        minMaxController.SetUIToolkitInputManaged(true);
        waveformController.SetUIToolkitInputManaged(true);
        ConfigureFields();
        WireActions();
        root.RegisterCallback<AttachToPanelEvent>(OnRootAttachedToPanel);
        RegisterPanelKeyboardCallbacks();
        pulseWidthProfiles.HardwareStateBecameUnknown += OnProfileHardwareStateUnknown;
        BuildWavePlot();
        ShowMode(ConsoleMode.Amplitude);
        root.schedule.Execute(Refresh).Every(100);
    }

    private void CacheElements()
    {
        amplitudePanel = root.Q("amplitude-panel");
        minMaxPanel = root.Q("minmax-panel");
        waveformPanel = root.Q("waveform-panel");
        eventControls = root.Q("event-controls");
        wavePlot = root.Q("wave-plot");
        readyLabel = root.Q<Label>("ready-label");
        profileStatus = root.Q<Label>("profile-status");
        toast = root.Q<Label>("toast");
        maximumDropdown = root.Q<DropdownField>("max-pulse-dropdown");
        ratioDropdown = root.Q<DropdownField>("ratio-dropdown");
        inputDropdown = root.Q<DropdownField>("input-dropdown");
        portDropdown = root.Q<DropdownField>("port-dropdown");
        pointDropdown = root.Q<DropdownField>("point-dropdown");
    }

    private void ConfigureFields()
    {
        maximumDropdown.choices = new List<string> { "250 us", "500 us", "1000 us" };
        ratioDropdown.choices = new List<string> { "1:1", "1:2", "1:4" };
        inputDropdown.choices = new List<string> { "Serial", "File reader" };
        inputDropdown.SetValueWithoutNotify("Serial");
        minMaxController.changeInput("serial");

        var ports = SerialPort.GetPortNames().OrderBy(port => port).ToList();
        if (ports.Count == 0) ports.Add("No ports found");
        portDropdown.choices = ports;
        portDropdown.SetValueWithoutNotify(ports.Contains(stimulation.comPort) ? stimulation.comPort : ports[0]);
        root.Q<Toggle>("force-port-toggle").SetValueWithoutNotify(stimulation.forcePort);

        pointDropdown.choices = Enumerable.Range(1, 32).Select(i => i.ToString()).ToList();
        pointDropdown.SetValueWithoutNotify("1");
        var shapeDropdown = root.Q<DropdownField>("shape-dropdown");
        shapeDropdown.choices = ShapeNames.ToList();
        shapeDropdown.SetValueWithoutNotify(ShapeNames[0]);
        RestoreProfileFields();
    }

    private void WireActions()
    {
        root.Q<Button>("amplitude-tab").clicked += SelectAmplitudeMode;
        root.Q<Button>("minmax-tab").clicked += SelectMinMaxMode;
        root.Q<Button>("waveform-tab").clicked += SelectWaveformMode;

        root.Q<Button>("start-button").clicked += StartSystem;
        root.Q<Button>("stop-button").clicked += StopSystem;
        root.Q<Button>("zero-button").clicked += ZeroOutput;
        root.Q<Button>("reset-amps-button").clicked += ResetAmplitudes;
        root.Q<Button>("reset-radio-button").clicked += ResetRadio;

        maximumDropdown.RegisterValueChangedCallback(evt =>
        {
            if (suppressProfileCallbacks) return;
            pulseWidthProfiles.RequestMaximumPulseWidth(ParseLeadingInt(evt.newValue));
        });
        ratioDropdown.RegisterValueChangedCallback(evt =>
        {
            if (suppressProfileCallbacks) return;
            pulseWidthProfiles.RequestRatioValue(ParseTrailingInt(evt.newValue));
        });

        var forcePort = root.Q<Toggle>("force-port-toggle");
        forcePort.RegisterValueChangedCallback(evt => ApplyPort(evt.newValue));
        portDropdown.RegisterValueChangedCallback(_ => { if (forcePort.value) ApplyPort(true); });

        var shapeDropdown = root.Q<DropdownField>("shape-dropdown");
        shapeDropdown.RegisterValueChangedCallback(evt => amplitudeController.SelectWaveform(Array.IndexOf(ShapeNames, evt.newValue)));
        root.Q<Button>("upload-shape-button").clicked += amplitudeController.updateWaveform;

        inputDropdown.RegisterValueChangedCallback(evt =>
        {
            minMaxController.changeInput(evt.newValue == "Serial" ? "serial" : "file");
            RefreshEventVisibility();
            RefreshPortTarget();
        });
        root.Q<Button>("link-button").clicked += () => { minMaxController.link(); RefreshEventVisibility(); };
        root.Q<Button>("prev-trial-button").clicked += eventReader.PrevTrial;
        root.Q<Button>("next-trial-button").clicked += eventReader.NextTrial;
        root.Q<Button>("prev-event-button").clicked += eventReader.PrevEvent;
        root.Q<Button>("next-event-button").clicked += eventReader.NextEvent;
        root.Q<Button>("load-event-list-button").clicked += eventReader.LoadEventList;
        root.Q<Button>("start-event-button").clicked += eventReader.StartEvent;
        root.Q<Button>("stop-event-button").clicked += eventReader.StopEvent;
        root.Q<Button>("event-a-button").clicked += eventReader.EventA;
        root.Q<Button>("event-b-button").clicked += eventReader.EventB;
        root.Q<Button>("response-a-button").clicked += () => responseRecorder.logEvent("A");
        root.Q<Button>("response-b-button").clicked += () => responseRecorder.logEvent("B");

        pointDropdown.RegisterValueChangedCallback(evt => waveformController.SelectPoint(ParseLeadingInt(evt.newValue) - 1));
        root.Q<Button>("previous-point-button").clicked += () => waveformController.updatePoints(-1);
        root.Q<Button>("next-point-button").clicked += () => waveformController.updatePoints(1);
        root.Q<Button>("decrease-point-button").clicked += () => waveformController.changePointAmp(-1);
        root.Q<Button>("increase-point-button").clicked += () => waveformController.changePointAmp(1);
        root.Q<Button>("decrease-rate-button").clicked += () => waveformController.changeRate(-1);
        root.Q<Button>("increase-rate-button").clicked += () => waveformController.changeRate(1);
        root.Q<Button>("load-wave-button").clicked += () => waveformController.LoadNamed(root.Q<TextField>("wave-file-field").value);
        root.Q<Button>("save-wave-button").clicked += () => waveformController.SaveNamed(root.Q<TextField>("wave-file-field").value);
        root.Q<Button>("upload-wave-button").clicked += waveformController.upload;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            spaceOwnedByTextField = IsTextEntryFocused();
            if (!spaceOwnedByTextField) ActivateMomentaryOutput();
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            if (momentaryOutputHeld) DeactivateMomentaryOutput();
            spaceOwnedByTextField = false;
        }
        if (Input.GetKeyDown(KeyCode.Escape)) StopSystem();
    }

    private void OnDisable()
    {
        if (momentaryOutputHeld) DeactivateMomentaryOutput();
        UnregisterPanelKeyboardCallbacks();
        if (root != null) root.UnregisterCallback<AttachToPanelEvent>(OnRootAttachedToPanel);
        if (pulseWidthProfiles != null) pulseWidthProfiles.HardwareStateBecameUnknown -= OnProfileHardwareStateUnknown;
        if (amplitudeController != null) amplitudeController.SetUIToolkitInputManaged(false);
        if (minMaxController != null) minMaxController.SetUIToolkitInputManaged(false);
        if (waveformController != null) waveformController.SetUIToolkitInputManaged(false);
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused && momentaryOutputHeld) DeactivateMomentaryOutput();
    }

    private void OnRootAttachedToPanel(AttachToPanelEvent evt) => RegisterPanelKeyboardCallbacks();

    private void RegisterPanelKeyboardCallbacks()
    {
        VisualElement visualTree = root != null && root.panel != null ? root.panel.visualTree : null;
        if (visualTree == null || visualTree == panelKeyboardRoot) return;
        UnregisterPanelKeyboardCallbacks();
        panelKeyboardRoot = visualTree;
        panelKeyboardRoot.RegisterCallback<KeyDownEvent>(SuppressSpaceUIInteraction, TrickleDown.TrickleDown);
        panelKeyboardRoot.RegisterCallback<KeyUpEvent>(SuppressSpaceUIInteraction, TrickleDown.TrickleDown);
        panelKeyboardRoot.RegisterCallback<NavigationSubmitEvent>(SuppressSpaceNavigationSubmit, TrickleDown.TrickleDown);
    }

    private void UnregisterPanelKeyboardCallbacks()
    {
        if (panelKeyboardRoot == null) return;
        panelKeyboardRoot.UnregisterCallback<KeyDownEvent>(SuppressSpaceUIInteraction, TrickleDown.TrickleDown);
        panelKeyboardRoot.UnregisterCallback<KeyUpEvent>(SuppressSpaceUIInteraction, TrickleDown.TrickleDown);
        panelKeyboardRoot.UnregisterCallback<NavigationSubmitEvent>(SuppressSpaceNavigationSubmit, TrickleDown.TrickleDown);
        panelKeyboardRoot = null;
    }

    private void SuppressSpaceUIInteraction(KeyDownEvent evt)
    {
        if (evt.keyCode != KeyCode.Space || IsTextEntryFocused(evt.target as VisualElement)) return;
        evt.PreventDefault();
        evt.StopImmediatePropagation();
    }

    private void SuppressSpaceUIInteraction(KeyUpEvent evt)
    {
        if (evt.keyCode != KeyCode.Space || spaceOwnedByTextField || IsTextEntryFocused(evt.target as VisualElement)) return;
        evt.PreventDefault();
        evt.StopImmediatePropagation();
    }

    private void SuppressSpaceNavigationSubmit(NavigationSubmitEvent evt)
    {
        if (!Input.GetKey(KeyCode.Space) || spaceOwnedByTextField || IsTextEntryFocused(evt.target as VisualElement)) return;
        evt.PreventDefault();
        evt.StopImmediatePropagation();
    }

    private bool IsTextEntryFocused(VisualElement eventTarget = null)
    {
        if (IsTextEntryElement(eventTarget)) return true;
        var focused = panelKeyboardRoot != null && panelKeyboardRoot.panel != null
            ? panelKeyboardRoot.panel.focusController.focusedElement as VisualElement
            : null;
        return IsTextEntryElement(focused);
    }

    private static bool IsTextEntryElement(VisualElement element)
    {
        for (VisualElement current = element; current != null; current = current.hierarchy.parent)
        {
            if (current is TextField || current.ClassListContains("unity-text-input")) return true;
        }
        return false;
    }

    private void ShowMode(ConsoleMode nextMode)
    {
        if (momentaryOutputHeld) DeactivateMomentaryOutput();
        mode = nextMode;
        SetVisible(amplitudePanel, mode == ConsoleMode.Amplitude);
        SetVisible(minMaxPanel, mode == ConsoleMode.MinMax);
        SetVisible(waveformPanel, mode == ConsoleMode.Waveform);
        SetNavActive("amplitude-tab", mode == ConsoleMode.Amplitude);
        SetNavActive("minmax-tab", mode == ConsoleMode.MinMax);
        SetNavActive("waveform-tab", mode == ConsoleMode.Waveform);

        amplitudeController.enabled = mode == ConsoleMode.Amplitude;
        minMaxController.enabled = mode == ConsoleMode.MinMax;
        waveformController.enabled = mode == ConsoleMode.Waveform;
        if (mode != ConsoleMode.MinMax && minMaxController.Linked) minMaxController.link();
        if (mode != ConsoleMode.MinMax && eventReader != null) eventReader.StopEvent();
        RefreshPortTarget();
    }

    public void SelectAmplitudeMode() => ShowMode(ConsoleMode.Amplitude);
    public void SelectMinMaxMode() => ShowMode(ConsoleMode.MinMax);
    public void SelectWaveformMode() => ShowMode(ConsoleMode.Waveform);
    public void RefreshNow() => Refresh();
    public void BeginMomentaryOutput() => ActivateMomentaryOutput();
    public void EndMomentaryOutput() => DeactivateMomentaryOutput();

    private void StartSystem()
    {
        QueueDeviceAction("Start stimulation system", stimulation.StartStimulation, "Stimulation system started.");
    }

    private void StopSystem()
    {
        DeactivateMomentaryOutput();
        ZeroOutput(false);
        QueueDeviceAction("Stop stimulation system", stimulation.StopStimulation, "Stimulation stopped and output zeroed.");
    }

    private void ZeroOutput() => ZeroOutput(true);

    private void ZeroOutput(bool showConfirmation)
    {
        TryDeviceAction(() =>
        {
            amplitudeController.ZeroStim();
            minMaxController.ZeroStim();
            waveformController.ZeroStim();
            momentaryOutputHeld = false;
        }, showConfirmation ? "Output zeroed." : null);
    }

    private void ActivateMomentaryOutput()
    {
        if (momentaryOutputHeld) return;
        try
        {
            if (mode == ConsoleMode.Amplitude) amplitudeController.test();
            else if (mode == ConsoleMode.MinMax) minMaxController.test();
            else waveformController.TestPulse();
            momentaryOutputHeld = true;
            RefreshConnectionState();
        }
        catch (Exception exception)
        {
            momentaryOutputHeld = false;
            Debug.LogError(exception, this);
            ShowToast(exception.Message, true);
        }
    }

    private void DeactivateMomentaryOutput()
    {
        if (!momentaryOutputHeld && !IsOutputActive()) return;
        try
        {
            if (mode == ConsoleMode.Amplitude) amplitudeController.ZeroStim();
            else if (mode == ConsoleMode.MinMax) minMaxController.ZeroStim();
            else waveformController.ZeroStim();
        }
        catch (Exception exception)
        {
            Debug.LogError(exception, this);
        }
        finally
        {
            momentaryOutputHeld = false;
            if (root != null) RefreshConnectionState();
        }
    }

    private void OnProfileHardwareStateUnknown()
    {
        ZeroOutput(false);
        ShowToast("Profile acknowledgement failed. Output was zeroed; hardware profile is unknown.", true);
        RefreshProfile();
    }

    private void ResetAmplitudes()
    {
        TryDeviceAction(() =>
        {
            amplitudeController.resetAmp();
            minMaxController.resetAmp();
        }, "Amplitudes reset to safe defaults.");
    }

    private void ResetRadio()
    {
        QueueDeviceAction("Reset radio", stimulation.resetRadio, "Radio connection reset.");
    }

    private void ApplyPort(bool force)
    {
        string selected = portDropdown.value;
        if (force && selected == "No ports found")
        {
            root.Q<Toggle>("force-port-toggle").SetValueWithoutNotify(false);
            ShowToast("No serial ports are currently available.", true);
            return;
        }

        bool useInputDevice = mode == ConsoleMode.MinMax && inputDropdown.value == "Serial" && crabSerial != null;
        if (useInputDevice)
        {
            crabSerial.forcePort = force;
            if (force) crabSerial.comPort = selected;
            QueueDeviceAction("Apply input-device port", () =>
            {
                crabSerial.gameObject.SetActive(false);
                if (minMaxController.Linked) crabSerial.gameObject.SetActive(true);
            }, force ? $"Input device using {selected}." : "Automatic input-device port selection enabled.");
        }
        else
        {
            stimulation.forcePort = force;
            if (force) stimulation.comPort = selected;
            QueueDeviceAction("Apply stimulator port", () =>
            {
                stimulation.gameObject.SetActive(false);
                stimulation.gameObject.SetActive(true);
            }, force ? $"Stimulator using {selected}." : "Automatic stimulator port selection enabled.");
        }
    }

    private void TryDeviceAction(Action action, string success)
    {
        try
        {
            action();
            if (!string.IsNullOrEmpty(success)) ShowToast(success, false);
        }
        catch (Exception ex)
        {
            Debug.LogError(ex, this);
            ShowToast(ex.Message, true);
        }
    }

    private void QueueDeviceAction(string label, Action action, string success)
    {
        commandQueue.EnqueueAction(
            label,
            action,
            () =>
            {
                if (!string.IsNullOrEmpty(success)) ShowToast(success, false);
            },
            exception =>
            {
                Debug.LogError(exception, this);
                ShowToast(exception.Message, true);
            });
        RefreshCommandQueueStatus();
    }

    private void Refresh()
    {
        RefreshConnectionState();
        RefreshCommandQueueStatus();
        RefreshProfile();
        RefreshAmplitude();
        RefreshMinMax();
        RefreshWaveform();
        RefreshEventVisibility();
    }

    private void RefreshConnectionState()
    {
        bool ready = false;
        string status = "Setting up...";
        try
        {
            ready = stimulation.Ready() || stimulation.Started();
            status = ready ? "Ready" : "Setting up...";
            if (ready && stimulation.isModeValid()) status += " - " + stimulation.GetModelConfigCTRL().GetCalibMode();
        }
        catch { status = "Device unavailable"; }

        readyLabel.text = status;
        root.Q("ready-dot").EnableInClassList("status-dot--waiting", !ready);
        root.Q("ready-dot").EnableInClassList("status-dot--active", ready);
        bool started = false;
        try { started = stimulation.Started(); } catch { }
        bool outputActive = IsOutputActive();
        VisualElement stimulationState = root.Q("stim-state");
        root.Q<Label>("stim-state-label").text = outputActive ? "STIMULATION ACTIVE" : "Output inactive";
        root.Q("stim-dot").EnableInClassList("status-dot--off", !outputActive);
        root.Q("stim-dot").EnableInClassList("status-dot--active", outputActive);
        stimulationState.EnableInClassList("stim-state--active", outputActive);
        root.Q<Label>("system-state-label").text = started ? "System running" : "System stopped";
    }

    private bool IsOutputActive()
    {
        if (mode == ConsoleMode.Amplitude) return amplitudeController.OutputActive;
        if (mode == ConsoleMode.MinMax) return minMaxController.OutputActive;
        return waveformController.OutputActive;
    }

    private void RefreshCommandQueueStatus()
    {
        Label label = root.Q<Label>("system-command-label");
        if (commandQueue == null || !commandQueue.IsBusy)
        {
            label.text = "Command queue idle";
            return;
        }

        int waiting = Mathf.Max(0, commandQueue.PendingCount - 1);
        label.text = waiting > 0
            ? $"Running: {commandQueue.CurrentLabel} - {waiting} queued"
            : $"Running: {commandQueue.CurrentLabel}";
    }

    private void RefreshProfile()
    {
        bool pending = pulseWidthProfiles.State == PulseWidthProfileController.ProfileState.Pending;
        maximumDropdown.SetEnabled(!pending);
        ratioDropdown.SetEnabled(!pending);
        if (pending)
        {
            maximumDropdown.SetValueWithoutNotify(pulseWidthProfiles.PendingMaximumPulseWidth + " us");
            ratioDropdown.SetValueWithoutNotify("1:" + pulseWidthProfiles.PendingRatio);
        }
        bool unknown = pulseWidthProfiles.State == PulseWidthProfileController.ProfileState.Unknown;
        profileStatus.text = pending
            ? $"Queued/applying {pulseWidthProfiles.PendingMaximumPulseWidth} us - 1:{pulseWidthProfiles.PendingRatio}..."
            : $"Active: {pulseWidthProfiles.ActiveMaximumPulseWidth} us - 1:{pulseWidthProfiles.ActiveRatio}";
        if (unknown)
        {
            profileStatus.text = "HARDWARE PROFILE UNKNOWN - " + pulseWidthProfiles.LastError;
            SetUnknownProfileFields();
        }
        else if (pulseWidthProfiles.State == PulseWidthProfileController.ProfileState.Failed && !string.IsNullOrEmpty(pulseWidthProfiles.LastError))
            profileStatus.text = "Change failed - " + pulseWidthProfiles.LastError;
        if (!pending && !unknown) RestoreProfileFields();
    }

    private void SetUnknownProfileFields()
    {
        suppressProfileCallbacks = true;
        maximumDropdown.SetValueWithoutNotify("Unknown - select profile");
        ratioDropdown.SetValueWithoutNotify("Unknown - select ratio");
        suppressProfileCallbacks = false;
    }

    private void RestoreProfileFields()
    {
        if (pulseWidthProfiles == null) return;
        suppressProfileCallbacks = true;
        maximumDropdown.SetValueWithoutNotify(pulseWidthProfiles.ActiveMaximumPulseWidth + " us");
        ratioDropdown.SetValueWithoutNotify("1:" + pulseWidthProfiles.ActiveRatio);
        suppressProfileCallbacks = false;
    }

    private void RefreshAmplitude()
    {
        int maximum = pulseWidthProfiles.ActiveMaximumPulseWidth;
        SetChannel(1, amplitudeController.PulseWidth1, amplitudeController.Amplitude1, maximum);
        SetChannel(2, amplitudeController.PulseWidth2, amplitudeController.Amplitude2, maximum);
        SetChannel(3, amplitudeController.PulseWidth3, amplitudeController.Amplitude3, maximum);
        for (int i = 1; i <= 3; i++) root.Q("channel-" + i).EnableInClassList("metric-card--active", amplitudeController.CurrentChannel == i);
    }

    private void SetChannel(int channel, int pulseWidth, int amplitude, int maximum)
    {
        root.Q<Label>($"ch{channel}-value").text = $"{pulseWidth} µs  ·  {amplitude} mA";
        SetProgress($"ch{channel}-pw", pulseWidth, maximum, $"Pulse width · {pulseWidth}/{maximum} µs");
        SetProgress($"ch{channel}-amp", amplitude, 80, $"Amplitude · {amplitude}/80 mA");
    }

    private void RefreshMinMax()
    {
        int maximum = pulseWidthProfiles.ActiveMaximumPulseWidth;
        root.Q<Label>("max-value").text = minMaxController.MaximumPulseWidth + " µs";
        root.Q<Label>("min-value").text = minMaxController.MinimumPulseWidth + " µs";
        root.Q<Label>("minmax-amp-value").text = minMaxController.Amplitude + " mA";
        SetProgress("max-progress", minMaxController.MaximumPulseWidth, maximum, "Maximum pulse width");
        SetProgress("min-progress", minMaxController.MinimumPulseWidth, maximum, "Minimum pulse width");
        SetProgress("minmax-amp-progress", minMaxController.Amplitude, 80, "Amplitude");
        root.Q("max-card").EnableInClassList("metric-card--active", minMaxController.MaximumSelected);
        root.Q("min-card").EnableInClassList("metric-card--active", !minMaxController.MaximumSelected);
        root.Q<Button>("link-button").text = minMaxController.Linked ? "Unlink input" : "Link input";
        root.Q<Label>("link-status").text = minMaxController.Linked ? "Linked · " + inputDropdown.value : "Not linked";
        if (eventReader != null)
        {
            root.Q<Label>("trial-label").text = "Trial " + eventReader.TrialIndex;
            root.Q<Label>("event-label").text = "Event " + eventReader.EventIndex;
            root.Q<Button>("start-event-button").text = eventReader.EventRunning ? "Event running" : "Start event";
        }
    }

    private void RefreshEventVisibility()
    {
        bool visible = mode == ConsoleMode.MinMax && minMaxController.Linked && inputDropdown.value == "File reader";
        SetVisible(eventControls, visible);
        SetVisible(root.Q("response-controls"), visible && eventReader.getEventAStatus() && eventReader.getEventBStatus());
    }

    private void RefreshPortTarget()
    {
        bool inputDevice = mode == ConsoleMode.MinMax && inputDropdown.value == "Serial" && crabSerial != null;
        root.Q<Label>("port-target-label").text = inputDevice ? "INPUT DEVICE PORT" : "STIMULATOR PORT";
        root.Q<Toggle>("force-port-toggle").SetValueWithoutNotify(inputDevice ? crabSerial.forcePort : stimulation.forcePort);
        string currentPort = inputDevice ? crabSerial.comPort : stimulation.comPort;
        if (portDropdown.choices.Contains(currentPort)) portDropdown.SetValueWithoutNotify(currentPort);
    }

    private void BuildWavePlot()
    {
        if (wavePlot == null) return;
        wavePlot.Clear();
        waveDots.Clear();
        for (int i = 0; i < 32; i++)
        {
            var dot = new VisualElement();
            dot.AddToClassList("wave-dot");
            dot.style.left = Length.Percent((i + 0.5f) * 100f / 32f);
            dot.style.bottom = Length.Percent(0);
            wavePlot.Add(dot);
            waveDots.Add(dot);
        }
    }

    private void RefreshWaveform()
    {
        if (waveformController == null || waveDots.Count == 0) return;
        for (int i = 0; i < waveDots.Count; i++)
        {
            waveDots[i].style.bottom = Length.Percent(waveformController.GetPointPercent(i));
            waveDots[i].EnableInClassList("wave-dot--active", waveformController.CurrentIndex == i);
        }
        pointDropdown.SetValueWithoutNotify((waveformController.CurrentIndex + 1).ToString());
        root.Q<Label>("point-value").text = waveformController.CurrentPercent.ToString("F2") + "%";
        root.Q<Label>("rate-value").text = "Rate " + waveformController.Rate.ToString("F2") + "%";
    }

    private void SetProgress(string name, float value, float highValue, string title)
    {
        var bar = root.Q<ProgressBar>(name);
        bar.lowValue = 0;
        bar.highValue = Mathf.Max(1, highValue);
        bar.value = value;
        bar.title = title;
    }

    private void ShowToast(string message, bool error)
    {
        toast.text = message;
        toast.RemoveFromClassList("hidden");
        toast.style.borderLeftColor = error ? new Color(1f, .32f, .4f) : new Color(.15f, .78f, .92f);
        toast.schedule.Execute(() => toast.AddToClassList("hidden")).ExecuteLater(3500);
    }

    private void SetNavActive(string name, bool active) => root.Q<Button>(name).EnableInClassList("nav-button--active", active);
    private static void SetVisible(VisualElement element, bool visible) => element.EnableInClassList("hidden", !visible);
    private static int ParseLeadingInt(string value) => int.TryParse(value.Split(' ')[0], out int parsed) ? parsed : 0;
    private static int ParseTrailingInt(string value)
    {
        string[] pieces = value.Split(':');
        return int.TryParse(pieces[pieces.Length - 1], out int parsed) ? parsed : 0;
    }
}
