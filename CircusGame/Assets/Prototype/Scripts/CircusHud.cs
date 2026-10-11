using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CircusPrototype
{
    public sealed class CircusHud : MonoBehaviour
    {
        [SerializeField] GameObject menu;
        [SerializeField] GameObject gameplay;
        [SerializeField] GameObject pause;
        [SerializeField] GameObject ending;
        [SerializeField] GameObject settings;
        [SerializeField] GameObject menuShowcase;
        [SerializeField] Camera previewCamera;
        [SerializeField] TextMeshProUGUI healthText;
        [SerializeField] TextMeshProUGUI roundText;
        [SerializeField] TextMeshProUGUI remainingText;
        [SerializeField] TextMeshProUGUI scoreText;
        [SerializeField] TextMeshProUGUI message;
        [SerializeField] TextMeshProUGUI endingTitle;
        [SerializeField] TextMeshProUGUI endingBody;
        [SerializeField] Image healthFill;
        [SerializeField] GameObject rangedLegend;
        [SerializeField] Slider volumeSlider;
        [SerializeField] Toggle fullscreenToggle;
        [SerializeField] Button startButton;
        [SerializeField] Button quitButton;
        [SerializeField] Button settingsButton;
        [SerializeField] Button settingsCloseButton;
        [SerializeField] Button pauseButton;
        [SerializeField] Button resumeButton;
        [SerializeField] Button pauseMenuButton;
        [SerializeField] Button retryButton;
        [SerializeField] Button endingMenuButton;
        CircusSession session;
        float announcementUntil;
        bool settingsOpen;
        bool initialized;
        public void Bind(CircusSession value)
        {
            session = value;
            startButton.onClick.AddListener(session.BeginShow);
            quitButton.onClick.AddListener(session.Quit);
            settingsButton.onClick.AddListener(() => { settingsOpen = true; RefreshPanels(); });
            settingsCloseButton.onClick.AddListener(CloseSettings);
            pauseButton.onClick.AddListener(session.TogglePause);
            resumeButton.onClick.AddListener(session.Resume);
            pauseMenuButton.onClick.AddListener(session.ReturnToMenu);
            retryButton.onClick.AddListener(session.BeginShow);
            endingMenuButton.onClick.AddListener(session.ReturnToMenu);
            volumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("Circus.Volume", .7f));
            AudioListener.volume = volumeSlider.value;
            volumeSlider.onValueChanged.AddListener(v => { AudioListener.volume = v; PlayerPrefs.SetFloat("Circus.Volume", v); });
            fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
            fullscreenToggle.onValueChanged.AddListener(v => { Screen.fullScreen = v; });
            initialized = true;
            RefreshPanels();
        }
        public void CloseSettings() { settingsOpen = false; if (initialized) RefreshPanels(); }
        public void Announce(string text, float seconds)
        {
            message.text = text;
            announcementUntil = seconds <= 0 ? float.PositiveInfinity : Time.time + seconds;
        }
        void Update()
        {
            if (!initialized) return;
            RefreshPanels();
            var hp = session.Player.Health;
            healthFill.fillAmount = hp / 100;
            healthFill.color = session.Infected ? new Color(.35f, .8f, .43f) : new Color(.78f, .18f, .23f);
            healthText.text = $"{(session.Infected ? "감염" : "체력")}  {Mathf.CeilToInt(hp)} / 100";
            roundText.text = $"제{session.Round}막 / 2";
            remainingText.text = $"남은 랑 {session.Remaining:00}   ·   무대 위 {session.Alive:00}";
            scoreText.text = $"점수 {session.Score:0000}";
            rangedLegend.SetActive(session.Round > 1);
            message.gameObject.SetActive(Time.time < announcementUntil && session.State != ShowState.Menu);
            bool victory = session.State == ShowState.Victory;
            endingTitle.text = victory ? "공연 종료" : "무인의 마지막 곡예";
            endingBody.text = victory
                ? $"{(session.Infected ? "감염된 채로" : "살아남아")} 두 막을 마쳤습니다.\n최종 점수  {session.Score}"
                : $"무대는 다음 무인을 기다립니다.\n제{session.Round}막 · 점수  {session.Score}";
        }
        void RefreshPanels()
        {
            if (menuShowcase) menuShowcase.SetActive(session.State == ShowState.Menu);
            if (previewCamera) previewCamera.enabled = session.State == ShowState.Menu;
            session.Player.SetPresentationVisible(session.State != ShowState.Menu);
            menu.SetActive(session.State == ShowState.Menu && !settingsOpen);
            settings.SetActive(settingsOpen && session.State == ShowState.Menu);
            gameplay.SetActive(session.State != ShowState.Menu);
            pause.SetActive(session.State == ShowState.Paused);
            ending.SetActive(session.State == ShowState.Victory || session.State == ShowState.Defeat);
        }
    }
}
