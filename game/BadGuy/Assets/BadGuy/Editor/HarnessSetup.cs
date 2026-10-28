using System;
using System.IO;
using BadGuy;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class HarnessSetup
{
    const string ScenePath = "Assets/BadGuy/Scenes/CombatHarness.unity";
    [MenuItem("BAD GUY/Create or open combat harness")]
    public static void Setup()
    {
        Directory.CreateDirectory("Assets/BadGuy/Scenes");
        Directory.CreateDirectory("Assets/BadGuy/Settings");
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/BadGuy/Settings/MobileURP.asset");
        if (pipeline == null)
        {
            var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, "Assets/BadGuy/Settings/MobileRenderer.asset");
            pipeline = UniversalRenderPipelineAsset.Create(renderer);
            pipeline.msaaSampleCount = 2; pipeline.renderScale = 1;
            pipeline.supportsHDR = false;
            AssetDatabase.CreateAsset(pipeline, "Assets/BadGuy/Settings/MobileURP.asset");
        }
        GraphicsSettings.defaultRenderPipeline = pipeline; QualitySettings.renderPipeline = pipeline;
        var tuning = AssetDatabase.LoadAssetAtPath<HarnessTuning>("Assets/BadGuy/Settings/HarnessTuning.asset");
        if (tuning == null) { tuning = ScriptableObject.CreateInstance<HarnessTuning>(); AssetDatabase.CreateAsset(tuning,"Assets/BadGuy/Settings/HarnessTuning.asset"); }
        if (!File.Exists(ScenePath))
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera = new GameObject("Harness camera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0,2,-7); camera.transform.rotation = Quaternion.Euler(2,0,0);
            camera.fieldOfView = 42; camera.backgroundColor = new Color(.035f,.065f,.10f); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.gameObject.AddComponent<AudioListener>();
            var light = new GameObject("Key light").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.5f; light.transform.rotation=Quaternion.Euler(45,-25,0);
            RenderSettings.ambientLight = new Color(.5f,.55f,.65f);
            var root = new GameObject("BAD GUY harness").AddComponent<HarnessController>(); root.View = camera; root.Tuning = tuning;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name="Greybox floor"; floor.transform.position=new Vector3(0,-.2f,8); floor.transform.localScale=new Vector3(30,.25f,35);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")); material.color=new Color(.12f,.17f,.21f); AssetDatabase.CreateAsset(material,"Assets/BadGuy/Settings/Floor.asset"); floor.GetComponent<Renderer>().sharedMaterial=material;
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),ScenePath);
        }
        else EditorSceneManager.OpenScene(ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath,true) };
        PlayerSettings.companyName="Dcop"; PlayerSettings.productName="BAD GUY Combat Harness";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.dcop.badguy");
        PlayerSettings.defaultScreenWidth=1280; PlayerSettings.defaultScreenHeight=720;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
        PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
        PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("BADGUY_SETUP_OK " + ScenePath);
    }
    [MenuItem("BAD GUY/Run rules checks")]
    public static void Validate()
    {
        int checks=0;
        Action<bool,string> expect=(ok,label)=> { if(!ok) throw new Exception("FAILED: " + label); checks++; Debug.Log("PASS: " + label); };
        expect(Math.Abs(CombatRules.Damage(20,HitZone.Crotch,false)-60)<.001,"3x precision damage");
        expect(Math.Abs(CombatRules.Damage(20,HitZone.Crotch,true)-69)<.001,"Overdrive multiplier");
        expect(Math.Abs(CombatRules.LinearFactor(true,false)*CombatRules.LinearFactor(true,false)-1.2)<.001,"Bhabi area not width");
        expect(CombatRules.AreaFactor(true,true)==.7f,"Casualty overrides presence");
        expect(!CombatRules.RollAI(HitZone.Body,1,0),"Nonprecision cannot drop AI");
        expect(CombatRules.RollAI(HitZone.Crotch,.05f,.01),"Eligible precision drop");
        expect(!CombatRules.RollAI(HitZone.Crotch,.05f,.1),"Eligible does not guarantee drop");
        var state=new RunState{Magazine=0,Reserve=5}; state.Reload(12);
        expect(state.Magazine==5 && state.Reserve==0,"Reload conserves rounds");
        state.Magazine=0; expect(!state.TryFire() && state.Magazine==0,"Empty cannot fire");
        state.AIReady=true; expect(state.Activate(8),"Charge activates");
        expect(!state.Activate(8),"Active cannot stack");
        expect(!state.HurtCivilian(true) && state.CiviliansHit==0 && !state.BhabiCasualty,"AI civilian immunity");
        state.OverdriveRemaining=0; expect(state.HurtCivilian(true) && state.BhabiCasualty && state.Score==-1000,"Casualty penalty");
        state.Health=90; state.Collect(PickupKind.Biryani,25,120); expect(state.Health==100,"Health cap");
        state.Reserve=115; state.Collect(PickupKind.Ammo,24,120); expect(state.Reserve==120,"Ammo cap");
        var rng1=new System.Random(2026); var rng2=new System.Random(2026); bool same=true;
        for(int i=0;i<30;i++) same &= rng1.Next()==rng2.Next(); expect(same,"Seed reproducibility");
        string report=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts","rules-report.json"));
        Directory.CreateDirectory(Path.GetDirectoryName(report)); File.WriteAllText(report,"{\"passed\":"+checks+",\"failed\":0,\"engine\":\""+Application.unityVersion+"\"}");
        Debug.Log("BADGUY_RULES_OK " + checks);
    }
    public static void SetupAndValidate() { Setup(); Validate(); }
    public static void BuildWindows()
    {
        SetupAndValidate();
        string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts","Windows","BadGuyHarness.exe"));
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report=BuildPipeline.BuildPlayer(new[]{ScenePath},output,BuildTarget.StandaloneWindows64,BuildOptions.Development);
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Build failed: "+report.summary.result);
        Debug.Log("BADGUY_BUILD_OK " + output);
    }
    public static void BuildAndroid()
    {
        if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android)) throw new Exception("Install Android Build Support, SDK/NDK and OpenJDK via Unity Hub first.");
        SetupAndValidate();
        string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts","Android","BadGuyHarness.apk"));
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report=BuildPipeline.BuildPlayer(new[]{ScenePath},output,BuildTarget.Android,BuildOptions.Development);
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Android build failed.");
    }
}

