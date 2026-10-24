using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using Wss.Transports;
using Wss.Transports.Backends.Windows;
using Wss.Ble.Windows.Native;
#endif

[DisallowMultipleComponent]
public sealed class WindowsBleTransportSmokeTest : MonoBehaviour
{
    // No Start/OnEnable activation: this diagnostic never connects, scans or writes.
    [ContextMenu("BLE Transport Smoke Test/Construct and Dispose Twice")]
    public void RunDiagnostic()
    {
        if (!Application.isPlaying || !isActiveAndEnabled)
        {
            Debug.LogWarning("[BLE transport] Requires an enabled component in an isolated Play-mode scene.", this);
            return;
        }
        try { VerifyInstallation(); }
        catch (Exception ex) { Debug.LogException(ex, this); }
    }

    // Explicit batch invocation also permits verification without entering a scene
    // or activating any existing stimulation controllers.
    public static void VerifyInstallation()
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        WindowsBleBackend.Register();
        Debug.Log("[BLE transport] Windows native backend registered.");
        for (int cycle = 1; cycle <= 2; ++cycle)
        {
            BleNusTransportOptions options = BleNusTransport.CreateDefaultOptions("diagnostic-never-connect");
            using (var transport = new BleNusTransport(options))
            {
                // The facade intentionally exposes no backend/discovery getter.
                // Read-only introspection verifies registration selected the adapter,
                // rather than merely proving that its assembly was loaded.
                var field = typeof(BleNusTransport).GetField("_backend", BindingFlags.Instance | BindingFlags.NonPublic);
                var backend = field == null ? null : field.GetValue(transport);
                if (backend == null || backend.GetType().FullName !=
                    "Wss.Transports.Backends.Windows.WindowsBleNusBackend")
                    throw new InvalidOperationException("Normal transport did not resolve the registered Windows native backend.");
                if (transport.IsConnected)
                    throw new InvalidOperationException("Diagnostic transport unexpectedly connected.");
                Debug.Log("[BLE transport] Cycle " + cycle + ": constructed normal BleNusTransport; resolved " + backend.GetType().AssemblyQualifiedName);
            }
            Debug.Log("[BLE transport] Cycle " + cycle + ": Dispose returned.");
        }
        // Construction is lazy; separately validate native loading/ABI without
        // initializing the adapter, connecting, scanning or writing.
        using (var client = new WindowsBleClient())
            Debug.Log("[BLE transport] Production native DLL loaded; ABI " + client.AbiVersion);

        string[] expected = {
            "WSS_Core_Interface", "WSS.Transport.Serial", "WSS.Transport.BLE",
            "WSS.Transport.BLE.Windows", "WSS.Transport.BLE.Windows.Native"
        };
        var loaded = AppDomain.CurrentDomain.GetAssemblies();
        foreach (string name in expected)
        {
            var matches = loaded.Where(a => a.GetName().Name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Expected exactly one loaded " + name + "; found " + matches.Length);
            Debug.Log("[BLE transport] Loaded " + matches[0].FullName + " from " + matches[0].Location);
        }
        foreach (var assembly in loaded.Where(a => a.GetName().Name == "Newtonsoft.Json" ||
            a.GetName().Name.StartsWith("System.Memory", StringComparison.Ordinal) ||
            a.GetName().Name == "System.IO.Ports" || a.GetName().Name == "System.Buffers" ||
            a.GetName().Name == "System.Numerics.Vectors" || a.GetName().Name == "System.Runtime.CompilerServices.Unsafe"))
            Debug.Log("[BLE transport] Dependency " + assembly.FullName + " from " + assembly.Location);
        Debug.Log("[BLE transport] PASS: repeated construction/disposal, native ABI and unique WSS identities; no connections or writes.");
#else
        throw new PlatformNotSupportedException("This diagnostic requires Windows Editor or Windows standalone.");
#endif
    }
}
