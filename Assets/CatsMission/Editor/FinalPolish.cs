using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatsMission.EditorTools
{
    public static class FinalPolish
    {
        public static void Apply()
        {
            if(EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                throw new System.InvalidOperationException("Save the scene and stop playing first.");
            var cat=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(GardenBuilder.Root+"/Prefabs/Miso.prefab"));
            cat.transform.position=new Vector3(1000,0,1000);
            foreach(var child in cat.GetComponentsInChildren<Transform>()) child.gameObject.layer=30;
            var cameraObject=new GameObject("Portrait camera"); var cam=cameraObject.AddComponent<Camera>();
            cam.cullingMask=1<<30; cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=Color.clear;
            cam.orthographic=true; cam.orthographicSize=.95f;
            cam.transform.position=cat.transform.position+new Vector3(1.7f,1.4f,3);
            cam.transform.LookAt(cat.transform.position+Vector3.up*.8f);
            var lightObject=new GameObject("Portrait light"); var light=lightObject.AddComponent<Light>();
            light.type=LightType.Directional; light.cullingMask=1<<30; light.intensity=1.2f; light.transform.rotation=Quaternion.Euler(35,-30,0);
            var rt=new RenderTexture(384,384,24); rt.antiAliasing=4; cam.targetTexture=rt; cam.Render();
            var previous=RenderTexture.active; RenderTexture.active=rt;
            var texture=new Texture2D(384,384,TextureFormat.RGBA32,false); texture.ReadPixels(new Rect(0,0,384,384),0,0); texture.Apply();
            string path=GardenBuilder.Root+"/UI/CatPortrait.png"; File.WriteAllBytes(path,texture.EncodeToPNG());
            RenderTexture.active=previous; cam.targetTexture=null; rt.Release();
            Object.DestroyImmediate(rt); Object.DestroyImmediate(texture); Object.DestroyImmediate(cameraObject); Object.DestroyImmediate(lightObject); Object.DestroyImmediate(cat);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path); importer.textureType=TextureImporterType.Sprite; importer.alphaIsTransparency=true; importer.SaveAndReimport();
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            foreach(var entry in EditorBuildSettings.scenes.Where(s=>s.enabled))
            {
                var scene=EditorSceneManager.OpenScene(entry.path); var canvas=Object.FindObjectOfType<Canvas>();
                foreach(var button in canvas.GetComponentsInChildren<Button>(true))
                {
                    bool primary=new[]{"Play","Next","Resume","Back","Retry"}.Contains(button.name);
                    var image=button.GetComponent<Image>(); image.sprite=null;
                    image.color=primary?new Color(1,.68f,.16f):new Color(.08f,.33f,.35f);
                    button.GetComponentInChildren<Text>().color=primary?new Color(.09f,.16f,.19f):Color.white;
                }
                foreach(var panel in canvas.GetComponentsInChildren<RectTransform>(true).Where(t=>t.name=="Victory panel" || t.name=="Game over panel"))
                {
                    var existing=panel.Find("Cat illustration");
                    var obj=existing!=null?existing.gameObject:new GameObject("Cat illustration",typeof(RectTransform),typeof(Image));
                    obj.transform.SetParent(panel,false); var rect=(RectTransform)obj.transform;
                    rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f); rect.sizeDelta=new Vector2(300,300); rect.anchoredPosition=new Vector2(-590,35);
                    var image=obj.GetComponent<Image>(); image.sprite=sprite; image.preserveAspect=true; image.raycastTarget=false;
                    image.color=panel.name=="Victory panel"?Color.white:new Color(.65f,.72f,.76f,1);
                    var content=panel.Find("Content").GetComponent<Text>(); content.rectTransform.sizeDelta=new Vector2(1120,320); content.rectTransform.anchoredPosition=new Vector2(140,45);
                }
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets(); EditorSceneManager.OpenScene(GardenBuilder.Scenes+"MainMenu.unity");
        }
    }
}
