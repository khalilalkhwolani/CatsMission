using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CatsMission.EditorTools
{
    public static class AssetPolish
    {
        private const string Root="Assets/CatsMission/ThirdParty/";
        public static void Apply()
        {
            if (EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Stop play and save the active scene before applying assets.");
            string active=SceneManager.GetActiveScene().path;
            Directory.CreateDirectory(".backups/BeforeAssetPolish");
            AssetDatabase.Refresh();
            var textureImporter=(TextureImporter)AssetImporter.GetAtPath(Root+"UI/button.png");
            textureImporter.textureType=TextureImporterType.Sprite;
            textureImporter.spriteBorder=new Vector4(12,12,12,12); textureImporter.SaveAndReimport();
            Sprite button=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"UI/button.png");
            Sprite heart=MakeHeart();
            try
            {
                foreach (var entry in EditorBuildSettings.scenes.Where(s=>s.enabled))
                {
                    string backup=".backups/BeforeAssetPolish/"+Path.GetFileName(entry.path);
                    if (!File.Exists(backup)) File.Copy(entry.path,backup);
                    var scene=EditorSceneManager.OpenScene(entry.path);
                    foreach(var control in UnityEngine.Object.FindObjectsOfType<Button>(true))
                    {
                        var image=control.GetComponent<Image>();
                        if(image!=null) { image.sprite=button; image.type=Image.Type.Sliced; }
                    }
                    foreach(var root in scene.GetRootGameObjects())
                        foreach(var item in root.GetComponentsInChildren<Transform>(true))
                        {
                            if(item.name=="Garden tree") ReplaceVisual(item,"tree_oak",3.5f);
                            if(item.name=="Wildflower") ReplaceVisual(item,"flower_purpleA",.6f);
                        }
                    var audio=UnityEngine.Object.FindObjectOfType<GameAudio>();
                    if(audio==null) audio=new GameObject("Garden audio").AddComponent<GameAudio>();
                    audio.Configure(Clip("garden-music.mp3"),Clip("collect.ogg"),Clip("jump.ogg"),Clip("life.ogg"),
                        Clip("warning.ogg"),Clip("victory.ogg"),Clip("game-over.ogg"));
                    audio.ConfigureAmbience(Clip("garden-ambience.ogg"));
                    var level=UnityEngine.Object.FindObjectOfType<LevelController>();
                    if(level!=null)
                    {
                        level.ConfigureAudio(audio);
                        var hud=UnityEngine.Object.FindObjectOfType<GameHud>();
                        var lives=hud.GetComponentsInChildren<Text>(true).First(t=>t.name=="Lives");
                        var icons=new Image[3];
                        for(int i=0;i<3;i++)
                        {
                            string name="Life heart "+i;
                            var existing=lives.transform.parent.Find(name);
                            var go=existing!=null?existing.gameObject:new GameObject(name,typeof(RectTransform),typeof(Image));
                            var rect=(RectTransform)go.transform; rect.SetParent(lives.transform.parent,false);
                            rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f); rect.sizeDelta=new Vector2(30,30);
                            rect.anchoredPosition=new Vector2(-300+i*40,-17);
                            icons[i]=go.GetComponent<Image>(); icons[i].sprite=heart; icons[i].raycastTarget=false;
                            icons[i].color=new Color(1,.4f,.4f);
                        }
                        hud.ConfigureHearts(icons);
                        lives.enabled=false;
                    }
                    EditorSceneManager.SaveScene(scene);
                }
            }
            finally { if(!string.IsNullOrEmpty(active)) EditorSceneManager.OpenScene(active); }
            AssetDatabase.SaveAssets();
        }
        private static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"Audio/"+name);
        private static void ReplaceVisual(Transform parent,string model,float height)
        {
            if(parent.Find("Kenney visual")!=null) return;
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Nature/"+model+".fbx");
            if(source==null) throw new InvalidOperationException("Missing model "+model);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.name="Kenney visual"; instance.transform.SetParent(parent,false);
            var renderers=instance.GetComponentsInChildren<Renderer>();
            var bounds=renderers[0].bounds; foreach(var r in renderers) bounds.Encapsulate(r.bounds);
            float scale=height/Mathf.Max(.01f,bounds.size.y);
            instance.transform.localScale*=scale;
            instance.transform.localPosition-=new Vector3(bounds.center.x-parent.position.x,bounds.min.y-parent.position.y,bounds.center.z-parent.position.z)*scale;
            // Keep original collision geometry: changing art must not change the routes.
            foreach(var r in parent.GetComponentsInChildren<Renderer>(true))
                if(!r.transform.IsChildOf(instance.transform)) r.enabled=false;
        }
        private static Sprite MakeHeart()
        {
            const string file=Root+"UI/life-heart.png";
            if(!File.Exists(file))
            {
                var texture=new Texture2D(64,64,TextureFormat.RGBA32,false);
                for(int y=0;y<64;y++) for(int x=0;x<64;x++)
                {
                    float u=(x-31.5f)/27, v=(y-28f)/27;
                    float a=u*u+v*v-1;
                    texture.SetPixel(x,y,a*a*a-u*u*v*v*v<=0?Color.white:Color.clear);
                }
                texture.Apply(); File.WriteAllBytes(file,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(file);
            }
            var importer=(TextureImporter)AssetImporter.GetAtPath(file);
            importer.textureType=TextureImporterType.Sprite; importer.alphaIsTransparency=true; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(file);
        }
    }
}
