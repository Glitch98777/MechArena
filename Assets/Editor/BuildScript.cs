using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Android;
using UnityEngine;

// Batchmode Android build entry point:
//   Unity.exe -batchmode -quit -buildTarget Android -executeMethod BuildScript.PerformAndroidBuild
public static class BuildScript
{
    const string AndroidRoot =
        @"C:\Program Files\Unity\Hub\Editor\6000.3.5f1\Editor\Data\PlaybackEngines\AndroidPlayer";
    const string Jdk = @"C:\Program Files\Eclipse Adoptium\jdk-17.0.19.10-hotspot";

    public static void PerformAndroidBuild()
    {
        // External tools (embedded OpenJDK is missing on this machine, so point at Adoptium 17).
        try
        {
            AndroidExternalToolsSettings.sdkRootPath = AndroidRoot + @"\SDK";
            AndroidExternalToolsSettings.ndkRootPath = AndroidRoot + @"\NDK";
            AndroidExternalToolsSettings.gradlePath  = AndroidRoot + @"\Tools\gradle";
            AndroidExternalToolsSettings.jdkRootPath = Jdk;
        }
        catch (Exception e) { Debug.LogWarning("Tool path set failed: " + e.Message); }

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        PlayerSettings.companyName = "Kaden";
        PlayerSettings.productName = "Mech Arena";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.kaden.mecharena");
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

        // IL2CPP + ARM64 so it installs on 64-bit-only phones too (Mono only builds 32-bit armeabi-v7a).
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;

        // Release-signed with a dedicated keystore so it sideloads cleanly on any device.
        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = @"C:\Users\kaden\MechArena\mecharena.keystore";
        PlayerSettings.Android.keystorePass = "mecharena";
        PlayerSettings.Android.keyaliasName = "mecharena";
        PlayerSettings.Android.keyaliasPass = "mecharena";
        EditorUserBuildSettings.buildAppBundle = false;

        string outDir = "Build";
        System.IO.Directory.CreateDirectory(outDir);
        string apk = outDir + "/MechArena.apk";

        var opts = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Main.unity" },
            locationPathName = apk,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None
        };

        Debug.Log("=== MechArena: starting Android build ===");
        BuildReport report = BuildPipeline.BuildPlayer(opts);
        BuildSummary s = report.summary;
        Debug.Log($"=== MechArena build result: {s.result}, size {s.totalSize} bytes, errors {s.totalErrors} ===");

        if (s.result != BuildResult.Succeeded)
        {
            Debug.LogError("MechArena build FAILED.");
            EditorApplication.Exit(1);
        }
        Debug.Log("=== MechArena build SUCCEEDED: " + apk + " ===");
        EditorApplication.Exit(0);
    }
}
