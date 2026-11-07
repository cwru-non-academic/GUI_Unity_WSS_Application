using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

public sealed class BleImporterContractTests
{
    [TestCase("WSS.Transport.BLE.MacOS.dll", false)]
    [TestCase("libwss_macos_ble.dylib", true)]
    public void MacPluginsAreRestrictedToMacMonoUniversal(string filename, bool native)
    {
        string path = AssetDatabase.GetAllAssetPaths().Single(p => p.EndsWith("/backends/macos/" + filename, StringComparison.Ordinal));
        var importer = (PluginImporter)AssetImporter.GetAtPath(path);
        Assert.That(importer.GetCompatibleWithAnyPlatform(), Is.False);
        Assert.That(importer.GetCompatibleWithEditor(), Is.True);
        Assert.That(importer.GetEditorData("OS"), Is.EqualTo("OSX"));
        Assert.That(importer.GetEditorData("CPU"), Is.EqualTo("AnyCPU"));
        Assert.That(importer.GetCompatibleWithPlatform(BuildTarget.StandaloneOSX), Is.True);
        Assert.That(importer.GetPlatformData(BuildTarget.StandaloneOSX, "CPU"), Is.EqualTo("AnyCPU"));
        Assert.That(importer.GetCompatibleWithPlatform(BuildTarget.StandaloneWindows64), Is.False);
        Assert.That(importer.GetCompatibleWithPlatform(BuildTarget.StandaloneLinux64), Is.False);
        Assert.That(importer.DefineConstraints, Is.EquivalentTo(new[] { "!ENABLE_IL2CPP" }));
        Assert.That(importer.isNativePlugin, Is.EqualTo(native));
        Assert.That(importer.isPreloaded, Is.False);
    }

    [Test]
    public void DesktopDiagnosticDoesNotScanConnectOrStimulate()
    {
#if UNITY_EDITOR_WIN || (UNITY_EDITOR_OSX && !ENABLE_IL2CPP)
        WindowsBleTransportSmokeTest.VerifyInstallation();
#else
        Assert.Throws<PlatformNotSupportedException>(() => WindowsBleTransportSmokeTest.VerifyInstallation());
#endif
    }
}
