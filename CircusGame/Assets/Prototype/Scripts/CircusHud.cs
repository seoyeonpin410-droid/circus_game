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
        [SerializeField] TextMeshProUGUI countdownText;
        [SerializeField] TextMeshProUGUI volumeValueText;
        [SerializeField] GameObject countdownPanel;
        [SerializeField] GameObject bossPanel;
        [SerializeField] Image bossHealthFill;
        [SerializeField] TextMeshProUGUI bossHealthText;
        CircusSession session;
        float announcementUntil;
        bool settingsOpen;
        bool initialized;
        RectTransform countdownRect;Vector2 countdownPosition,countdownSize,framePosition;
        Color countdownColor,frameColor;float countdownFontSize;
        UnityEngine.UI.Image countdownFrame;Transform countdownMask;CanvasGroup bossCanvasGroup;
        bool countdownTextInsideFrame;
        [SerializeField] float bossNamePopupSeconds=.32f,bossNameYOffset=-230;
        public bool BossNameVisible {get;private set;}
        public bool BossHealthVisible=>bossPanel && bossPanel.activeInHierarchy;
        public void Bind(CircusSession value)
        {
            session = value;
            if(countdownText){countdownRect=countdownText.rectTransform;countdownPosition=countdownRect.anchoredPosition;countdownSize=countdownRect.sizeDelta;countdownColor=countdownText.color;countdownFontSize=countdownText.fontSize;countdownMask=countdownText.transform.Find("Artist Countdown Mask");}
            if(countdownPanel){countdownFrame=countdownPanel.GetComponent<UnityEngine.UI.Image>();framePosition=((RectTransform)countdownPanel.transform).anchoredPosition;if(countdownFrame)frameColor=countdownFrame.color;}
            countdownTextInsideFrame=countdownPanel && countdownText && countdownText.transform.IsChildOf(countdownPanel.transform);
            if(bossPanel)bossCanvasGroup=bossPanel.GetComponent<CanvasGroup>();
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
            foreach (var button in GetComponentsInChildren<UnityEngine.UI.Button>(true))
                button.onClick.AddListener(() => CircusAudio.Play(CircusCue.UIClick));
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
            if (volumeValueText) volumeValueText.text = $"{Mathf.RoundToInt(volumeSlider.value * 100)}%";
            healthFill.fillAmount = hp / 100;
            healthFill.color = session.Infected ? new Color(.35f, .8f, .43f) : new Color(.78f, .18f, .23f);
            healthText.text = $"{(session.Infected ? "감염" : "체력")}  {Mathf.CeilToInt(hp)} / 100";
            roundText.text = session.Round == 3 ? "3막 - Boss" : $"제{session.Round}막 / 3";
            remainingText.text = session.Round == 3 ? "도깨비 대장을 쓰러뜨리세요" : $"남은 랑 {session.Remaining:00}   ·   무대 위 {session.Alive:00}";
            scoreText.text = $"점수 {session.Score:0000}";
            rangedLegend.SetActive(session.Round > 1);
            var entry=session.Boss?session.Boss.GetComponent<CircusBossEntrance>():null;
            message.gameObject.SetActive(Time.time < announcementUntil && session.State != ShowState.Menu && session.State != ShowState.Countdown);
            if (countdownText)
            {
                bool intro=session.State==ShowState.BossIntro && entry;
                BossNameVisible=intro && entry.NameAge>=0;
                bool showCountdown=session.CountdownText.Length>0 && session.State!=ShowState.Paused && session.State!=ShowState.Menu;
                countdownText.text=BossNameVisible?"도깨비 대장":session.CountdownText;
                countdownText.gameObject.SetActive(showCountdown || BossNameVisible);if(countdownPanel)countdownPanel.SetActive(showCountdown || BossNameVisible);
                float age=BossNameVisible?entry.NameAge:0;
                float popup=BossNameVisible?Mathf.Clamp01(age/Mathf.Max(.05f,bossNamePopupSeconds)):1;
                float scale=popup<.5f?Mathf.Lerp(.8f,1.05f,popup*2):Mathf.Lerp(1.05f,1,(popup-.5f)*2);
                float alpha=BossNameVisible?Mathf.Clamp01(age/.12f)*(1-entry.ReturnBlend):1;
                countdownRect.localScale=Vector3.one*(BossNameVisible && !countdownTextInsideFrame?scale:1);
                countdownRect.anchoredPosition=BossNameVisible && !countdownTextInsideFrame?new Vector2(0,bossNameYOffset):countdownPosition;
                countdownRect.sizeDelta=BossNameVisible?new Vector2(620,125):countdownSize;
                countdownText.fontSize=BossNameVisible?76:countdownFontSize;
                countdownText.color=new Color(countdownColor.r,countdownColor.g,countdownColor.b,countdownColor.a*alpha);
                if(countdownPanel){var rect=(RectTransform)countdownPanel.transform;rect.localScale=Vector3.one*(BossNameVisible?scale:1);rect.anchoredPosition=BossNameVisible?new Vector2(0,bossNameYOffset):framePosition;}
                if(countdownFrame)countdownFrame.color=new Color(frameColor.r,frameColor.g,frameColor.b,frameColor.a*alpha);
                if(countdownMask)countdownMask.gameObject.SetActive(!intro);
            }

            if (bossPanel)
            {
                bool showBoss = session.Boss && session.Boss.Health > 0 && session.Round == 3 && (!entry || entry.HealthVisible);
                bossPanel.SetActive(showBoss);if(bossCanvasGroup)bossCanvasGroup.alpha=entry?Mathf.Clamp01((entry.NameAge-.16f)/.25f):1;
                if (showBoss)
                {
                    bossHealthFill.fillAmount = session.Boss.Health / session.Boss.MaxHealth;
                    int phase=session.Boss.GetComponent<CircusBoss>().Phase;
                    bossHealthText.text = $"도깨비 대장 · {phase}페이즈  {Mathf.CeilToInt(session.Boss.Health)} / {session.Boss.MaxHealth:0}";
                    bossHealthFill.color=phase==3 ? new Color(.7f,.15f,.65f) : phase==2 ? new Color(.85f,.28f,.18f) : new Color(.08f,.68f,.48f);
                }
            }
            bool victory = session.State == ShowState.Victory;
            endingTitle.text = victory ? "게임 클리어" : "무인의 마지막 곡예";
            endingBody.text = victory
                ? $"{(session.Infected ? "감염된 채로" : "살아남아")} 도깨비 대장을 쓰러뜨렸습니다.\n세 막의 공연 완료 · 최종 점수  {session.Score}"
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
