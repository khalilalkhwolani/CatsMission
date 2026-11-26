using System;
using System.Collections.Generic;

namespace CatsMission
{
    public enum LevelPhase { Playing, Paused, Complete, GameOver }

    // Deterministic rules; scene objects and UI observe this single source of truth.
    public sealed class LevelState
    {
        private readonly HashSet<int> collected = new HashSet<int>();
        public int RequiredFish { get; }
        public int Fish => collected.Count;
        public int Score => Fish;
        public int Lives { get; private set; } = 3;
        public float RemainingTime { get; private set; }
        public LevelPhase Phase { get; private set; } = LevelPhase.Playing;
        public string FailureReason { get; private set; } = "";

        public LevelState(int requiredFish, float duration)
        {
            if (requiredFish < 1 || duration <= 0) throw new ArgumentOutOfRangeException();
            RequiredFish = requiredFish;
            RemainingTime = duration;
        }

        public bool Collect(int id) => Phase == LevelPhase.Playing && collected.Add(id);
        public void Tick(float seconds)
        {
            if (Phase != LevelPhase.Playing || seconds <= 0) return;
            RemainingTime = Math.Max(0, RemainingTime - seconds);
            if (RemainingTime <= 0) End("Time ran out");
        }
        public bool Fall()
        {
            if (Phase != LevelPhase.Playing) return false;
            Lives--;
            if (Lives == 0) End("No lives remaining");
            return Lives > 0;
        }
        public bool TryComplete()
        {
            if (Phase != LevelPhase.Playing || Fish < RequiredFish || RemainingTime <= 0) return false;
            Phase = LevelPhase.Complete;
            return true;
        }
        public void TogglePause()
        {
            if (Phase == LevelPhase.Playing) Phase = LevelPhase.Paused;
            else if (Phase == LevelPhase.Paused) Phase = LevelPhase.Playing;
        }
        private void End(string reason) { Phase = LevelPhase.GameOver; FailureReason = reason; }
    }

    public sealed class CampaignScore
    {
        private readonly Dictionary<int, int> scores = new Dictionary<int, int>();
        public int Total { get; private set; }
        public void Reset() { scores.Clear(); Total = 0; }
        public void CompleteLevel(int level, int score)
        {
            // Replace an existing result; repeated goal callbacks cannot inflate the total.
            int previous;
            scores.TryGetValue(level, out previous);
            scores[level] = score;
            Total += score - previous;
        }
    }
}
