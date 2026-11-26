using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatsMission
{
    public sealed class LevelController : MonoBehaviour
    {
        [SerializeField] private int levelNumber = 1;
        [SerializeField] private string levelTitle = "The Sunny Garden";
        [SerializeField] private int requiredFish = 5;
        [SerializeField] private float duration = 150;
        [SerializeField] private PlayerCat player;
        [SerializeField] private GameHud hud;
        [SerializeField] private Transform respawn;
        [SerializeField] private FollowCamera followCamera;
        [SerializeField] private GameAudio audioFeedback;
        private LevelPhase previousPhase;
        private bool warned;
        public void ConfigureAudio(GameAudio feedback) => audioFeedback = feedback;
        public LevelState State { get; private set; }
        public int LevelNumber => levelNumber;
        public string LevelTitle => levelTitle;
        public PlayerCat Player => player;
        public bool CanPlay => State != null && State.Phase == LevelPhase.Playing;

        public void Configure(int number, string title, int fish, float seconds, PlayerCat cat,
            GameHud view, Transform spawn, FollowCamera camera)
        {
            levelNumber = number; levelTitle = title; requiredFish = fish; duration = seconds;
            player = cat; hud = view; respawn = spawn; followCamera = camera;
        }
        private void Awake()
        {
            Time.timeScale = 1;
            GameSession.Ensure();
            State = new LevelState(requiredFish, duration);
            player.Initialize(this, followCamera.transform);
            hud.Initialize(this);
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
            State.Tick(Time.deltaTime);
            if (!warned && CanPlay && State.RemainingTime <= 20)
            { warned = true; if (audioFeedback != null) audioFeedback.Warning(); }
            if (State.Phase != previousPhase)
            {
                if (audioFeedback != null) audioFeedback.SetPhase(State.Phase);
                previousPhase = State.Phase;
            }
        }
        public bool Collect(int id)
        {
            if (!State.Collect(id)) return false;
            if (audioFeedback != null) audioFeedback.Collect();
            hud.ShowMessage(State.Fish >= State.RequiredFish ? "Enough fish! Reach the destination." : "Fish collected! +1", 2);
            return true;
        }
        public void Fall()
        {
            if (!CanPlay) return;
            if (audioFeedback != null) audioFeedback.LoseLife();
            if (State.Fall())
            {
                player.Teleport(respawn.position);
                followCamera.Snap();
                hud.ShowMessage("Life lost. Back to the start; collected fish are kept.", 2.5f);
            }
        }
        public bool ReachHome()
        {
            if (!State.TryComplete())
            {
                if (CanPlay) hud.ShowMessage("Collect " + Mathf.Max(0, State.RequiredFish - State.Fish) + " more fish.", 2);
                return false;
            }
            GameSession.Current.Score.CompleteLevel(levelNumber, State.Score);
            return true;
        }
        public void TogglePause()
        {
            State.TogglePause();
            Time.timeScale = State.Phase == LevelPhase.Paused ? 0 : 1;
        }
        public void Restart() => GameSession.Current.Load(SceneManager.GetActiveScene().name);
        public void NextLevel()
        {
            if (State.Phase != LevelPhase.Complete) return;
            if (levelNumber < 3) GameSession.Current.Load("Level" + (levelNumber + 1));
            else GameSession.Current.MainMenu();
        }
        public void MainMenu() => GameSession.Current.MainMenu();
        public void PlayAgain() => GameSession.Current.NewGame();
        public void JumpFeedback() { if (audioFeedback != null) audioFeedback.Jump(); }
        private void OnDestroy() { Time.timeScale = 1; }
    }
}
