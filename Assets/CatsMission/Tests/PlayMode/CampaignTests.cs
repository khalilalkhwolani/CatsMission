using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CatsMission.Tests
{
    public class CampaignTests
    {
        [UnityTearDown] public IEnumerator Cleanup()
        { Time.timeScale=1; yield return SceneManager.LoadSceneAsync("MainMenu"); yield return null; }

        [UnityTest] public IEnumerator FourScenesProgressToFinalVictoryAndAccumulateScore()
        {
            yield return SceneManager.LoadSceneAsync("Level1"); yield return null;
            GameSession.Current.Score.Reset();
            int total=0;
            for(int number=1;number<=3;number++)
            {
                var level=Object.FindObjectOfType<LevelController>();
                Assert.AreEqual(number,level.LevelNumber);
                foreach(var fish in Object.FindObjectsOfType<FishCollectible>()) Assert.IsTrue(fish.TryCollect());
                total+=level.State.Score;
                var home=Object.FindObjectOfType<HomeGoal>();
                level.Player.Teleport(home.transform.position-Vector3.up*.7f);
                yield return new WaitForSeconds(.2f);
                Assert.AreEqual(LevelPhase.Complete,level.State.Phase,"Home trigger must complete garden "+number);
                Assert.AreEqual(total,GameSession.Current.Score.Total);
                Assert.IsFalse(level.ReachHome());
                level.NextLevel(); yield return null; yield return null;
            }
            Assert.AreEqual("MainMenu",SceneManager.GetActiveScene().name);
            Assert.AreEqual(25,total);
        }
        [UnityTest] public IEnumerator FinalGardenGapsAreJumpableAndMissedJumpCostsLife()
        {
            yield return SceneManager.LoadSceneAsync("Level3"); yield return null;
            var level=Object.FindObjectOfType<LevelController>();
            // Fixed manual simulation uses the production movement method and real Physics colliders.
            var cat=level.Player; cat.enabled=false;
            foreach(float edge in new[]{7.65f,22.65f})
            {
                cat.Teleport(new Vector3(0,.08f,edge));
                // Teleport starts slightly above the ground. Let gravity settle the real
                // CharacterController before testing a grounded jump.
                Physics.SyncTransforms();
                for(int settle=0;settle<15;settle++) cat.Step(Vector2.zero,false,.02f);
                Assert.IsTrue(cat.IsGrounded);
                cat.Step(Vector2.up,true,.02f);
                for(int i=0;i<42;i++) { cat.Step(Vector2.up,false,.02f); yield return null; }
                Assert.Greater(cat.transform.position.z,edge+3.1f);
                Assert.Greater(cat.transform.position.y,-.3f,"Jump must land on the next island.");
            }
            cat.enabled=true;
            cat.Teleport(new Vector3(6,-7,9.5f)); yield return null; yield return null;
            Assert.AreEqual(2,level.State.Lives);
            Assert.Greater(cat.transform.position.y,-1);
        }
        [UnityTest] public IEnumerator MazeAlternatingOpeningsAreNavigable()
        {
            yield return SceneManager.LoadSceneAsync("Level2"); yield return null;
            var level=Object.FindObjectOfType<LevelController>(); var cat=level.Player; cat.enabled=false;
            for(int wall=0;wall<4;wall++)
            {
                float x=wall%2==0?9:-9; float z=8+wall*10;
                cat.Teleport(new Vector3(x,.08f,z-2));
                for(int i=0;i<40;i++) { cat.Step(Vector2.up,false,.02f); yield return null; }
                Assert.Greater(cat.transform.position.z,z+1,"Maze opening "+wall+" must not be blocked.");
            }
        }
    }
}
