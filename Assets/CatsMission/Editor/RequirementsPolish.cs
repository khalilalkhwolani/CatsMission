using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CatsMission.EditorTools
{
    public static class RequirementsPolish
    {
        [MenuItem("Cat's Mission/Upgrade/Requirements Polish")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                throw new System.InvalidOperationException("Stop Play Mode and save the current scene first.");
            Directory.CreateDirectory(".backups/BeforeRequirementsPolish");
            foreach(var entry in EditorBuildSettings.scenes.Where(x=>x.enabled))
            {
                string backup=".backups/BeforeRequirementsPolish/"+Path.GetFileName(entry.path);
                if(!File.Exists(backup)) File.Copy(entry.path,backup);
                var scene=EditorSceneManager.OpenScene(entry.path);
                var old=GameObject.Find("Picnic Adventure");
                if(old!=null)
                {
                    // Keep useful scenery, optional static jumps, goal guidance and celebration.
                    var keep=new GameObject("Garden polish");
                    foreach(Transform child in old.transform.Cast<Transform>().ToArray())
                        if(new[]{"Jump lesson step","Creek water","Gate left post","Gate right post","Garden arch","Exit beacon","Victory confetti"}.Contains(child.name)) child.SetParent(keep.transform,true);
                    var source=old.GetComponent<AdventurePresentation>();
                    if(source!=null)
                    {
                        var presentation=keep.AddComponent<AdventurePresentation>();
                        presentation.exitBeacon=source.exitBeacon; presentation.celebration=source.celebration;
                    }
                    Object.DestroyImmediate(old);
                }
                var canvas=Object.FindObjectOfType<Canvas>();
                var strip=canvas.transform.Find("Adventure objective"); if(strip!=null) Object.DestroyImmediate(strip.gameObject);
                var level=Object.FindObjectOfType<LevelController>();
                foreach(var fish in Object.FindObjectsOfType<FishCollectible>())
                {
                    fish.name="Fish "+fish.Id;
                    var visual=fish.transform.Find("Fish visual");
                    foreach(string name in new[]{"Biscuit stamp","Paper wrapper"})
                    { var child=visual.Find(name); if(child!=null) Object.DestroyImmediate(child.gameObject); }
                    if(visual.Find("Fish eye")==null)
                    {
                        var eye=GameObject.CreatePrimitive(PrimitiveType.Sphere); eye.name="Fish eye";
                        eye.transform.SetParent(visual,false); eye.transform.localPosition=new Vector3(.17f,.045f,-.11f);
                        eye.transform.localScale=Vector3.one*.055f; Object.DestroyImmediate(eye.GetComponent<Collider>());
                        eye.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(GardenBuilder.Root+"/Materials/Eyes.mat");
                    }
                    // Keep the collectible above the ground and above optional crate tops.
                    var position=fish.transform.position;
                    position.y=((level.LevelNumber==1 && fish.Id==6)||(level.LevelNumber==3 && fish.Id==8))?1.8f:.85f;
                    fish.transform.position=position;
                }
                foreach(var text in canvas.GetComponentsInChildren<Text>(true))
                {
                    if(text.name=="Story") text.text="Explore the garden. Collect lost fish.\nHelp the little cat find its way home.";
                    if(text.name=="Caption") text.text="Cat's Mission";
                    if(text.name=="Footer") text.text="THREE GARDENS TO EXPLORE";
                    if(text.name=="Content" && text.transform.parent.name=="Instructions panel")
                    {
                        text.fontSize=22;
                        text.text="WASD / Arrow keys: Move     Space: Jump     Esc: Pause\n\nCollect the required fish, then reach the destination.\nEach fish gives 1 point. Extra fish increase your score.\n\nJump over boxes, fences and gaps.\nFalling costs 1 life and returns you to the level start.\nCollected fish and remaining time are kept after a fall.\n\nLose all 3 lives or run out of time: Retry the current level.\nFinish all three levels to bring the cat home and win!";
                    }
                    if(text.name=="Eyebrow" && text.transform.parent.name=="Game over panel") text.text="TRY THE LEVEL AGAIN";
                    if(text.name=="Eyebrow" && text.transform.parent.name=="Victory panel") text.text="CAT'S MISSION";
                }
                foreach(var button in canvas.GetComponentsInChildren<Button>(true))
                {
                    var image=button.GetComponent<Image>(); image.sprite=null;
                    bool primary=new[]{"Play","Next","Resume","Retry","Back"}.Contains(button.name);
                    image.color=primary?new Color(1,.68f,.16f):new Color(.08f,.33f,.35f);
                    button.GetComponentInChildren<Text>().color=primary?new Color(.09f,.16f,.19f):Color.white;
                    string label=button.name;
                    switch(button.name)
                    {
                        case "Instructions":label="Instructions";break;
                        case "Back":label="Back";break;
                        case "Resume":label="Resume";break;
                        case "Restart":label="Restart";break;
                        case "Main menu": case "Menu":label="Main Menu";break;
                        case "Next":label=level!=null && level.LevelNumber==3?"Play Again":"Next Level";break;
                        case "Retry":label="Retry";break;
                        case "Pause":label="Pause / Esc";break;
                    }
                    button.GetComponentInChildren<Text>().text=label;
                }
                if(level!=null)
                {
                    var message=canvas.GetComponentsInChildren<Text>(true).Single(x=>x.name=="Message");
                    message.fontSize=20; message.color=Color.white; message.alignment=TextAnchor.MiddleCenter;
                    message.rectTransform.sizeDelta=new Vector2(720,42);
                    message.rectTransform.anchoredPosition=new Vector2(0,-118);
                    // Text and its background use separate objects, as required by Unity UI.
                    var panel=canvas.transform.Find("Objective background");
                    if(panel==null)
                    {
                        var go=new GameObject("Objective background",typeof(RectTransform),typeof(Image)); go.transform.SetParent(canvas.transform,false);
                        var rect=(RectTransform)go.transform; rect.anchorMin=rect.anchorMax=new Vector2(.5f,1); rect.sizeDelta=new Vector2(760,44); rect.anchoredPosition=new Vector2(0,-118);
                        go.GetComponent<Image>().color=new Color(.04f,.13f,.15f,.92f); go.GetComponent<Image>().raycastTarget=false;
                        go.transform.SetSiblingIndex(message.transform.GetSiblingIndex()); panel=go.transform;
                    }
                    // This compact objective replaces the long narration over the play area.
                }
                var scaler=canvas.GetComponent<CanvasScaler>();
                if(scaler.referenceResolution.x<1900)
                {
                    foreach(var rect in canvas.GetComponentsInChildren<RectTransform>(true).Where(x=>x.transform!=canvas.transform))
                    { rect.anchoredPosition*=1.5f; rect.sizeDelta*=1.5f; }
                    foreach(var text in canvas.GetComponentsInChildren<Text>(true)) text.fontSize=Mathf.RoundToInt(text.fontSize*1.5f);
                    scaler.referenceResolution=new Vector2(1920,1080);
                }
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(GardenBuilder.Scenes+"MainMenu.unity");
        }
    }
}
