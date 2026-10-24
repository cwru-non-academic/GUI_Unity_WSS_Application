// Alternate lifetime host only; the application's forwarding methods are compiled
// from Assets/Scripts/Diagnostics/WssConformanceController.cs without substitutions.
public sealed partial class WssConformanceController
{
    /// <summary>Creates the ordinary .NET emulator host for the shared application command path.</summary>
    /// <param name="configDirectory">Existing canonical configuration directory.</param>
    public WssConformanceController(string configDirectory)
    {
        _stimulation = new Stimulation(configDirectory);
    }

    private void DisposeHost() { }
}
