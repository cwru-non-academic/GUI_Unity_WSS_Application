using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEditor;
using Wss.Testing;

// Bridges the unchanged C# fixture's controller-shaped calls to the application entry point.
// Editor updates tick the inactive emulator host; no Play-mode scene is entered.
internal sealed class UnityConformanceAdapter : IDisposable
{
    internal static IEnumerator Wait(Task operation)
    {
        while (!operation.IsCompleted) yield return null;
        operation.GetAwaiter().GetResult();
    }

    // Complete on the Editor thread, even when no SynchronizationContext is installed.
    // Inline continuations preserve the original async case bodies without worker-thread Unity calls.
    internal static Task NextUpdate()
    {
        var completion = new TaskCompletionSource<bool>();
        EditorApplication.delayCall += () => completion.SetResult(true);
        return completion.Task;
    }

    private readonly WssConformanceController _controller;
    private Exception _tickFailure;

    internal UnityConformanceAdapter(string directory)
    {
        _controller = new WssConformanceController(directory);
    }

    public void Initialize()
    {
        _controller.Initialize();
        EditorApplication.update += Tick;
    }

    private void Tick()
    {
        if (_tickFailure != null) return;
        try { _controller.Tick(); }
        catch (Exception ex) { _tickFailure = ex; }
    }

    private void CheckTick()
    {
        if (_tickFailure != null) throw new InvalidOperationException("Unity conformance tick failed.", _tickFailure);
    }

    public bool Ready() { CheckTick(); return _controller.Ready(); }
    public bool Started() { CheckTick(); return _controller.Started(); }
    public bool TryGetConformance(out IWssConformance conformance) => _controller.TryGetConformance(out conformance);
    public void StimulateAnalog(string channel, int pw, int amplitude, int ipi) => _controller.StimulateAnalog(channel, pw, amplitude, ipi);
    public void StimulateNormalized(string channel, float input) => _controller.StimulateNormalized(channel, input);
    public void UpdateEventRatio(int target, int ratio, int eventId) => _controller.UpdateEventRatio(target, ratio, eventId);
    public void StopStimulation(int target) => _controller.StopStimulation(target);

    public void Dispose()
    {
        EditorApplication.update -= Tick;
        _controller.Dispose();
    }
}
