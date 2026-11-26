using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatsMission
{
    public sealed class GameSession : MonoBehaviour
    {
        public static GameSession Current { get; private set; }
        public CampaignScore Score { get; } = new CampaignScore();
        public static GameSession Ensure()
        {
            if (Current == null) new GameObject("GameSession").AddComponent<GameSession>();
            return Current;
        }
        private void Awake()
        {
            if (Current != null && Current != this) { Destroy(gameObject); return; }
            Current = this;
            DontDestroyOnLoad(gameObject);
        }
        private void OnDestroy() { if (Current == this) Current = null; }
        public void NewGame() { Score.Reset(); Load("Level1"); }
        public void Load(string scene) { Time.timeScale = 1; SceneManager.LoadScene(scene); }
        public void MainMenu() => Load("MainMenu");
    }
}
