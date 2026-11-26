using UnityEditor;
using UnityEngine;
namespace CatsMission.EditorTools
{
    // Read-only editor selections for documenting the existing implementation.
    public static class EditorEvidence
    {
        public static void Show(string topic)
        {
            switch(topic)
            {
                case "player":
                    Selection.activeGameObject=Object.FindObjectOfType<PlayerCat>().gameObject;
                    SceneView.lastActiveSceneView.FrameSelected(); break;
                case "rules":
                    Selection.activeObject=AssetDatabase.LoadAssetAtPath<MonoScript>(GardenBuilder.Root+"/Scripts/Core/LevelState.cs"); break;
                case "audio":
                    Selection.activeGameObject=Object.FindObjectOfType<GameAudio>().gameObject; break;
                case "hud":
                    Selection.activeGameObject=Object.FindObjectOfType<GameHud>().gameObject; break;
                case "tests": EditorApplication.ExecuteMenuItem("Window/General/Test Runner"); break;
                case "build": EditorApplication.ExecuteMenuItem("File/Build Settings..."); break;
                case "scenes":
                    Selection.activeObject=AssetDatabase.LoadAssetAtPath<DefaultAsset>(GardenBuilder.Root+"/Scenes");
                    EditorGUIUtility.PingObject(Selection.activeObject); break;
            }
        }
    }
}
