using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CatsMission.EditorTools
{
    public static class GardenBuilder
    {
        public const string Root = "Assets/CatsMission";
        public const string Scenes = Root + "/Scenes/";
        private static readonly Dictionary<string, Material> palette = new Dictionary<string, Material>();
        private static readonly Color Ink = new Color(.09f,.16f,.19f);
        private static readonly Color Cream = new Color(1,.96f,.84f);
        private static readonly Color Gold = new Color(1,.75f,.27f);
        private static Font font;

        [MenuItem("Cat's Mission/Generate/Playable Vertical Slice")]
        public static void VerticalSlice() => Generate(1);
        [MenuItem("Cat's Mission/Generate/Complete Campaign")]
        public static void CompleteCampaign() => Generate(3);

        private static void Generate(int count)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before generating assets.");
            if (SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Save the current scene first; automation will not discard unsaved work.");
            foreach (string dir in new[] { "Scenes", "Prefabs", "Models", "Materials", "Animations", "Audio", "UI", "Editor", "Tests" })
                Directory.CreateDirectory(Root + "/" + dir);
            AssetDatabase.Refresh();
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            palette.Clear();
            if (!File.Exists(Scenes + "MainMenu.unity")) MakeMenu();
            for (int i=1; i<=count; i++)
                if (!File.Exists(Scenes + "Level" + i + ".unity")) MakeLevel(i);
            var buildScenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(Scenes + "MainMenu.unity", true) };
            for (int i=1; i<=count; i++) buildScenes.Add(new EditorBuildSettingsScene(Scenes + "Level" + i + ".unity", true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
            PlayerSettings.productName = "Cat's Mission";
            PlayerSettings.defaultScreenWidth=1280;
            PlayerSettings.defaultScreenHeight=720;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.resizableWindow=true;
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(Scenes + "MainMenu.unity");
            Debug.Log("CATSMISSION: Generated " + buildScenes.Count + " build scenes; original SampleScene preserved.");
        }
        private static Material Mat(string name, Color color)
        {
            Material result;
            if (palette.TryGetValue(name, out result)) return result;
            string path = Root + "/Materials/" + name + ".mat";
            result=AssetDatabase.LoadAssetAtPath<Material>(path);
            if (result == null)
            {
                result=new Material(Shader.Find("Standard")) { name=name, color=color };
                result.SetFloat("_Glossiness", .12f);
                AssetDatabase.CreateAsset(result,path);
            }
            palette[name]=result;
            return result;
        }
        private static GameObject Shape(string name, PrimitiveType type, Vector3 position, Vector3 scale,
            Material material, Transform parent=null, bool solid=true)
        {
            var go=GameObject.CreatePrimitive(type);
            go.name=name; go.transform.SetParent(parent,false);
            go.transform.localPosition=position; go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=material;
            if (!solid) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
        private static GameObject Group(string name, Transform parent=null)
        { var go=new GameObject(name); go.transform.SetParent(parent,false); return go; }
        private static Material Grass => Mat("Grass",new Color(.36f,.66f,.32f));
        private static Material Wood => Mat("Cedar",new Color(.57f,.32f,.17f));
        private static void Environment()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientMode=AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.72f,.8f,.86f);
            RenderSettings.fog=true; RenderSettings.fogColor=new Color(.7f,.87f,.91f);
            RenderSettings.fogMode=FogMode.Linear; RenderSettings.fogStartDistance=45; RenderSettings.fogEndDistance=110;
            var sun=Group("Sunlight").AddComponent<Light>(); sun.type=LightType.Directional;
            sun.intensity=1.05f; sun.color=new Color(1,.95f,.8f); sun.shadows=LightShadows.Soft;
            sun.transform.rotation=Quaternion.Euler(48,-28,0);
        }
        private static Camera CameraAt(Vector3 position, Vector3 lookAt)
        {
            var go=Group("Main Camera"); go.tag="MainCamera";
            var camera=go.AddComponent<Camera>(); camera.fieldOfView=58;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.7f,.87f,.91f);
            camera.nearClipPlane=.1f; camera.farClipPlane=160;
            go.AddComponent<AudioListener>(); go.transform.position=position; go.transform.LookAt(lookAt);
            return camera;
        }
        private static void Floor(float center, float depth, float width)
        {
            Shape("Garden island",PrimitiveType.Cube,new Vector3(0,-.55f,center),new Vector3(width,1.1f,depth),Grass);
            Shape("Earth edge",PrimitiveType.Cube,new Vector3(0,-1.25f,center),new Vector3(width-.15f,.65f,depth-.15f),
                Mat("Earth",new Color(.43f,.29f,.2f)));
        }
        private static void Fence(Vector3 position, float length, bool sideways=false)
        {
            var root=Group("Picket fence"); root.transform.position=position;
            if(sideways) root.transform.rotation=Quaternion.Euler(0,90,0);
            for(float x=-length/2;x<=length/2;x+=.65f)
                Shape("Picket",PrimitiveType.Cube,new Vector3(x,.65f,0),new Vector3(.18f,1.3f,.18f),
                    Mat("Fence cream",new Color(.96f,.88f,.65f)),root.transform);
            Shape("Rail",PrimitiveType.Cube,new Vector3(0,.7f,0),new Vector3(length,.18f,.16f),Wood,root.transform);
        }
        private static void Box(Vector3 position)
        {
            var root=Group("Wooden crate"); root.transform.position=position;
            Shape("Box",PrimitiveType.Cube,new Vector3(0,.5f,0),Vector3.one,Wood,root.transform);
            var trim=Mat("Crate trim",new Color(.8f,.55f,.27f));
            foreach(float x in new[]{-.43f,.43f})
                Shape("Brace",PrimitiveType.Cube,new Vector3(x,.52f,-.52f),new Vector3(.12f,1.05f,.08f),trim,root.transform,false);
            var cross=Shape("Diagonal brace",PrimitiveType.Cube,new Vector3(0,.5f,-.54f),new Vector3(.12f,1.2f,.08f),trim,root.transform,false);
            cross.transform.localRotation=Quaternion.Euler(0,0,45);
        }
        private static void Tree(Vector3 position, float size)
        {
            var root=Group("Garden tree"); root.transform.position=position;
            Shape("Trunk",PrimitiveType.Cylinder,new Vector3(0,1,0),new Vector3(.4f,1,.4f)*size,Wood,root.transform);
            Shape("Crown",PrimitiveType.Sphere,new Vector3(0,2.8f,0)*size,new Vector3(2.7f,2.9f,2.5f)*size,
                Mat("Leaf",new Color(.18f,.46f,.3f)),root.transform,false);
            Shape("Crown highlight",PrimitiveType.Sphere,new Vector3(.6f,3.25f,-.15f)*size,Vector3.one*1.9f*size,
                Mat("Leaf light",new Color(.46f,.69f,.32f)),root.transform,false);
        }
        private static void Decorations(int level, float length)
        {
            var random=new System.Random(842+level);
            for(int i=0;i<16;i++)
            {
                float side=i%2==0 ? -1:1;
                float z=-4+i*(length+4)/16;
                Tree(new Vector3(side*(level==2?12.9f:9.2f),0,z),.7f+(float)random.NextDouble()*.35f);
            }
            var green=Mat("Stem",new Color(.21f,.4f,.21f));
            for(int i=0;i<65;i++)
            {
                float x=(i%2==0?-1:1)*(6.5f+(float)random.NextDouble()*2);
                float z=-5+(float)random.NextDouble()*(length+7);
                var flower=Group("Wildflower"); flower.transform.position=new Vector3(x,0,z);
                Shape("Stem",PrimitiveType.Cylinder,new Vector3(0,.18f,0),new Vector3(.04f,.18f,.04f),green,flower.transform,false);
                var petal=Mat("Petal"+i%3,i%3==0?new Color(1,.62f,.52f):i%3==1?new Color(.8f,.63f,.95f):Gold);
                Shape("Blossom",PrimitiveType.Sphere,new Vector3(0,.4f,0),new Vector3(.25f,.14f,.25f),petal,flower.transform,false);
            }
        }
        private static Mesh Triangle()
        {
            string path=Root+"/Models/TrianglePrism.asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh!=null) return mesh;
            mesh=new Mesh { name="Original triangle prism" };
            mesh.vertices=new[]{ new Vector3(-.5f,0,-.3f),new Vector3(.5f,0,-.3f),new Vector3(0,1,-.3f),
                new Vector3(-.5f,0,.3f),new Vector3(.5f,0,.3f),new Vector3(0,1,.3f) };
            mesh.triangles=new[]{0,2,1,3,4,5,0,1,4,0,4,3,1,2,5,1,5,4,2,0,3,2,3,5};
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh,path); return mesh;
        }
        private static GameObject Wedge(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var go=Group(name,parent); go.transform.localPosition=position; go.transform.localScale=scale;
            go.AddComponent<MeshFilter>().sharedMesh=Triangle(); go.AddComponent<MeshRenderer>().sharedMaterial=material;
            return go;
        }
        private static PlayerCat Cat(Vector3 position)
        {
            var root=Group("Miso the cat"); root.transform.position=position;
            var character=root.AddComponent<CharacterController>(); character.height=1.25f; character.radius=.35f;
            character.center=new Vector3(0,.64f,0); character.stepOffset=.25f; character.slopeLimit=45; character.skinWidth=.04f;
            var player=root.AddComponent<PlayerCat>();
            var model=Group("Cat visual",root.transform).transform;
            var fur=Mat("Miso ginger",new Color(.93f,.56f,.25f));
            var light=Mat("Miso cream",new Color(1,.89f,.69f));
            var dark=Mat("Miso stripe",new Color(.53f,.26f,.15f));
            Shape("Body",PrimitiveType.Sphere,new Vector3(0,.62f,0),new Vector3(.7f,.65f,1),fur,model,false);
            Shape("Chest",PrimitiveType.Sphere,new Vector3(0,.63f,.36f),new Vector3(.48f,.48f,.45f),light,model,false);
            Shape("Head",PrimitiveType.Sphere,new Vector3(0,1,.4f),new Vector3(.76f,.66f,.62f),fur,model,false);
            for(int s=-1;s<=1;s+=2)
            {
                Wedge("Pointed ear",new Vector3(s*.25f,1.21f,.4f),new Vector3(.28f,.38f,.3f),fur,model);
                Wedge("Pink inner ear",new Vector3(s*.25f,1.25f,.49f),new Vector3(.15f,.21f,.08f),
                    Mat("Ear pink",new Color(.96f,.6f,.58f)),model);
                Shape("Eye",PrimitiveType.Sphere,new Vector3(s*.19f,1.04f,.678f),new Vector3(.15f,.18f,.065f),
                    Mat("Eyes",new Color(.1f,.22f,.19f)),model,false);
                Shape("Eye sparkle",PrimitiveType.Sphere,new Vector3(s*.19f-.025f,1.08f,.712f),Vector3.one*.04f,
                    Mat("Sparkle",Color.white),model,false);
                Shape("Muzzle",PrimitiveType.Sphere,new Vector3(s*.1f,.9f,.69f),new Vector3(.2f,.16f,.12f),light,model,false);
                for(int w=0;w<2;w++)
                {
                    var whisker=Shape("Whisker",PrimitiveType.Cube,new Vector3(s*.27f,.91f-w*.06f,.72f),new Vector3(.28f,.012f,.012f),dark,model,false);
                    whisker.transform.localRotation=Quaternion.Euler(0,0,s*(w==0?8:-8));
                }
            }
            Shape("Nose",PrimitiveType.Sphere,new Vector3(0,.95f,.76f),new Vector3(.095f,.07f,.055f),dark,model,false);
            var paws=new Transform[4];
            for(int i=0;i<4;i++) paws[i]=Shape("Paw "+i,PrimitiveType.Capsule,
                new Vector3(i%2==0?-.23f:.23f,.25f,i<2?.3f:-.3f),new Vector3(.2f,.26f,.22f),light,model,false).transform;
            var tail=Group("Tail",model).transform; tail.localPosition=new Vector3(0,.65f,-.48f);
            var tailMesh=Shape("Tail fur",PrimitiveType.Capsule,new Vector3(0,.28f,-.22f),new Vector3(.18f,.5f,.18f),fur,tail,false);
            tailMesh.transform.localRotation=Quaternion.Euler(-32,0,0);
            for(int i=0;i<3;i++) Shape("Back stripe",PrimitiveType.Sphere,new Vector3(0,.91f,-.28f+i*.2f),new Vector3(.58f,.05f,.075f),dark,model,false);
            player.Configure(model,paws,tail);
            string prefab=Root+"/Prefabs/Miso.prefab";
            if(!File.Exists(prefab)) PrefabUtility.SaveAsPrefabAsset(root,prefab);
            return player;
        }
        private static void Fish(Vector3 position,int id,LevelController level)
        {
            var root=Group("Fish "+id); root.transform.position=position;
            var trigger=root.AddComponent<SphereCollider>(); trigger.isTrigger=true; trigger.radius=.62f;
            var body=root.AddComponent<Rigidbody>(); body.isKinematic=true; body.useGravity=false;
            var visual=Group("Fish visual",root.transform).transform;
            var material=Mat("Fish gold",Gold);
            Shape("Fish body",PrimitiveType.Sphere,Vector3.zero,new Vector3(.6f,.28f,.22f),material,visual,false);
            var fin=Wedge("Tail fin",new Vector3(-.39f,-.15f,0),new Vector3(.36f,.3f,.18f),material,visual);
            fin.transform.localRotation=Quaternion.Euler(0,0,-90);
            Shape("Fish eye",PrimitiveType.Sphere,new Vector3(.17f,.045f,-.11f),Vector3.one*.055f,Mat("Eyes",Ink),visual,false);
            var collectible=root.AddComponent<FishCollectible>(); collectible.Configure(id,level,visual);
        }
        private static void House(Vector3 position,LevelController level)
        {
            var root=Group("Miso's home"); root.transform.position=position;
            Shape("House",PrimitiveType.Cube,new Vector3(0,1.4f,0),new Vector3(3.5f,2.8f,2.6f),Mat("House cream",Cream),root.transform);
            Wedge("Terracotta roof",new Vector3(0,2.8f,0),new Vector3(4.2f,1.5f,5.5f),Mat("Roof",new Color(.83f,.34f,.26f)),root.transform);
            Shape("Door",PrimitiveType.Cube,new Vector3(0,.9f,-1.34f),new Vector3(1.1f,1.8f,.12f),Mat("Door teal",new Color(.12f,.4f,.4f)),root.transform,false);
            Shape("Door knob",PrimitiveType.Sphere,new Vector3(.3f,.85f,-1.45f),Vector3.one*.12f,Mat("Fish gold",Gold),root.transform,false);
            for(int s=-1;s<=1;s+=2)
                Shape("Window",PrimitiveType.Cube,new Vector3(s*1.15f,1.6f,-1.34f),new Vector3(.65f,.65f,.1f),Mat("Window",new Color(.48f,.79f,.85f)),root.transform,false);
            Shape("Welcome mat",PrimitiveType.Cube,new Vector3(0,.025f,-2.5f),new Vector3(3,.05f,2),Mat("Welcome gold",Gold),root.transform,false);
            if(level!=null)
            {
                var goal=Group("Home destination",root.transform); goal.transform.localPosition=new Vector3(0,1,-2.6f);
                var trigger=goal.AddComponent<BoxCollider>(); trigger.isTrigger=true; trigger.size=new Vector3(3,2,2.1f);
                var rigidbody=goal.AddComponent<Rigidbody>(); rigidbody.isKinematic=true; rigidbody.useGravity=false;
                goal.AddComponent<HomeGoal>().Configure(level);
            }
            var sign=Group("Home sign",root.transform); sign.transform.localPosition=new Vector3(0,4.9f,0);
            var text=sign.AddComponent<TextMesh>(); text.text="HOME"; text.anchor=TextAnchor.MiddleCenter;
            text.fontSize=64; text.characterSize=.12f; text.color=Ink; sign.transform.rotation=Quaternion.Euler(20,0,0);
        }
        private static void MakeLevel(int number)
        {
            Environment();
            float end=number==1?30:number==2?48:39;
            if(number<3) Floor((end-3)/2,end+15,number==2?29:23);
            else { Floor(0,16,23); Floor(17,12,23); Floor(34,16,23); }
            Decorations(number,end);
            for(int i=0;i<(int)end;i+=4)
            {
                if(number==3 && (i==8 || i==24)) continue;
                Shape("Path stone",PrimitiveType.Cylinder,new Vector3((i%8==0?-.35f:.35f),.025f,i-3),new Vector3(1.6f,.025f,1.2f),
                    Mat("Path stone",new Color(.82f,.8f,.61f)),null,false);
            }
            var level=Group("Level controller").AddComponent<LevelController>();
            var spawn=Group("Safe spawn").transform; spawn.position=new Vector3(0,.08f,-3);
            var cat=Cat(spawn.position);
            var camera=CameraAt(new Vector3(0,6,-12),cat.transform.position).gameObject.AddComponent<FollowCamera>();
            camera.Configure(cat.transform);
            var hud=MakeHud(number);
            int required=number==1?5:7;
            string title=number==1?"The Sunny Garden":number==2?"The Garden Maze":"The Final Garden Challenge";
            level.Configure(number,title,required,number==1?150:number==2?210:180,cat,hud,spawn,camera);
            if(number==1)
            {
                Vector3[] points={new Vector3(0,.85f,1),new Vector3(2,.85f,6),new Vector3(-3,.85f,11),new Vector3(2,.85f,16),
                    new Vector3(-2,.85f,22),new Vector3(5,1.8f,11),new Vector3(-7,.85f,20)};
                for(int i=0;i<points.Length;i++) Fish(points[i],i+1,level);
                Box(new Vector3(5,0,11)); Box(new Vector3(-4,0,5)); Box(new Vector3(4,0,20));
                Fence(new Vector3(-6,0,14),3); Fence(new Vector3(5,0,25),4);
            }
            else if(number==2)
            {
                var hedge=Mat("Hedge",new Color(.23f,.48f,.28f));
                for(int i=0;i<4;i++)
                {
                    float x=i%2==0?-4:4;
                    Shape("Maze hedge",PrimitiveType.Cube,new Vector3(x,.8f,8+i*10),new Vector3(19,1.6f,1.2f),hedge);
                }
                Vector3[] points={new Vector3(0,.85f,1),new Vector3(9,.85f,8),new Vector3(8,.85f,13),new Vector3(-9,.85f,18),
                    new Vector3(-8,.85f,23),new Vector3(9,.85f,28),new Vector3(8,.85f,33),new Vector3(-9,.85f,38),new Vector3(0,.85f,43)};
                for(int i=0;i<points.Length;i++) Fish(points[i],i+1,level);
                Box(new Vector3(3,0,13)); Box(new Vector3(-3,0,33));
            }
            else
            {
                Vector3[] points={new Vector3(0,.85f,0),new Vector3(2,.85f,5),new Vector3(0,.85f,12),new Vector3(-3,.85f,17),
                    new Vector3(2,.85f,21),new Vector3(0,.85f,27),new Vector3(-3,.85f,31),new Vector3(4,1.85f,33),new Vector3(0,.85f,34)};
                for(int i=0;i<points.Length;i++) Fish(points[i],i+1,level);
                Box(new Vector3(4,0,33)); Box(new Vector3(-2,0,14)); Fence(new Vector3(4,0,18),5);
                Fence(new Vector3(-5,0,30),4);
                foreach(float z in new[]{6.6f,21.6f})
                    Shape("Jump marker",PrimitiveType.Cube,new Vector3(0,.04f,z),new Vector3(3,.08f,.35f),Mat("Jump gold",Gold),null,false);
            }
            Fence(new Vector3(-10.8f,0,-4),5,true); Fence(new Vector3(10.8f,0,-4),5,true);
            House(new Vector3(0,0,end),level);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Scenes+"Level"+number+".unity");
        }
        private static Canvas Canvas()
        {
            var go=new GameObject("Interface",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=go.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=go.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1280,720); scaler.matchWidthOrHeight=.5f;
            new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            return canvas;
        }
        private static RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 size,Vector2 position)
        {
            var go=new GameObject(name,typeof(RectTransform)); var rect=go.GetComponent<RectTransform>();
            rect.SetParent(parent,false); rect.anchorMin=rect.anchorMax=anchor; rect.sizeDelta=size; rect.anchoredPosition=position;
            return rect;
        }
        private static GameObject Panel(string name,Transform parent,Color color,Vector2 size,Vector2 pos,Vector2? anchor=null)
        {
            var rect=Rect(name,parent,anchor??new Vector2(.5f,.5f),size,pos);
            rect.gameObject.AddComponent<Image>().color=color; return rect.gameObject;
        }
        private static Text Text(string name,Transform parent,string value,int size,Color color,Vector2 area,Vector2 pos,
            TextAnchor alignment=TextAnchor.MiddleCenter,Vector2? anchor=null)
        {
            var rect=Rect(name,parent,anchor??new Vector2(.5f,.5f),area,pos);
            var text=rect.gameObject.AddComponent<Text>(); text.font=font; text.text=value; text.fontSize=size;
            text.color=color; text.alignment=alignment; text.raycastTarget=false;
            text.horizontalOverflow=HorizontalWrapMode.Wrap; text.verticalOverflow=VerticalWrapMode.Overflow;
            return text;
        }
        private static Button Button(string name,Transform parent,string label,Vector2 size,Vector2 pos,bool primary=false)
        {
            var go=Panel(name,parent,primary?Gold:new Color(.18f,.31f,.32f),size,pos);
            var button=go.AddComponent<Button>();
            var colors=button.colors; colors.highlightedColor=new Color(1,1,.85f); colors.pressedColor=new Color(.8f,.84f,.74f); button.colors=colors;
            Text("Label",go.transform,label,20,primary?Ink:Cream,size,Vector2.zero);
            return button;
        }
        private static GameObject Overlay(string name,Transform parent,string eyebrow,string title,out Text content)
        {
            var shade=Panel(name,parent,new Color(.04f,.11f,.14f,.9f),new Vector2(1280,720),Vector2.zero);
            var rect=(RectTransform)shade.transform; rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.sizeDelta=Vector2.zero;
            Text("Eyebrow",shade.transform,eyebrow,16,Gold,new Vector2(700,30),new Vector2(0,165));
            content=Text("Content",shade.transform,title,30,Cream,new Vector2(800,190),new Vector2(0,30));
            shade.SetActive(false); return shade;
        }
        private static GameHud MakeHud(int number)
        {
            var canvas=Canvas(); var hud=canvas.gameObject.AddComponent<GameHud>();
            var bar=Panel("Top bar",canvas.transform,new Color(.06f,.16f,.18f,.95f),new Vector2(1280,90),new Vector2(0,-45),new Vector2(.5f,1));
            var heading=Text("Garden title",bar.transform,"",16,Gold,new Vector2(650,28),new Vector2(-270,22),TextAnchor.MiddleLeft);
            var fish=Text("Fish count",bar.transform,"",23,Cream,new Vector2(220,38),new Vector2(-490,-17),TextAnchor.MiddleLeft);
            var lives=Text("Lives",bar.transform,"",23,Cream,new Vector2(180,38),new Vector2(-230,-17),TextAnchor.MiddleLeft);
            var time=Text("Timer",bar.transform,"",23,Cream,new Vector2(220,38),new Vector2(20,-17),TextAnchor.MiddleLeft);
            var score=Text("Score",bar.transform,"",23,Cream,new Vector2(180,38),new Vector2(260,-17),TextAnchor.MiddleLeft);
            var pause=Button("Pause",bar.transform,"PAUSE  /  ESC",new Vector2(175,42),new Vector2(515,0));
            Text("Controls",canvas.transform,"WASD / ARROWS   Move       SPACE   Jump       ESC   Pause",16,Ink,new Vector2(950,32),new Vector2(0,25),TextAnchor.MiddleCenter,new Vector2(.5f,0));
            var message=Text("Message",canvas.transform,"",23,Ink,new Vector2(1100,45),new Vector2(0,-115),TextAnchor.MiddleCenter,new Vector2(.5f,1));
            Text unused;
            var paused=Overlay("Pause panel",canvas.transform,"TAKE A LITTLE BREATHER","PAUSED",out unused);
            var resume=Button("Resume",paused.transform,"KEEP EXPLORING",new Vector2(280,52),new Vector2(0,-85),true);
            var restart=Button("Restart",paused.transform,"RESTART GARDEN",new Vector2(280,46),new Vector2(0,-150));
            var menu=Button("Main menu",paused.transform,"MAIN MENU",new Vector2(280,46),new Vector2(0,-210));
            Text result;
            var completed=Overlay("Victory panel",canvas.transform,"A LITTLE CAT. A BIG ADVENTURE.","",out result);
            var next=Button("Next",completed.transform,number==3?"BACK TO MAIN MENU":"NEXT GARDEN",new Vector2(300,55),new Vector2(0,-120),true);
            var completeMenu=Button("Menu",completed.transform,"MAIN MENU",new Vector2(300,46),new Vector2(0,-185));
            Text failure;
            var lost=Overlay("Game over panel",canvas.transform,"NINE LIVES? LET'S START WITH THREE.","",out failure);
            var retry=Button("Retry",lost.transform,"TRY AGAIN NOW",new Vector2(300,55),new Vector2(0,-120),true);
            var lostMenu=Button("Menu",lost.transform,"MAIN MENU",new Vector2(300,46),new Vector2(0,-185));
            hud.Configure(heading,fish,lives,time,score,message,paused,completed,lost,result,failure,resume,restart,menu,next,completeMenu,retry,lostMenu,pause);
            return hud;
        }
        private static void MakeMenu()
        {
            Environment(); Floor(5,30,30); Decorations(1,20);
            Cat(new Vector3(3,.08f,2)).enabled=false;
            House(new Vector3(5,0,10),null);
            CameraAt(new Vector3(12,9,-11),new Vector3(3,.8f,4));
            var canvas=Canvas();
            var card=Panel("Menu card",canvas.transform,new Color(.07f,.17f,.19f,.97f),new Vector2(470,720),new Vector2(235,0),new Vector2(0,.5f));
            Text("Eyebrow",card.transform,"A GARDEN ADVENTURE",16,Gold,new Vector2(400,35),new Vector2(0,240));
            var title=Text("Game title",card.transform,"Cat's\nMission",66,Cream,new Vector2(380,180),new Vector2(0,130),TextAnchor.MiddleLeft);
            title.fontStyle=FontStyle.Bold;
            Text("Story",card.transform,"Meet Miso. Follow the fish.\nFind your way home.",22,new Color(.73f,.86f,.79f),new Vector2(380,90),new Vector2(0,-5),TextAnchor.MiddleLeft);
            var play=Button("Play",card.transform,"LET'S EXPLORE  >",new Vector2(370,58),new Vector2(0,-115),true);
            var help=Button("Instructions",card.transform,"HOW TO PLAY",new Vector2(370,50),new Vector2(0,-183));
            var quit=Button("Quit",card.transform,"QUIT",new Vector2(370,45),new Vector2(0,-245));
            Text("Footer",card.transform,"3 GARDENS   /   1 LITTLE EXPLORER",14,new Color(.65f,.76f,.7f),new Vector2(400,25),new Vector2(0,-318));
            Text("Caption",canvas.transform,"EVERY ADVENTURE\nSTARTS WITH A PAW.",27,Ink,new Vector2(610,100),new Vector2(-320,70),TextAnchor.MiddleRight,new Vector2(1,0));
            Text content;
            var instructions=Overlay("Instructions panel",canvas.transform,"YOUR FIELD GUIDE","",out content);
            content.fontSize=24;
            content.text="WASD / Arrow keys: move    |    Space: jump\nESC: pause / resume\n\nCollect the required fish, then reach HOME.\nEach fish is worth 1 point. Extra fish also count!\n\nA fall costs 1 life; fish and time are preserved.\nNo lives or no time? The current garden restarts.\nCompleted gardens add to your campaign score.";
            content.rectTransform.sizeDelta=new Vector2(950,300); content.rectTransform.anchoredPosition=new Vector2(0,0);
            var back=Button("Back",instructions.transform,"GOT IT!",new Vector2(280,54),new Vector2(0,-220),true);
            canvas.gameObject.AddComponent<MainMenuView>().Configure(play,help,quit,back,instructions);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Scenes+"MainMenu.unity");
        }
    }
}
