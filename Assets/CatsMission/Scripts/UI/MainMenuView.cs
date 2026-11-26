using UnityEngine;
using UnityEngine.UI;

namespace CatsMission
{
    public sealed class MainMenuView : MonoBehaviour
    {
        [SerializeField] private Button play, instructions, quit, back;
        [SerializeField] private GameObject instructionsPanel;
        public void Configure(Button start, Button help, Button exit, Button close, GameObject panel)
        { play=start; instructions=help; quit=exit; back=close; instructionsPanel=panel; }
        private void Start()
        {
            Time.timeScale=1;
            GameSession.Ensure();
            play.onClick.AddListener(GameSession.Current.NewGame);
            instructions.onClick.AddListener(() => instructionsPanel.SetActive(true));
            back.onClick.AddListener(() => instructionsPanel.SetActive(false));
            quit.onClick.AddListener(Application.Quit);
        }
    }
}
