using NUnit.Framework;

namespace CatsMission.Tests
{
    public class LevelStateTests
    {
        [Test] public void FishCountsOnceAndScoresOnePoint()
        { var s=new LevelState(5,60); Assert.IsTrue(s.Collect(1)); Assert.IsFalse(s.Collect(1)); Assert.AreEqual(1,s.Fish); Assert.AreEqual(1,s.Score); }
        [Test] public void OrdinaryFallPreservesFishAndTime()
        { var s=new LevelState(2,60); s.Collect(1); s.Tick(12); Assert.IsTrue(s.Fall()); Assert.AreEqual(2,s.Lives); Assert.AreEqual(1,s.Fish); Assert.AreEqual(48,s.RemainingTime); }
        [Test] public void ThreeFallsEndTheLevel()
        { var s=new LevelState(1,60); s.Fall(); s.Fall(); Assert.IsFalse(s.Fall()); Assert.AreEqual(0,s.Lives); Assert.AreEqual(LevelPhase.GameOver,s.Phase); }
        [Test] public void ExpiredTimerCannotComplete()
        { var s=new LevelState(1,1); s.Collect(1); s.Tick(2); Assert.AreEqual(0,s.RemainingTime); Assert.IsFalse(s.TryComplete()); }
        [Test] public void HomeRequiresEnoughFish()
        { var s=new LevelState(2,60); s.Collect(1); Assert.IsFalse(s.TryComplete()); s.Collect(2); Assert.IsTrue(s.TryComplete()); }
        [Test] public void ExtraFishIncreaseScore()
        { var s=new LevelState(1,60); s.Collect(1); s.Collect(2); Assert.IsTrue(s.TryComplete()); Assert.AreEqual(2,s.Score); }
        [Test] public void PauseFreezesTimerAndCollecting()
        { var s=new LevelState(1,60); s.TogglePause(); s.Tick(10); Assert.IsFalse(s.Collect(1)); Assert.AreEqual(60,s.RemainingTime); s.TogglePause(); s.Tick(1); Assert.AreEqual(59,s.RemainingTime); }
        [Test] public void CompletionCannotBeTriggeredTwice()
        { var s=new LevelState(1,60); s.Collect(1); Assert.IsTrue(s.TryComplete()); Assert.IsFalse(s.TryComplete()); }
        [Test] public void CampaignAccumulatesCompletedLevelsWithoutDuplicates()
        { var score=new CampaignScore(); score.CompleteLevel(1,7); score.CompleteLevel(1,7); score.CompleteLevel(2,9); Assert.AreEqual(16,score.Total); score.Reset(); Assert.AreEqual(0,score.Total); }
        [Test] public void NewAttemptStartsWithFreshRules()
        { var old=new LevelState(2,60); old.Collect(1); old.Fall(); var next=new LevelState(2,60); Assert.AreEqual(0,next.Fish); Assert.AreEqual(3,next.Lives); Assert.AreEqual(60,next.RemainingTime); }
    }
}
