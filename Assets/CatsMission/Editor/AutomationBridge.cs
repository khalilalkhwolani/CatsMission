using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatsMission.EditorTools
{
    // Project-local, allowlisted automation. No server, sockets, downloads or arbitrary C# evaluation.
    [InitializeOnLoad]
    public static class AutomationBridge
    {
        private static readonly string Folder=Path.GetFullPath("UserSettings/CatsMissionAutomation");
        private static double nextPoll;
        private static TestRunnerApi runner;
        private static readonly TestResults callbacks=new TestResults();
        [Serializable] public class Request { public string id; public string action; public string argument; }
        [Serializable] public class Response
        { public string id; public string action; public string status; public string detail; public string utc; }
        static AutomationBridge()
        {
            Directory.CreateDirectory(Folder);
            EditorApplication.update+=Update;
            CompilationPipeline.assemblyCompilationFinished+=CompilationFinished;
            EditorApplication.delayCall+=() =>
            {
                runner=ScriptableObject.CreateInstance<TestRunnerApi>();
                runner.RegisterCallbacks(callbacks);
                Heartbeat();
            };
        }
        private static void CompilationFinished(string assembly,CompilerMessage[] messages)
        {
            var errors=messages.Where(m=>m.type==CompilerMessageType.Error).Select(m=>m.message);
            if(errors.Any()) File.AppendAllLines(Path.Combine(Folder,"compile-errors.txt"),errors);
        }
        private static void Heartbeat()
        {
            File.WriteAllText(Path.Combine(Folder,"heartbeat.json"),JsonUtility.ToJson(new Response {
                status=EditorApplication.isCompiling?"compiling":EditorApplication.isPlaying?"playing":"ready",
                detail=Application.unityVersion+" | "+Application.dataPath+" | "+SceneManager.GetActiveScene().name,
                utc=DateTime.UtcNow.ToString("O") },true));
        }
        private static void Update()
        {
            if(EditorApplication.timeSinceStartup<nextPoll) return;
            nextPoll=EditorApplication.timeSinceStartup+1;
            Heartbeat();
            string requestPath=Path.Combine(Folder,"request.json");
            if(!File.Exists(requestPath) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            var request=JsonUtility.FromJson<Request>(File.ReadAllText(requestPath));
            File.Move(requestPath,Path.Combine(Folder,"consumed-"+request.id+".json"));
            Respond(request,"running","Accepted by the current Unity Editor.");
            try { Execute(request); }
            catch(Exception error) { Respond(request,"failed",error.ToString()); Debug.LogException(error); }
        }
        private static void Execute(Request request)
        {
            switch(request.action)
            {
                case "slice": GardenBuilder.VerticalSlice(); break;
                case "campaign": GardenBuilder.CompleteCampaign(); break;
                case "polish": AssetPolish.Apply(); break;
                case "requirements-polish": RequirementsPolish.Apply(); break;
                case "final-polish": FinalPolish.Apply(); break;
                case "status": break;
                case "refresh": AssetDatabase.Refresh(); break;
                case "evidence": EditorEvidence.Show(request.argument); break;
                case "validate": ValidateScenes(); break;
                case "open":
                    if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
                    if(SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Current scene has unsaved changes.");
                    if(!new[]{"MainMenu","Level1","Level2","Level3"}.Contains(request.argument)) throw new ArgumentException("Scene is not allowlisted.");
                    EditorSceneManager.OpenScene(GardenBuilder.Scenes+request.argument+".unity"); break;
                case "play": EditorApplication.isPlaying=true; break;
                case "stop": EditorApplication.isPlaying=false; break;
                case "edit-tests": case "play-tests":
                    if(runner==null)
                    {
                        runner=ScriptableObject.CreateInstance<TestRunnerApi>();
                        runner.RegisterCallbacks(callbacks);
                    }
                    SessionState.SetString("CatsMission.TestRequest",JsonUtility.ToJson(request));
                    runner.Execute(new ExecutionSettings(new Filter { testMode=request.action=="edit-tests"?TestMode.EditMode:TestMode.PlayMode,
                        assemblyNames=new[]{request.action=="edit-tests"?"CatsMission.EditModeTests":"CatsMission.PlayModeTests"} }));
                    return;
                case "build": BuildWindows(); break;
                default: throw new ArgumentException("Unsupported automation action.");
            }
            Respond(request,"passed","Completed " + request.action + ".");
        }
        public static void ValidateScenes()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Validate scenes outside Play Mode.");
            string active=SceneManager.GetActiveScene().path;
            if(SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save scene before validation.");
            try
            {
                foreach(var entry in EditorBuildSettings.scenes.Where(s=>s.enabled))
                {
                    var scene=EditorSceneManager.OpenScene(entry.path);
                    foreach(var root in scene.GetRootGameObjects())
                        foreach(var transform in root.GetComponentsInChildren<Transform>(true))
                            if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)>0)
                                throw new InvalidOperationException("Missing script: "+entry.path+" / "+transform.name);
                    var controller=UnityEngine.Object.FindObjectOfType<LevelController>();
                    if(scene.name.StartsWith("Level") && (controller==null || controller.Player==null || UnityEngine.Object.FindObjectsOfType<FishCollectible>().Length<5))
                        throw new InvalidOperationException("Incomplete gameplay wiring: "+entry.path);
                }
                Debug.Log("CATSMISSION: Scene references validated.");
            }
            finally { if(!string.IsNullOrEmpty(active)) EditorSceneManager.OpenScene(active); }
        }
        [MenuItem("Cat's Mission/Validate/Scene References")]
        public static void ValidateFromMenu() => ValidateScenes();
        [MenuItem("Cat's Mission/Build/Windows Local Preview")]
        public static void BuildWindows()
        {
            ValidateScenes();
            string folder=Path.GetFullPath("Builds/Windows");
            Directory.CreateDirectory(folder);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),
                locationPathName=Path.Combine(folder,"CatsMission.exe"),target=BuildTarget.StandaloneWindows64,
                options=BuildOptions.None });
            Directory.CreateDirectory("Docs/Testing");
            File.WriteAllText("Docs/Testing/WindowsBuild.txt","Result: "+report.summary.result+
                "\nEditor: "+Application.unityVersion+"\nTarget: Windows x64 Local Preview\nPath: "+folder+
                "\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings+
                "\nBytes: "+report.summary.totalSize+"\nDuration: "+report.summary.totalTime+
                "\nSecurity: NOT cleared for distribution; CVE-2025-59489 remediation required.\n");
            if(report.summary.result!=BuildResult.Succeeded) throw new InvalidOperationException("Windows build failed; see Docs/Testing/WindowsBuild.txt.");
        }
        public static void Respond(Request request,string status,string detail)
        {
            File.WriteAllText(Path.Combine(Folder,"result-"+request.id+".json"),JsonUtility.ToJson(new Response {
                id=request.id,action=request.action,status=status,detail=detail,utc=DateTime.UtcNow.ToString("O") },true));
        }
        private sealed class TestResults : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                string value=SessionState.GetString("CatsMission.TestRequest","");
                if(string.IsNullOrEmpty(value)) return;
                var request=JsonUtility.FromJson<Request>(value);
                Directory.CreateDirectory("Docs/Testing");
                File.WriteAllText("Docs/Testing/"+request.id+".xml",result.ToXml().OuterXml);
                Respond(request,result.FailCount==0 && result.PassCount>0?"passed":"failed",
                    "Passed: "+result.PassCount+"; failed: "+result.FailCount+"; skipped: "+result.SkipCount+"; duration: "+result.Duration);
                SessionState.EraseString("CatsMission.TestRequest");
            }
        }
    }
}
