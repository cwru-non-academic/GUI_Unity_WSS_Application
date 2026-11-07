using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using Michsky.MUIP;
using Wss.CoreModule;
using Wss.Testing;

// Actual MinMax and Stimulation source, real Core and the existing HostLifecycle emulator.
// This fixture is not a Unity Editor, Play Mode, rendered UI or hardware test.
[TestFixture, NonParallelizable, Category("GuiSourceBoundary")]
public sealed class GuiBoundaryTests
{
    private HostReloadTests setup;
    private MinMax ui;
    private Stimulation stimulation;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [SetUp]
    public void SetUp()
    {
        setup = new HostReloadTests();
        setup.SetUp();
        Call(setup, "Start", typeof(Stimulation), false);
        stimulation = (Stimulation)Field(setup, "host");
        ui = new MinMax();
        Set(ui, "stim", stimulation);
        foreach (string name in new[] { "pwMinBar", "pwMaxBar", "AmpBar" }) Set(ui, name, new ProgressBar());
        foreach (string name in new[] { "MaxSelect", "MinSelect", "stimOn", "stimOff", "crabSerial", "fileReader", "instructions" }) Set(ui, name, new GameObject());
        Set(ui, "input", new CustomDropdown()); Set(ui, "linkBTN", new ButtonManager());
        Call(ui, "Start");
    }
    [TearDown] public void TearDown() { setup?.TearDown(); }
    private IStimulationCore Owner => (IStimulationCore)Field(stimulation, "_stackOwner");
    private IList Transports => (IList)Field(setup, "transports");
    private EmulatedWssTransport Emulator => (EmulatedWssTransport)Field(Transports[Transports.Count - 1], "Emulator");
    private void Pump(Func<bool> condition) => Call(setup, "PumpUntil", condition);
    private long Baseline => Emulator.Conformance.MessageHistory.Last().SequenceNumber;

    private void ExpectOutput(float input, int width, int amplitude)
    {
        long baseline = Baseline;
        ui.inputFloat(input);
        Pump(() => Emulator.Conformance.StimulationHistory.Any(item => item.SequenceNumber > baseline && item.Channel == 1 && item.PulseWidth == width && item.PulseAmplitude == amplitude));
        Assert.That(stimulation.GetStimIntensity("Thumb"), Is.EqualTo(width));
    }
    private void Calibrate()
    {
        ui.resetAmp();
        // Exercise the actual calibration methods, including the max-PW amplitude rollover.
        for (int i = 0; i < 252; i++) Call(ui, "increaseStim");
        for (int i = 0; i < 196; i++) Call(ui, "increaseStim");
        Set(ui, "maxOn", false);
        for (int i = 0; i < 96; i++) Call(ui, "increaseStim");
        Set(ui, "maxOn", true);
        Call(ui, "decreaseStim"); Call(ui, "increaseStim");
        Assert.That(ui.getStimParams(), Is.EqualTo(new[] { "200", "100", "2" }));
        Assert.That(((ProgressBar)Field(ui, "AmpBar")).currentPercent, Is.EqualTo(2));
        ui.changeInput("file"); ui.link();
    }

    [Test]
    public void CalibrationEditsFlowThroughInputFloatToCoreDefaultAmplitudeAndPulseWidth()
    {
        Calibrate();
        ExpectOutput(.5f, 150, 2);
        ExpectOutput(1f, 200, 2);
        Set(ui, "maxOn", false); Call(ui, "decreaseStim");
        Assert.That(ui.getStimParams(), Is.EqualTo(new[] { "200", "99", "2" }));
        ExpectOutput(1f, 200, 2);
        ui.resetAmp();
        ExpectOutput(1f, 4, 1);
    }

    [Test]
    public void UnlinkedInputDoesNotChangeAppliedOutputAndLowLinkedInputZerosIt()
    {
        Calibrate(); ExpectOutput(1f, 200, 2);
        ui.link(); ui.inputFloat(.5f);
        for (int i = 0; i < 10; i++) Call(stimulation, "Update");
        Assert.That(stimulation.GetStimIntensity("Thumb"), Is.EqualTo(200));
        ui.link(); ExpectOutput(.01f, 0, 2); ExpectOutput(-1f, 0, 2);
    }

    [Test]
    public void RadioResetReplacesCoreButRetainsAppliedCalibrationDespiteMalformedNonCoreFiles()
    {
        Calibrate(); ExpectOutput(.5f, 150, 2);
        var previous = Owner;
        string directory = (string)Field(setup, "directory");
        File.WriteAllText(Path.Combine(directory, "stimParams.json"), "invalid calibration on disk");
        File.WriteAllText(Path.Combine(directory, "modelConfig.json"), "invalid model on disk");
        Call(setup, "WriteCore", 2);
        stimulation.resetRadio();
        Pump(() => Owner != null && Owner.Started());
        Assert.That(Owner, Is.Not.SameAs(previous));
        Assert.That(Owner.GetCoreConfigController().MaxWss, Is.EqualTo(2));
        Assert.That(Transports.Count, Is.EqualTo(2));
        Assert.That((bool)Field(Transports[0], "Disposed"), Is.True);
        ExpectOutput(.5f, 150, 2);
        ExpectOutput(1f, 200, 2);
    }

    [TestCase("1203967498", "Stimulation", "resetRadio", TestName = "ConsoleSceneResetRadioBinding")]
    [TestCase("1889387618", "MinMax", "resetAmp", TestName = "ConsoleSceneResetAmpBinding")]
    public void ConsoleSceneBindsResetToRealComponent(string target, string type, string method)
    {
        string scene = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Console.unity"));
        string call = @"m_Target: \{fileID: " + target + @"\}\s+m_TargetAssemblyTypeName: " + type + @", Assembly-CSharp\s+m_MethodName: " + method + @"\s+m_Mode: 1\b";
        Assert.That(Regex.Matches(scene, call).Count, Is.EqualTo(1));
        string guid = Regex.Match(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, type + ".cs.meta")), @"(?m)^guid: (\w+)").Groups[1].Value;
        Assert.That(guid, Has.Length.EqualTo(32));
        string component = Regex.Match(scene, @"(?ms)^--- !u!114 &" + target + @"\r?\n(?:(?!^---).)*").Value;
        Assert.That(component, Does.Contain("guid: " + guid));
        Assert.That((type == "MinMax" ? typeof(MinMax) : typeof(Stimulation)).GetMethod(method, Type.EmptyTypes), Is.Not.Null);
    }

    // Small bridge to the unchanged wrapper fixture's existing private setup/observations.
    private static object Field(object instance, string name) => instance.GetType().GetField(name, Private).GetValue(instance);
    private static void Set(object instance, string name, object value) => instance.GetType().GetField(name, Private).SetValue(instance, value);
    private static void Call(object instance, string name, params object[] arguments)
    {
        try { instance.GetType().GetMethod(name, Private | BindingFlags.Public).Invoke(instance, arguments); }
        catch (TargetInvocationException ex) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw(); }
    }
}
