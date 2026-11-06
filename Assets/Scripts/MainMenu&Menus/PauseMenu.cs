using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("Panels (CanvasGroups)")]
    [Tooltip("The whole pause UI (dark backdrop + panels). Hidden while playing.")]
    [SerializeField] private CanvasGroup pauseRoot;
    [SerializeField] private CanvasGroup pausePanel;
    [SerializeField] private CanvasGroup optionsPanel;

    [Header("Selected first (keyboard / gamepad)")]
    [SerializeField] private GameObject pauseFirstSelected;
    [SerializeField] private GameObject optionsFirstSelected;

    [Header("While paused")]
    [SerializeField] private Behaviour[] disableWhilePaused;
    [SerializeField] private bool pauseGameAudio = true;

    [Header("Transitions")]
    [SerializeField] private float fadeTime = 0.2f;
    [SerializeField] private float panelSlideDistance = 20f;
    [Tooltip("full-screen black CanvasGroup for fading out when leaving.")]
    [SerializeField] private CanvasGroup fadeOverlay;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Other")]
    [SerializeField] private MenuAudio menuAudio;

    public static bool IsPaused { get; private set; }

    private bool[] wasEnabled;
    private bool optionsOpen;
    private bool busy;
    private Vector2 pausePanelPos, optionsPanelPos;

    private void Awake()
    {
        SettingsPanel.ApplySaved();
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (pauseRoot == null || pausePanel == null)
            Debug.LogError("PauseMenu: drag in Pause Root and Pause Panel.", this);

        if (pausePanel != null) pausePanelPos = ((RectTransform)pausePanel.transform).anchoredPosition;
        if (optionsPanel != null) optionsPanelPos = ((RectTransform)optionsPanel.transform).anchoredPosition;

        MenuUI.SetVisible(pauseRoot, false);
        MenuUI.SetVisible(pausePanel, false);
        MenuUI.SetVisible(optionsPanel, false);
        if (fadeOverlay != null) MenuUI.SetVisible(fadeOverlay, false);
    }

    private void Update()
    {
        if (busy) return;

        Keyboard kb = Keyboard.current;
        Gamepad pad = Gamepad.current;

        bool togglePressed = (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame))
                          || (pad != null && pad.startButton.wasPressedThisFrame);
        bool backPressed = pad != null && pad.buttonEast.wasPressedThisFrame;

        if (!IsPaused)
        {
            if (togglePressed) Pause();
        }
        else if (togglePressed || backPressed)
        {
            if (optionsOpen) CloseOptions();
            else Resume();
        }
    }

    private void OnDestroy()
    {
        //never leave the game frozen if this scene unloads while paused.
        if (IsPaused)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            IsPaused = false;
        }
    }

    public void Pause()
    {
        if (IsPaused || busy) return;
        IsPaused = true;
        Time.timeScale = 0f;
        if (pauseGameAudio) AudioListener.pause = true;
        SetGameplayScripts(false);

        if (menuAudio != null) menuAudio.PlayOpen();
        StartCoroutine(OpenRoutine());
    }

    public void Resume()
    {
        if (!IsPaused || busy) return;
        if (menuAudio != null) menuAudio.PlayBack();
        StartCoroutine(ResumeRoutine());
    }

    public void OpenOptions()
    {
        if (busy || optionsOpen || optionsPanel == null) return;
        if (menuAudio != null) menuAudio.PlayOpen();
        StartCoroutine(SwapPanels(pausePanel, pausePanelPos, optionsPanel, optionsPanelPos, optionsFirstSelected, true));
    }

    public void CloseOptions()
    {
        if (busy || !optionsOpen) return;
        if (menuAudio != null) menuAudio.PlayBack();
        StartCoroutine(SwapPanels(optionsPanel, optionsPanelPos, pausePanel, pausePanelPos, pauseFirstSelected, false));
    }

    public void RestartRoom() => StartCoroutine(LeaveRoutine(SceneManager.GetActiveScene().name));
    public void QuitToMainMenu() => StartCoroutine(LeaveRoutine(mainMenuSceneName));

    private IEnumerator OpenRoutine()
    {
        busy = true;
        optionsOpen = false;
        MenuUI.SetVisible(optionsPanel, false);
        StartCoroutine(MenuUI.Fade(pauseRoot, 0f, 1f, fadeTime));
        yield return MenuUI.Fade(pausePanel, 0f, 1f, fadeTime,
                                 (RectTransform)pausePanel.transform, pausePanelPos, panelSlideDistance);
        MenuUI.SelectQuietly(pauseFirstSelected);
        busy = false;
    }

    private IEnumerator ResumeRoutine()
    {
        busy = true;
        MenuUI.SelectQuietly(null);
        if (optionsOpen) StartCoroutine(MenuUI.Fade(optionsPanel, optionsPanel.alpha, 0f, fadeTime));
        StartCoroutine(MenuUI.Fade(pausePanel, pausePanel.alpha, 0f, fadeTime));
        yield return MenuUI.Fade(pauseRoot, pauseRoot.alpha, 0f, fadeTime);
        optionsOpen = false;

        Time.timeScale = 1f;
        AudioListener.pause = false;
        SetGameplayScripts(true);
        IsPaused = false;
        busy = false;
    }

    private IEnumerator SwapPanels(CanvasGroup from, Vector2 fromPos, CanvasGroup to, Vector2 toPos,
                                   GameObject select, bool openingOptions)
    {
        busy = true;
        MenuUI.SelectQuietly(null);
        yield return MenuUI.Fade(from, from.alpha, 0f, fadeTime, (RectTransform)from.transform, fromPos, panelSlideDistance * 0.5f);
        yield return MenuUI.Fade(to, 0f, 1f, fadeTime, (RectTransform)to.transform, toPos, panelSlideDistance);
        optionsOpen = openingOptions;
        MenuUI.SelectQuietly(select);
        busy = false;
    }

    private IEnumerator LeaveRoutine(string sceneName)
    {
        if (busy) yield break;
        busy = true;
        if (menuAudio != null) menuAudio.PlayClick();
        if (pauseRoot != null) pauseRoot.interactable = false;

        if (fadeOverlay != null)
            yield return MenuUI.Fade(fadeOverlay, 0f, 1f, 0.5f);

        Time.timeScale = 1f;
        AudioListener.pause = false;
        IsPaused = false;
        SceneManager.LoadScene(sceneName);
    }

    private void SetGameplayScripts(bool on)
    {
        if (disableWhilePaused == null) return;
        if (!on) wasEnabled = new bool[disableWhilePaused.Length];

        for (int i = 0; i < disableWhilePaused.Length; i++)
        {
            Behaviour b = disableWhilePaused[i];
            if (b == null) continue;
            if (!on)
            {
                wasEnabled[i] = b.enabled;
                b.enabled = false;
            }
            else if (wasEnabled != null && wasEnabled[i])
            {
                b.enabled = true;
            }
        }
    }

}
