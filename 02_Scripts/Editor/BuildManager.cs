#if UNITY_EDITOR
using System;
using System.IO;
using AppleAuth.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

public enum BuildType
{
    DEV,
    TEST,
    REAL
}

public enum AddressableProfile
{
    Dev,
    Real
}

public class BuildManager : Editor
{
    public const string DEV_SCRIPTING_DEFINE_SYMBOLS = "DEV_VER";
    public const string REAL_SCRIPTING_DEFINE_SYMBOLS = "";

    private static BuildType buildType = BuildType.DEV;
    
    [MenuItem("Build/AOS/Set AOS DEV Build Settings")]
    public static void SetAOSDEVBuildSettings()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        EditorUserBuildSettings.buildAppBundle = false;
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, DEV_SCRIPTING_DEFINE_SYMBOLS);
        
        buildType = BuildType.DEV;
        
        SetAddressableProfile(AddressableProfile.Dev);
        
        Logger.Log($"Build settings set to {buildType}");
    }

    [MenuItem("Build/AOS/Set AOS TEST Build Settings")]
    public static void SetAOSTESTBuildSettings()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        EditorUserBuildSettings.buildAppBundle = true;
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, DEV_SCRIPTING_DEFINE_SYMBOLS);
        
        buildType = BuildType.TEST;
        
        SetAddressableProfile(AddressableProfile.Dev);
        
        Logger.Log($"Build settings set to {buildType}");
    }

    [MenuItem("Build/AOS/Set AOS REAL Build Settings")]
    public static void SetAOSREALBuildSettings()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        EditorUserBuildSettings.buildAppBundle = true;
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, REAL_SCRIPTING_DEFINE_SYMBOLS);
        
        buildType = BuildType.REAL;
        
        SetAddressableProfile(AddressableProfile.Real);
        
        Logger.Log($"Build settings set to {buildType}");
    }

    private static void SetAddressableProfile(AddressableProfile profile)
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        AddressableAssetProfileSettings profileSettings = settings.profileSettings;
        string profileId = profileSettings.GetProfileId(profile.ToString());
        settings.activeProfileId = profileId;
        EditorUtility.SetDirty(settings);
    }
    
    [MenuItem("Build/AOS/Start AOS Build")]
    public static void StartAOSBuild()
    {
        PlayerSettings.Android.keystoreName = "Builds/AOS/sanghyun.keystore";
        PlayerSettings.Android.keystorePass = "581qw15qq!!A";
        PlayerSettings.Android.keyaliasName = "sanghyun";
        PlayerSettings.Android.keyaliasPass = "581qw15qq!!A";
        
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[]
        {
            "Assets/01_Scenes/Title.unity",
            "Assets/01_Scenes/Lobby.unity",
            "Assets/01_Scenes/InGame.unity"
        };
        buildPlayerOptions.target = BuildTarget.Android;
        string fileExtention = string.Empty;
        BuildOptions compressOption = BuildOptions.None;

        switch (buildType)
        {
            case BuildType.DEV:
                fileExtention = "apk";
                compressOption = BuildOptions.CompressWithLz4;
                break;
            case BuildType.TEST:
            case BuildType.REAL:
                fileExtention = "aab";
                compressOption = BuildOptions.CompressWithLz4HC;
                break;
            default:
                break;
        }

        buildPlayerOptions.locationPathName = $"Builds/AOS/PixelOrcSlayer_{Application.version}_{DateTime.Now.ToString("yyMMdd_HHmmss")}.{fileExtention}";
        buildPlayerOptions.options = compressOption;
        
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;
        if (summary.result == BuildResult.Succeeded)
        {
            Logger.Log($"Build successed. {summary.totalSize} bytes. [{buildType.ToString()}]");
        }
        else if (summary.result == BuildResult.Failed)
        {
            Logger.LogError($"Build failed.");
        }
    }
    
    [MenuItem("Build/iOS/Set iOS TEST Build Settings")]
    public static void SetiOSTESTBuildSettings()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, DEV_SCRIPTING_DEFINE_SYMBOLS);
        
        buildType = BuildType.TEST;
        
        SetAddressableProfile(AddressableProfile.Dev);
        
        Logger.Log($"Build settings set to {buildType}");
    }
    
    [MenuItem("Build/iOS/Set iOS REAL Build Settings")]
    public static void SetiOSREALBuildSettings()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, REAL_SCRIPTING_DEFINE_SYMBOLS);
        
        buildType = BuildType.REAL;
        
        SetAddressableProfile(AddressableProfile.Real);
        
        Logger.Log($"Build settings set to {buildType}");
    }
     
    [MenuItem("Build/iOS/Start iOS Build")]
    public static void StartiOSBuild()
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[]
        {
            "Assets/01_Scenes/Title.unity",
            "Assets/01_Scenes/Lobby.unity",
            "Assets/01_Scenes/InGame.unity"
        };
        buildPlayerOptions.target = BuildTarget.iOS;
        buildPlayerOptions.options = BuildOptions.CompressWithLz4HC;;
        
        buildPlayerOptions.locationPathName = $"../iOSBuilds/PixelOrcSlayer_{Application.version}_{DateTime.Now.ToString("yyMMdd_HHmmss")}";
        
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;
        if (summary.result == BuildResult.Succeeded)
        {
            Logger.Log($"Build successed. {summary.totalSize} bytes. [{buildType.ToString()}]");
        }
        else if (summary.result == BuildResult.Failed)
        {
            Logger.LogError($"Build failed.");
        }
    }
    
    // 사용자 앱 추적 금지 여부 문구 추가.
    [PostProcessBuild(0)]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string pathToXCode)
    {
#if UNITY_IOS        
        if (buildTarget == BuildTarget.iOS)
        {
            OnPostProcessBuildiOS(pathToXCode);
        }
#endif
    }
    
#if UNITY_IOS     
    private static void OnPostProcessBuildiOS(string xCodeProjectPath)
    {
        //Info.plist
        string plistPath = Path.Combine(xCodeProjectPath, "Info.plist");
        PlistDocument plistObj = new PlistDocument();
        plistObj.ReadFromString(File.ReadAllText(plistPath));
        PlistElementDict plistRoot = plistObj.root;
        
        plistRoot.SetString("NSUserTrackingUsageDescription", "Your data will be used to provide you a better and personalized ad experience.");
        
        PlistElementArray bundleUrlTypes = plistRoot.CreateArray("CFBundleURLTypes");
        PlistElementDict dict = bundleUrlTypes.AddDict();
        PlistElementArray urlSchemes = dict.CreateArray("CFBundleURLSchemes");
        urlSchemes.AddString("com.googleusercontent.apps.118768134419-ctgmi24fe2f0odu4pdjo0n5obahvv261");
        plistRoot.SetString("GIDClientID", "118768134419-ctgmi24fe2f0odu4pdjo0n5obahvv261.apps.googleusercontent.com");
        
        File.WriteAllText(plistPath, plistObj.WriteToString());
        
        var projectPath = PBXProject.GetPBXProjectPath(xCodeProjectPath);
        var project = new PBXProject();
        project.ReadFromString(File.ReadAllText(projectPath));
        var manager = new ProjectCapabilityManager(projectPath, "Entitlements,entitlements", null,
            project.GetUnityFrameworkTargetGuid());
        manager.AddSignInWithAppleWithCompatibility();
        manager.WriteToFile();
    }
#endif    
    
    [MenuItem("Build/Start Addressable Build")]
    public static void BuildAddressableAssets()
    {
        AddressableAssetSettings.BuildPlayerContent();
    }
}
#endif
