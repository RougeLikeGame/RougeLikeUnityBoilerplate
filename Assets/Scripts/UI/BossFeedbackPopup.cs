using UnityEngine;
using TMPro;

// pops up "fed!" or "starved!" over the boss, so players can see what their actions do.
// the first time each one happens it also shows a short hint line under it.
public class BossFeedbackPopup : MonoBehaviour
{
    [SerializeField] private ScarcityRemnant boss;
    [SerializeField] private TMP_Text popupText;
    [Tooltip("Optional smaller text for the one-time hints.")]
    [SerializeField] private TMP_Text hintText;

    [Header("Words")]
    [SerializeField] private string fedWord = "FED!";
    [SerializeField] private string starvedWord = "STARVED!";
    [SerializeField] private string firstFedHint = "your blows feed it";
    [SerializeField] private string firstStarvedHint = "kindness starves it";

    [Header("Look")]
    [SerializeField] private Color fedColor = new Color(1f, 0.3f, 0.2f);
    [SerializeField] private Color starvedColor = new Color(0.55f, 0.9f, 1f);
    [SerializeField] private float showTime = 0.9f;
    [SerializeField] private float hintTime = 2.5f;
    [SerializeField] private float riseDistance = 0.6f;

    private Vector3 popupBase;
    private float popupTimer;
    private float hintTimer;
    private bool shownFedHint;
    private bool shownStarvedHint;

    private void Awake()
    {
        if (boss == null || popupText == null)
            Debug.LogError("BossFeedbackPopup: drag in Boss and Popup Text.", this);
        if (popupText != null) popupBase = popupText.transform.localPosition;
        SetAlpha(popupText, 0f);
        SetAlpha(hintText, 0f);
    }

    private void OnEnable()
    {
        if (boss == null) return;
        boss.OnFed += HandleFed;
        boss.OnWeakened += HandleStarved;
    }

    private void OnDisable()
    {
        if (boss == null) return;
        boss.OnFed -= HandleFed;
        boss.OnWeakened -= HandleStarved;
    }

    private void HandleFed()
    {
        Show(fedWord, fedColor);
        if (!shownFedHint) { shownFedHint = true; ShowHint(firstFedHint, fedColor); }
    }

    private void HandleStarved()
    {
        Show(starvedWord, starvedColor);
        if (!shownStarvedHint) { shownStarvedHint = true; ShowHint(firstStarvedHint, starvedColor); }
    }

    private void Show(string word, Color color)
    {
        if (popupText == null) return;
        popupText.text = word;
        popupText.color = color;
        popupTimer = showTime;
    }

    private void ShowHint(string line, Color color)
    {
        if (hintText == null || string.IsNullOrEmpty(line)) return;
        hintText.text = line;
        hintText.color = color;
        hintTimer = hintTime;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (popupText != null)
        {
            popupTimer = Mathf.Max(0f, popupTimer - dt);
            float p = 1f - popupTimer / showTime; // 0 at start, 1 at end
            popupText.transform.localPosition = popupBase + Vector3.up * riseDistance * p;
            // quick pop in, then fade
            float scale = p < 0.15f ? Mathf.Lerp(1.4f, 1f, p / 0.15f) : 1f;
            popupText.transform.localScale = Vector3.one * scale;
            SetAlpha(popupText, popupTimer > 0f ? Mathf.Clamp01(popupTimer / (showTime * 0.4f)) : 0f);
        }

        if (hintText != null)
        {
            hintTimer = Mathf.Max(0f, hintTimer - dt);
            SetAlpha(hintText, Mathf.Clamp01(hintTimer / 0.5f));
        }
    }

    private static void SetAlpha(TMP_Text t, float a)
    {
        if (t == null) return;
        Color c = t.color;
        c.a = a;
        t.color = c;
    }
}
