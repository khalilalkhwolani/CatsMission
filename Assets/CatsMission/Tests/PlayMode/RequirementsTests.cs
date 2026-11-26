using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace CatsMission.Tests
{
    public class RequirementsTests
    {
        [UnityTearDown] public IEnumerator Cleanup()
        { Time.timeScale=1; yield return SceneManager.LoadSceneAsync("MainMenu"); }
        [UnityTest] public IEnumerator SceneContentMatchesFishAndSimpleObstacleDesign()
        {
            for(int n=1;n<=3;n++)
            {
                yield return SceneManager.LoadSceneAsync("Level"+n); yield return null;
                Assert.AreEqual(new Vector2(1920,1080),Object.FindObjectOfType<CanvasScaler>().referenceResolution);
                Assert.IsFalse(Object.FindObjectsOfType<Transform>().Any(t=>t.name.Contains("Secret golden bell") || t.name.Contains("Timed sprinkler") || t.name.Contains("Paper wrapper")));
                var fish=Object.FindObjectsOfType<FishCollectible>(); Assert.AreEqual(n==1?7:9,fish.Length);
                Assert.IsTrue(fish.All(f=>f.transform.Find("Fish visual/Fish eye")!=null));
                var level=Object.FindObjectOfType<LevelController>();
                Vector3 start=level.Player.transform.position;
                level.Collect(999); level.Player.Teleport(new Vector3(0,.08f,16)); level.Fall(); yield return null;
                Assert.Less(Vector3.Distance(start,level.Player.transform.position),.5f);
                Assert.AreEqual(2,level.State.Lives); Assert.AreEqual(1,level.State.Fish);
            }
        }
    }
}
