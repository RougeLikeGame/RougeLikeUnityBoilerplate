using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OutcomeScreen : MonoBehaviour
{
    [Header("Game references (drag in)")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private ScarcityRemnant boss;

    [Header("UI (CanvasGroups you build)")]
    [Tooltip("The whole outcome UI (dark backdrop). Hidden during play.")]
    [SerializeField] private CanvasGroup root;
    [SerializeField] private CanvasGroup winPanel;
    [SerializeField] private CanvasGroup losePanel;
    [SerializeField] private GameObject winFirstSelected;
    [SerializeField] private GameObject loseFirstSelected;
    [SerializeField] private CanvasGroup fadeOverlay;

    [Header("Text")]
    [SerializeField] private TMP_Text winTitle;
    [SerializeField] private TMP_Text loseTitle;
    [SerializeField] private TMP_Text winStats;
    [SerializeField] private TMP_Text loseStats;
    [SerializeField] private TMP_Text loseTip;
    [SerializeField]
    private string[] loseTips =
    {
        "The Remnant feeds on force.",
        "Not every battle is won with a blade.",
        "Lost souls linger near the Remnant. Perhaps they need you.",
        "When it grows, ask yourself what fed it."
    };

    [Header("Timing")]
    [Tooltip("Wait after the boss transforms, so the player sees it happen.")]
    [SerializeField] private float winDelay = 2.0f;
    [SerializeField] private float loseDelay = 1.2f;
    [SerializeField] private float fadeTime = 0.6f;
    [SerializeField] private float titleRevealTime = 1.6f;
    [Tooltip("Extra letter spacing the title starts with, then settles from (a slow, dramatic reveal).")]
    [SerializeField] private float titleSpreadStart = 40f;

    [Header("While showing")]
    [Tooltip("e.g. PlayerMovement, PlayerAttack, PlayerDebugKeys, BossDebugKeys, EnemyDebugKeys.")]
    [SerializeField] private Behaviour[] disableWhileShowing;
    [SerializeField] private bool calmEnemiesOnWin = true;

    [Header("Scenes & sound")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private MenuAudio menuAudio;

    public static bool IsShowing { get; private set; }

    private bool decided;
    private TMP_Text animTitle;
    private float titleBaseSpacing;
    private float titleTimer;

    private void Awake()
    {
        IsShowing = false;
        MenuUI.SetVisible(root, false);
        MenuUI.SetVisible(winPanel, false);
        MenuUI.SetVisible(losePanel, false);
        if (fadeOverlay != null) MenuUI.SetVisible(fadeOverlay, false);
        if (playerStats == null) Debug.LogError("OutcomeScreen: drag in Player Stats.", this);
    }

    private void OnEnable()
    {
        if (playerStats != null) playerStats.OnDied += ShowLose;
        if (boss != null) boss.OnTransformed += ShowWin;
    }

    private void OnDisable()
    {
        if (playerStats != null) playerStats.OnDied -= ShowLose;
        if (boss != null) boss.OnTransformed -= ShowWin;
    }

    private void OnDestroy() => IsShowing = false;

    public void ShowWin()
    {
        if (decided) return;
        decided = true;
        if (calmEnemiesOnWin)
            foreach (EnemyHealth e in new System.Collections.Generic.List<EnemyHealth>(EnemyHealth.All))
                e.ForceKill();

        if (winStats != null)
            winStats.text = $"Souls comforted: {Helped}     Blows struck: {Struck}     Time: {TimeText}";

        StartCoroutine(ShowRoutine(winPanel, winTitle, winFirstSelected, winDelay, win: true));
    }

    public void ShowLose()
    {
        if (decided) return;
        decided = true;

        if (loseStats != null)
        {
            string scarcity = boss != null && boss.State != RemnantState.Dormant
                ? $"Scarcity remained at {boss.Scarcity:0}     " : "";
            loseStats.text = $"{scarcity}Souls comforted: {Helped}     Time: {TimeText}";
        }
        if (loseTip != null && loseTips != null && loseTips.Length > 0)
            loseTip.text = loseTips[Random.Range(0, loseTips.Length)];

        StartCoroutine(ShowRoutine(losePanel, loseTitle, loseFirstSelected, loseDelay, win: false));
    }

    private IEnumerator ShowRoutine(CanvasGroup panel, TMP_Text title, GameObject first, float delay, bool win)
    {
        IsShowing = true;
        SetGameplay(false);

        yield return new WaitForSecondsRealtime(delay);

        if (menuAudio != null) { if (win) menuAudio.PlayStart(); else menuAudio.PlayBack(); }

        // Start the title reveal.
        animTitle = title;
        if (animTitle != null)
        {
            titleBaseSpacing = animTitle.characterSpacing;
            titleTimer = 0f;
            ApplyTitle(0f);
        }

        StartCoroutine(MenuUI.Fade(root, 0f, 1f, fadeTime));
        RectTransform rt = (RectTransform)panel.transform;
        yield return MenuUI.Fade(panel, 0f, 1f, fadeTime, rt, rt.anchoredPosition, 30f);
        MenuUI.SelectQuietly(first);
    }

    private void Update()
    {
        if (animTitle == null || titleTimer >= titleRevealTime) return;
        titleTimer += Time.unscaledDeltaTime;
        ApplyTitle(Mathf.Clamp01(titleTimer / titleRevealTime));
    }

    private void ApplyTitle(float p)
    {
        float eased = 1f - Mathf.Pow(1f - p, 3f);
        animTitle.characterSpacing = titleBaseSpacing + titleSpreadStart * (1f - eased);
        Color c = animTitle.color;
        c.a = eased;
        animTitle.color = c;
    }

    public void PlayAgain() => StartCoroutine(Leave(SceneManager.GetActiveScene().name));
    public void MainMenu() => StartCoroutine(Leave(mainMenuSceneName));

    private IEnumerator Leave(string scene)
    {
        if (menuAudio != null) menuAudio.PlayClick();
        if (root != null) root.interactable = false;
        if (fadeOverlay != null) yield return MenuUI.Fade(fadeOverlay, 0f, 1f, 0.5f);
        Time.timeScale = 1f;
        AudioListener.pause = false;
        IsShowing = false;
        SceneManager.LoadScene(scene);
    }

    private int Helped => playerStats != null ? playerStats.HelpfulActionsDone : 0;
    private int Struck => playerStats != null ? playerStats.AggressiveActionsDone : 0;

    private static string TimeText
    {
        get
        {
            int s = Mathf.FloorToInt(Time.timeSinceLevelLoad);
            return $"{s / 60}:{s % 60:00}";
        }
    }

    private void SetGameplay(bool on)
    {
        if (disableWhileShowing == null) return;
        foreach (Behaviour b in disableWhileShowing)
            if (b != null) b.enabled = on;
    }
}
