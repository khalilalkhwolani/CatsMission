using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CatsMission.Tests
{
    public class GameplayTests
    {
        private LevelController level;
        [UnitySetUp] public IEnumerator LoadGarden()
        {
            Time.timeScale=1;
            yield return SceneManager.LoadSceneAsync("Level1");
            yield return null;
            level=Object.FindObjectOfType<LevelController>();
            Assert.NotNull(level);
            yield return new WaitForSeconds(.2f);
        }
        [UnityTearDown] public IEnumerator Reset()
        { Time.timeScale=1; yield return SceneManager.LoadSceneAsync("MainMenu"); yield return null; }
        [UnityTest] public IEnumerator MovementJumpAndGroundingUseActualController()
        {
            var cat=level.Player;
            // Own the simulation clock: don't call Step manually AND run Update in the same frame.
            cat.enabled=false;
            for(int i=0;i<30;i++) cat.Step(Vector2.zero,false,1f/60);
            Assert.IsTrue(cat.IsGrounded,"Cat must begin grounded.");
            Vector3 start=cat.transform.position;
            for(int i=0;i<15;i++) { cat.Step(Vector2.up,false,1f/60); yield return null; }
            Assert.Greater(cat.transform.position.z,start.z+.7f);
            float floor=cat.transform.position.y;
            cat.Step(Vector2.zero,true,1f/60);
            for(int i=0;i<6;i++) { cat.Step(Vector2.zero,false,1f/60); yield return null; }
            Assert.Greater(cat.transform.position.y,floor+.2f);
            for(int i=0;i<90;i++) { cat.Step(Vector2.zero,false,1f/60); yield return null; }
            Assert.IsTrue(cat.IsGrounded,"Cat must land after jumping.");
        }
        [UnityTest] public IEnumerator CameraFollowsTheCat()
        {
            var camera=Object.FindObjectOfType<FollowCamera>();
            Vector3 original=camera.transform.position;
            level.Player.Teleport(new Vector3(3,.1f,-2));
            yield return new WaitForSeconds(.5f);
            Assert.Greater(camera.transform.position.x,original.x+1);
            Assert.AreEqual(level.Player.transform,camera.Target);
        }
        [UnityTest] public IEnumerator TriggerCollectionUpdatesHudAndCannotDoubleCollect()
        {
            var fish=Object.FindObjectsOfType<FishCollectible>().OrderBy(f=>f.Id).First();
            level.Player.Teleport(fish.transform.position-Vector3.up*.5f);
            yield return new WaitForSeconds(.2f);
            Assert.AreEqual(1,level.State.Fish);
            Assert.IsFalse(fish.gameObject.activeSelf);
            Assert.IsFalse(fish.TryCollect());
            Assert.That(Object.FindObjectOfType<GameHud>().FishLabel,Does.Contain("1 / 5"));
        }
        [UnityTest] public IEnumerator FallingRespawnsWithoutErasingProgress()
        {
            level.Collect(100); float time=level.State.RemainingTime;
            level.Player.Teleport(new Vector3(0,-8,0)); yield return null; yield return null;
            Assert.AreEqual(2,level.State.Lives); Assert.AreEqual(1,level.State.Fish);
            Assert.Greater(level.Player.transform.position.y,-1);
            Assert.LessOrEqual(level.State.RemainingTime,time);
            Assert.Greater(level.State.RemainingTime,time-1);
        }
        [UnityTest] public IEnumerator PauseFreezesTimerAndResumes()
        {
            level.TogglePause(); float time=level.State.RemainingTime;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(time,level.State.RemainingTime);
            Assert.AreEqual(0,Time.timeScale);
            level.TogglePause(); yield return null; yield return null;
            Assert.Less(level.State.RemainingTime,time);
        }
        [UnityTest] public IEnumerator LosingAllLivesWaitsForRetryAndRestartsCurrentGarden()
        {
            level.Collect(100); level.Fall(); level.Fall(); level.Fall();
            Assert.AreEqual(LevelPhase.GameOver,level.State.Phase);
            yield return new WaitForSecondsRealtime(5.4f);
            Assert.AreSame(level,Object.FindObjectOfType<LevelController>(),"Game over must wait for the player's choice.");
            Object.FindObjectsOfType<Button>().First(b=>b.name=="Retry").onClick.Invoke();
            yield return null; yield return null;
            var restarted=Object.FindObjectOfType<LevelController>();
            Assert.AreNotSame(level,restarted); Assert.AreEqual(3,restarted.State.Lives); Assert.AreEqual(0,restarted.State.Fish);
        }
        [UnityTest] public IEnumerator FinalVictoryPlayAgainResetsCampaign()
        {
            yield return SceneManager.LoadSceneAsync("Level3"); yield return null;
            var final=Object.FindObjectOfType<LevelController>();
            for(int i=0;i<final.State.RequiredFish;i++) final.Collect(400+i);
            Assert.IsTrue(final.ReachHome()); yield return null;
            var next=Object.FindObjectsOfType<Button>().First(b=>b.name=="Next");
            Assert.AreEqual("PLAY AGAIN",next.GetComponentInChildren<Text>().text);
            next.onClick.Invoke(); yield return null; yield return null;
            Assert.AreEqual("Level1",SceneManager.GetActiveScene().name);
            Assert.AreEqual(0,GameSession.Current.Score.Total);
        }
        [UnityTest] public IEnumerator TimeOutShowsGameOver()
        {
            level.State.Tick(1000); yield return null;
            Assert.AreEqual(LevelPhase.GameOver,level.State.Phase);
            Assert.IsTrue(GameObject.Find("Game over panel").activeInHierarchy);
        }
        [UnityTest] public IEnumerator ImportedAudioPausesAndLivesUseHeartIcons()
        {
            var audio=Object.FindObjectOfType<GameAudio>(); Assert.NotNull(audio);
            var sources=audio.GetComponents<AudioSource>();
            Assert.AreEqual(3,sources.Length);
            Assert.AreEqual(2,sources.Count(s=>s.loop && s.clip!=null));
            Assert.IsTrue(sources.Where(s=>s.loop).All(s=>s.isPlaying));
            level.TogglePause(); yield return null;
            Assert.IsTrue(sources.Where(s=>s.loop).All(s=>!s.isPlaying));
            level.TogglePause(); yield return null;
            Assert.IsTrue(sources.Where(s=>s.loop).All(s=>s.isPlaying));
            var hearts=Object.FindObjectsOfType<Image>().Where(i=>i.name.StartsWith("Life heart ")).OrderBy(i=>i.name).ToArray();
            Assert.AreEqual(3,hearts.Length); Assert.IsTrue(hearts.All(i=>i.sprite!=null));
            level.Fall(); yield return null;
            Assert.AreNotEqual(hearts[0].color,hearts[2].color);
            level.State.Tick(1000); yield return null; yield return null;
            Assert.IsTrue(sources.Where(s=>s.loop).All(s=>!s.isPlaying));
        }
        [UnityTest] public IEnumerator GoalAndMenuButtonsNavigateScenes()
        {
            Assert.IsFalse(level.ReachHome());
            for(int i=1;i<=5;i++) level.Collect(200+i);
            Assert.IsTrue(level.ReachHome()); yield return null;
            Assert.AreEqual(LevelPhase.Complete,level.State.Phase);
            Assert.IsTrue(GameObject.Find("Victory panel").activeInHierarchy);
            level.MainMenu(); yield return null; yield return null;
            var help=Object.FindObjectsOfType<Button>().First(b=>b.name=="Instructions"); help.onClick.Invoke();
            Assert.IsTrue(GameObject.Find("Instructions panel").activeInHierarchy);
            var back=Object.FindObjectsOfType<Button>().First(b=>b.name=="Back"); back.onClick.Invoke();
            var play=Object.FindObjectsOfType<Button>().First(b=>b.name=="Play"); play.onClick.Invoke();
            yield return null; yield return null;
            Assert.AreEqual("Level1",SceneManager.GetActiveScene().name);
            Assert.AreEqual(0,GameSession.Current.Score.Total);
        }
    }
}
