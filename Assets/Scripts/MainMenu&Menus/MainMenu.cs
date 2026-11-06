using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{

    [Header("Panels (CanvasGroups)")]
    [SerializeField] private CanvasGroup mainPanel;
    [SerializeField] private CanvasGroup optionsPanel;
    [SerializeField] private CanvasGroup creditsPanel;

    [Header("Selected first when each panel opens (for keyboard / gamepad)")]
    [SerializeField] private GameObject mainFirstSelected;
    [SerializeField] private GameObject optionsFirstSelected;
    [SerializeField] private GameObject creditsFirstSelected;

    [Header("Transitions")]
    [Tooltip("Full-screen black Image with a CanvasGroup, on top of everything.")]
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private float fadeInTime = 1.2f;
    [SerializeField] private float panelFadeTime = 0.25f;
    [SerializeField] private float panelSlideDistance = 30f;
    [SerializeField] private float startGameFadeTime = 1.4f;

    [Header("Scenes")]
    [Tooltip("Exact name of the gameplay scene")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("Other")]
    [SerializeField] private MenuAudio menuAudio;
    [Tooltip("Hidden in WebGL builds, because browsers can't quit.")]
    [SerializeField] private GameObject quitButton;

    private CanvasGroup current;
    private bool busy;
    private readonly Dictionary<CanvasGroup, Vector2> basePositions = new Dictionary<CanvasGroup, Vector2>();

    private void Awake()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SettingsPanel.ApplySaved();

        foreach (CanvasGroup g in new[] { mainPanel, optionsPanel, creditsPanel })
        {
            if (g == null) continue;
            basePositions[g] = ((RectTransform)g.transform).anchoredPosition;
            MenuUI.SetVisible(g, false);
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        if (quitButton != null) quitButton.SetActive(false);
#endif
        if (mainPanel == null) Debug.LogError("MainMenu: drag in at least the Main Panel.", this);
    }

    private void Start()
    {
        if (fadeOverlay != null)
        {
            MenuUI.SetVisible(fadeOverlay, true);
            fadeOverlay.blocksRaycasts = false;
            StartCoroutine(MenuUI.Fade(fadeOverlay, 1f, 0f, fadeInTime));
        }
        StartCoroutine(SwitchTo(mainPanel, mainFirstSelected, playSound: false));
    }

    private void Update()
    {
        if (busy || current == mainPanel) return;

        bool back = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                 || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
        if (back) Back();
    }

    public void Play()
    {
        if (busy) return;
        busy = true;
        if (menuAudio != null)
        {
            menuAudio.PlayStart();
            menuAudio.KeepAliveThroughLoad(3f); //let the chime finish over the load
        }
        StartCoroutine(StartGameRoutine());
    }

    public void OpenOptions() => Open(optionsPanel, optionsFirstSelected);
    public void OpenCredits() => Open(creditsPanel, creditsFirstSelected);

    public void Back()
    {
        if (busy || current == mainPanel) return;
        if (menuAudio != null) menuAudio.PlayBack();
        StartCoroutine(SwitchTo(mainPanel, mainFirstSelected, playSound: false));
    }

    public void Quit()
    {
        if (busy) return;
        busy = true;
        StartCoroutine(QuitRoutine());
    }

    private void Open(CanvasGroup panel, GameObject first)
    {
        if (busy || panel == null || panel == current) return;
        StartCoroutine(SwitchTo(panel, first, playSound: true));
    }

    private IEnumerator SwitchTo(CanvasGroup next, GameObject firstSelected, bool playSound)
    {
        if (next == null) yield break;
        busy = true;

        if (playSound && menuAudio != null) menuAudio.PlayOpen();

        if (current != null)
            yield return MenuUI.Fade(current, current.alpha, 0f, panelFadeTime,
                                     (RectTransform)current.transform, basePositions[current], panelSlideDistance * 0.5f);

        current = next;
        MenuUI.SelectQuietly(null);
        yield return MenuUI.Fade(next, 0f, 1f, panelFadeTime,
                                 (RectTransform)next.transform, basePositions[next], panelSlideDistance);
        MenuUI.SelectQuietly(firstSelected);
        busy = false;
    }

    private IEnumerator StartGameRoutine()
    {
        if (current != null) current.interactable = false;
        if (fadeOverlay != null) yield return MenuUI.Fade(fadeOverlay, 0f, 1f, startGameFadeTime);
        SceneManager.LoadScene(gameSceneName);
    }

    private IEnumerator QuitRoutine()
    {
        if (menuAudio != null) menuAudio.PlayBack();
        if (fadeOverlay != null) yield return MenuUI.Fade(fadeOverlay, 0f, 1f, 0.6f);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

}
