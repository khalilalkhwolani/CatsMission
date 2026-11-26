using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace CatsMission.Tests
{
    public class WalkthroughTests
    {
        private LevelController level;
        private IEnumerator Walk(float x,float z,bool jump=false)
        {
            var cat=level.Player; int steps=0;
            if(jump) cat.Step(Vector2.up,true,.02f);
            while(Vector2.Distance(new Vector2(cat.transform.position.x,cat.transform.position.z),new Vector2(x,z))>.18f && steps++<1200)
            {
                if(level.State.Phase==LevelPhase.Complete) yield break;
                var direction=new Vector2(x-cat.transform.position.x,z-cat.transform.position.z).normalized;
                cat.Step(direction,false,.02f); yield return null;
                Assert.Greater(cat.transform.position.y,-3,"Route fell off the garden.");
            }
            Assert.Less(steps,1200,"Route blocked on level "+level.LevelNumber+" at "+cat.transform.position+" toward "+x+", "+z);
            for(int i=0;i<20;i++) { cat.Step(Vector2.zero,false,.02f); yield return null; }
        }
        private IEnumerator Capture(string name)
        {
            Directory.CreateDirectory("Docs/Testing/Final/screenshots");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/Testing/Final/screenshots/"+name+".png"));
            for(int i=0;i<4;i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { Time.timeScale=1; yield return SceneManager.LoadSceneAsync("MainMenu"); }
        [UnityTest] public IEnumerator KeyboardMovementRouteCompletesAllThreeLevelsWithoutTeleporting()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu"); yield return null;
            yield return Capture("menu");
            Object.FindObjectsOfType<Button>().Single(b=>b.name=="Instructions").onClick.Invoke(); yield return null;
            yield return Capture("instructions");
            GameSession.Current.NewGame(); yield return null; yield return null;
            for(int n=1;n<=3;n++)
            {
                level=Object.FindObjectOfType<LevelController>(); level.Player.enabled=false;
                for(int i=0;i<30;i++) level.Player.Step(Vector2.zero,false,.02f);
                yield return Capture("level"+n);
                if(n==1)
                {
                    level.TogglePause(); yield return null; yield return Capture("pause"); level.TogglePause();
                    yield return Walk(0,1); yield return Walk(2,6); yield return Walk(-3,11); yield return Walk(2,16); yield return Walk(-2,22);
                    yield return Walk(0,27.4f);
                }
                if(n==2)
                {
                    yield return Walk(0,1); yield return Walk(9,5); yield return Walk(9,8); yield return Walk(8,13);
                    yield return Capture("maze-route");
                    yield return Walk(-9,15); yield return Walk(-9,18); yield return Walk(-8,23);
                    yield return Walk(9,25); yield return Walk(9,28); yield return Walk(8,33);
                    yield return Walk(-9,35); yield return Walk(-9,38); yield return Walk(0,43); yield return Walk(0,45.4f);
                }
                if(n==3)
                {
                    yield return Walk(0,0); yield return Walk(2,5); yield return Walk(0,7.4f);
                    yield return Capture("gap-approach");
                    yield return Walk(0,12,true); yield return Walk(-3,17); yield return Walk(2,21); yield return Walk(0,22.4f);
                    yield return Walk(0,27,true); yield return Walk(-3,31); yield return Walk(0,34); yield return Walk(0,36.4f);
                }
                yield return null;
                Assert.AreEqual(LevelPhase.Complete,level.State.Phase,"Finish using real movement and collectible/goal triggers.");
                Assert.AreEqual(3,level.State.Lives);
                yield return Capture(n==3?"victory":"complete"+n);
                if(n<3) { level.NextLevel(); yield return null; yield return null; }
            }
            level.PlayAgain(); yield return null; yield return null;
            level=Object.FindObjectOfType<LevelController>(); level.State.Tick(1000); yield return null;
            yield return Capture("game-over");
        }
    }
}
