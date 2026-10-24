using System;
using Wss.Testing;

/// <summary>Routes the application's conformance calls through the real shared WSS wrapper.</summary>
/// <remarks>
/// The Unity host requires its main thread; the .NET host serializes calls with its tick loop.
/// Call Initialize, advance Tick while awaiting observations,
/// and Dispose before removing fixture files. No scene or physical transport is activated.
/// This entry point checks Core propagation; it does not exercise the UI managers.
/// </remarks>
public sealed partial class WssConformanceController : IDisposable
{
    private readonly Stimulation _stimulation;
    private bool _disposed;

    /// <summary>Initializes Core through the shared wrapper without activating a scene.</summary>
    public void Initialize() { ThrowIfDisposed(); _stimulation.resetRadio(); }

    /// <summary>Advances the emulator through the wrapper on the host's serialized execution context.</summary>
    public void Tick() { ThrowIfDisposed(); _stimulation.TickConformance(); }

    /// <summary>Gets whether the wrapper reports readiness.</summary>
    public bool Ready() { ThrowIfDisposed(); return _stimulation.Ready(); }

    /// <summary>Gets whether the wrapper reports streaming.</summary>
    public bool Started() { ThrowIfDisposed(); return _stimulation.Started(); }

    /// <summary>Gets the shared emulator's observation and evaluation interface.</summary>
    /// <param name="conformance">The active observation interface, or null.</param>
    /// <returns>True when initialization supplied the emulator.</returns>
    public bool TryGetConformance(out IWssConformance conformance)
    { ThrowIfDisposed(); return _stimulation.TryGetConformance(out conformance); }

    /// <summary>Forwards the shared direct-analog scenario through the application entry point.</summary>
    /// <param name="channel">Wrapper channel name or chN alias.</param>
    /// <param name="pulseWidth">Pulse width in microseconds.</param>
    /// <param name="amplitude">Amplitude in milliamperes.</param>
    /// <param name="ipi">Inter-pulse interval in milliseconds.</param>
    public void StimulateAnalog(string channel, int pulseWidth, int amplitude, int ipi)
    { ThrowIfDisposed(); _stimulation.StimulateAnalog(channel, pulseWidth, amplitude, ipi); }

    /// <summary>Forwards a normalized scenario through the application entry point.</summary>
    /// <param name="channel">Wrapper channel name or chN alias.</param>
    /// <param name="input">Canonical scenario input, including clamping boundary inputs.</param>
    public void StimulateNormalized(string channel, float input)
    { ThrowIfDisposed(); _stimulation.StimulateNormalized(channel, input); }

    /// <summary>Forwards a runtime Event ratio edit through the application entry point.</summary>
    /// <param name="target">Wrapper target index.</param>
    /// <param name="ratio">Canonical Event ratio.</param>
    /// <param name="eventId">Canonical Event identifier.</param>
    public void UpdateEventRatio(int target, int ratio, int eventId)
    { ThrowIfDisposed(); _stimulation.UpdateEventRatio(target, ratio, eventId); }

    /// <summary>Forwards the shared stop scenario through the application entry point.</summary>
    /// <param name="target">Wrapper target index.</param>
    public void StopStimulation(int target)
    { ThrowIfDisposed(); _stimulation.StopStimulation(target); }

    /// <summary>Disposes Core and releases the platform host.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _stimulation.DisposeConformance(); }
        finally { DisposeHost(); }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(WssConformanceController));
    }
}
