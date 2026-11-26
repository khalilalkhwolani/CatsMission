using UnityEngine;
using UnityEngine.UI;

namespace CatsMission
{
    public sealed class GameHud : MonoBehaviour
    {
        [SerializeField] private Text heading, fish, lives, timer, score, message;
        [SerializeField] private GameObject pausePanel, completePanel, gameOverPanel;
        [SerializeField] private Text completeText, gameOverText;
        [SerializeField] private Button resume, restart, menu, next, completeMenu, retry, lostMenu, pause;
        private LevelController level;
        private float messageUntil;
        [SerializeField] private Image[] hearts;
        public void ConfigureHearts(Image[] icons) => hearts = icons;
        public string FishLabel => fish.text;
        public void Configure(Text title, Text fishText, Text livesText, Text clock, Text scoreText, Text toast,
            GameObject paused, GameObject completed, GameObject lost, Text result, Text failure,
            Button resumeButton, Button restartButton, Button menuButton, Button nextButton,
            Button completeMenuButton, Button retryButton, Button lostMenuButton, Button pauseButton)
        {
            heading=title; fish=fishText; lives=livesText; timer=clock; score=scoreText; message=toast;
            pausePanel=paused; completePanel=completed; gameOverPanel=lost; completeText=result; gameOverText=failure;
            resume=resumeButton; restart=restartButton; menu=menuButton; next=nextButton;
            completeMenu=completeMenuButton; retry=retryButton; lostMenu=lostMenuButton; pause=pauseButton;
        }
        public void Initialize(LevelController owner)
        {
            level = owner;
            heading.text = "0" + level.LevelNumber + " / " + level.LevelTitle.ToUpperInvariant();
            resume.onClick.AddListener(level.TogglePause); restart.onClick.AddListener(level.Restart);
            menu.onClick.AddListener(level.MainMenu);
            if (level.LevelNumber == 3)
            { next.GetComponentInChildren<Text>().text = "PLAY AGAIN"; next.onClick.AddListener(level.PlayAgain); }
            else next.onClick.AddListener(level.NextLevel);
            completeMenu.onClick.AddListener(level.MainMenu); retry.onClick.AddListener(level.Restart);
            lostMenu.onClick.AddListener(level.MainMenu); pause.onClick.AddListener(level.TogglePause);
            ShowMessage("Collect " + level.State.RequiredFish + " fish, then reach the destination.", 4);
            Refresh();
        }
        public void ShowMessage(string value, float seconds) { message.text=value; messageUntil=Time.unscaledTime + seconds; }
        private void Update() { if (level != null) Refresh(); }
        public void Refresh()
        {
            var state = level.State;
            fish.text = "FISH  " + state.Fish + " / " + state.RequiredFish;
            lives.text = "LIVES  " + state.Lives;
            if (hearts != null && hearts.Length > 0)
            {
                lives.enabled = false;
                for (int i=0; i<hearts.Length; i++)
                    hearts[i].color = i < state.Lives ? new Color(1,.4f,.4f) : new Color(.3f,.38f,.4f);
            }
            int seconds = Mathf.CeilToInt(state.RemainingTime);
            timer.text = string.Format("TIME  {0:00}:{1:00}", seconds / 60, seconds % 60);
            timer.color = seconds <= 20 ? new Color(1,.43f,.36f) : Color.white;
            score.text = "SCORE  " + state.Score;
            if(Time.unscaledTime >= messageUntil)
                message.text = state.Fish >= state.RequiredFish ? "Reach the destination" : "Collect fish, then reach the destination";
            message.gameObject.SetActive(state.Phase == LevelPhase.Playing);
            pausePanel.SetActive(state.Phase == LevelPhase.Paused);
            completePanel.SetActive(state.Phase == LevelPhase.Complete);
            gameOverPanel.SetActive(state.Phase == LevelPhase.GameOver);
            completeText.text = (level.LevelNumber == 3 ? "YOU WIN!\nMission Completed" : "LEVEL COMPLETE!") +
                "\n\nLevel score: " + state.Score + "\nTotal score: " + GameSession.Current.Score.Total;
            gameOverText.text = "GAME OVER\n" + state.FailureReason + "\nChoose Retry to try this garden again.\nCompleted gardens keep their scores.";
        }
    }
}
