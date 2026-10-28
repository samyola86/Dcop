using System;
using System.IO;
using BadGuy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ShoppingLevelSetup
{
    public const string Path="Assets/BadGuy/Scenes/ShoppingDistrict.unity";
    [MenuItem("BAD GUY/Open Level 01 - Shopping District")]
    public static void OpenLevel()
    {
        if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;EditorApplication.delayCall+=OpenLevel;return;}
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        EnsureLevel();EditorSceneManager.OpenScene(Path);
    }
    public static void EnsureLevel()
    {
        if(File.Exists(Path))
        {
            var loaded=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(Path);
            bool alreadyOpen=loaded.IsValid()&&loaded.isLoaded;
            var existing=alreadyOpen?loaded:EditorSceneManager.OpenScene(Path,OpenSceneMode.Additive);
            if(existing.isDirty)throw new Exception("Shopping scene has unsaved changes; save or review before building.");
            foreach(var go in existing.GetRootGameObjects())
            {
                var controller=go.GetComponent<HarnessController>();
                if(controller!=null && controller.PrototypeMaterial==null)
                {
                    controller.PrototypeMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/BadGuy/Settings/Floor.asset");
                    if(controller.PrototypeMaterial==null)throw new Exception("Missing prototype material");
                    EditorSceneManager.MarkSceneDirty(existing);EditorSceneManager.SaveScene(existing);
                }
            }
            if(!alreadyOpen)EditorSceneManager.CloseScene(existing,true);
            return;
        }
        Directory.CreateDirectory("Assets/BadGuy/Scenes");
        var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        var camera=new GameObject("Street camera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(0,3,-10);camera.transform.LookAt(new Vector3(0,2.7f,9));camera.fieldOfView=48;
        camera.gameObject.AddComponent<AudioListener>();
        var light=new GameObject("Late afternoon sunlight").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.8f;light.transform.rotation=Quaternion.Euler(42,-32,0);
        RenderSettings.ambientLight=new Color(.6f,.65f,.7f);
        var root=new GameObject("Level 01 / shared combat");
        var combat=root.AddComponent<HarnessController>();var level=root.AddComponent<ShoppingDistrictLevel>();
        combat.LevelMode=true;combat.Level=level;combat.View=camera;level.Combat=combat;
        combat.Tuning=AssetDatabase.LoadAssetAtPath<HarnessTuning>("Assets/BadGuy/Settings/HarnessTuning.asset");
        combat.PrototypeMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/BadGuy/Settings/Floor.asset");
        if(combat.Tuning==null)throw new Exception("HarnessTuning.asset is missing. Run harness setup first.");
        EditorSceneManager.SaveScene(scene,Path);EditorSceneManager.CloseScene(scene,true);
        if(previous.IsValid())UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
        AssetDatabase.SaveAssets();
        Debug.Log("BADGUY_LEVEL_CREATED "+Path);
    }
    public static void BuildLevel()
    {
        EnsureLevel();HarnessSetup.Validate();
        string output=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../..","artifacts","Level01","BadGuyLevel01.exe"));
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output));
        var result=BuildPipeline.BuildPlayer(new[]{Path},output,BuildTarget.StandaloneWindows64,BuildOptions.Development);
        if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Level build failed: "+result.summary.result);
        Debug.Log("BADGUY_LEVEL_BUILD_OK "+output);
    }
    public static void BuildPractice()
    {
        HarnessSetup.Validate();
        string output=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../..","artifacts","Windows","BadGuyHarness.exe"));
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output));
        var result=BuildPipeline.BuildPlayer(new[]{"Assets/BadGuy/Scenes/CombatHarness.unity"},output,BuildTarget.StandaloneWindows64,BuildOptions.Development);
        if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Practice build failed");
    }
}

// File-based local editor command bridge. No network listener, shell execution or arbitrary methods.
[InitializeOnLoad]
public static class LevelEditorBridge
{
    static string Root=>System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../..","artifacts"));
    static bool busy;
    static LevelEditorBridge(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        string request=System.IO.Path.Combine(Root,"level-command.txt");
        if(busy||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(request))return;
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        string command=File.ReadAllText(request).Trim();File.Delete(request);busy=true;
        File.WriteAllText(System.IO.Path.Combine(Root,"level-editor-busy.json"),JsonUtility.ToJson(new Receipt{command=command,scenes=DateTime.UtcNow.ToString("O")}));
        EditorApplication.delayCall+=()=>
        {
            try
            {
                if(command=="build"||command=="build-practice")
                    for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                        if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene changes: review/save before building.");
                if(command=="build")ShoppingLevelSetup.BuildLevel();
                else if(command=="build-practice")ShoppingLevelSetup.BuildPractice();
                else if(command=="create")ShoppingLevelSetup.EnsureLevel();
                else if(command=="inspect") { }
                else throw new Exception("Unsupported level command");
                string scenes="";
                for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                {
                    var scene=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                    scenes+=scene.path+" dirty="+scene.isDirty+"; ";
                }
                File.WriteAllText(System.IO.Path.Combine(Root,"level-editor-result.json"),JsonUtility.ToJson(new Receipt{success=true,command=command,scenes=scenes},true));
            }
            catch(Exception ex){Debug.LogException(ex);File.WriteAllText(System.IO.Path.Combine(Root,"level-editor-result.json"),JsonUtility.ToJson(new Failure{error=ex.ToString()}));}
            finally{busy=false;File.Delete(System.IO.Path.Combine(Root,"level-editor-busy.json"));}
        };
    }
    [Serializable]class Failure {public bool success=false;public string error;}
    [Serializable]class Receipt {public bool success;public string command;public string scenes;}
}
