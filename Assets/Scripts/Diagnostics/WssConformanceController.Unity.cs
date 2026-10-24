using UnityEngine;

public sealed partial class WssConformanceController
{
    private readonly GameObject _host;

    /// <summary>Creates an inactive Unity host configured with the canonical fixture directory.</summary>
    /// <param name="configDirectory">Existing directory containing Core and parameter JSON.</param>
    public WssConformanceController(string configDirectory)
    {
        _host = new GameObject("WSS shared conformance");
        _host.hideFlags = HideFlags.HideAndDontSave;
        _host.SetActive(false);
        try
        {
            _stimulation = _host.AddComponent<Stimulation>();
            _stimulation.ConfigureConformance(configDirectory);
        }
        catch
        {
            UnityEngine.Object.DestroyImmediate(_host);
            throw;
        }
    }

    private void DisposeHost() => UnityEngine.Object.DestroyImmediate(_host);
}
