using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// "choose a blessing": shows 3 random cards after a room is cleared. the game pauses until you pick.
// hook it up: on the room's enemyspawner, add to "on all cleared" > this object > blessingchoiceui.offer
// this script also starts each level: it resets the run (first level only) and gives the start light.
public class BlessingChoiceUI : MonoBehaviour
{
    [System.Serializable]
    public class Card
    {
        public Button button;
        public Image icon;
        public TMP_Text title;
        public TMP_Text description;
    }

    public static bool IsShowing { get; private set; }

    [Header("References (drag in)")]
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private Card[] cards = new Card[3];
    [Tooltip("All the blessings that can show up.")]
    [SerializeField] private Blessing[] pool;
    [SerializeField] private PlayerStats stats;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MenuAudio menuAudio;

    [Header("Level start")]
    [Tooltip("Tick this in the FIRST level (the tutorial) so every new run starts with no blessings.")]
    [SerializeField] private bool resetRunOnStart = false;

    private readonly List<Blessing> offered = new List<Blessing>();
    private float oldTimeScale = 1f;

    private void Awake()
    {
        if (panel == null) Debug.LogError("BlessingChoiceUI: drag in the Panel.", this);
        if (resetRunOnStart) Blessings.Reset();
        IsShowing = false;
        Hide();

        for (int i = 0; i < cards.Length; i++)
        {
            int index = i; // copy for the click
            if (cards[i] != null && cards[i].button != null)
                cards[i].button.onClick.AddListener(() => Pick(index));
        }
    }

    private void Start()
    {
        // blessings carried in from the last level
        ApplyLevelStart();
    }

    private void OnDestroy()
    {
        if (IsShowing) Time.timeScale = 1f;
        IsShowing = false;
    }

    // call this when a room is cleared
    public void Offer()
    {
        if (IsShowing || pool == null) return;

        // pick up to 3 different blessings the player can still take
        var options = new List<Blessing>();
        foreach (Blessing b in pool)
            if (b != null && (b.stackable || !Blessings.Has(b))) options.Add(b);
        if (options.Count == 0) return;

        offered.Clear();
        while (offered.Count < cards.Length && options.Count > 0)
        {
            int r = Random.Range(0, options.Count);
            offered.Add(options[r]);
            options.RemoveAt(r);
        }

        for (int i = 0; i < cards.Length; i++)
        {
            Card c = cards[i];
            if (c == null || c.button == null) continue;
            bool used = i < offered.Count;
            c.button.gameObject.SetActive(used);
            if (!used) continue;
            if (c.icon != null) c.icon.sprite = offered[i].icon;
            if (c.title != null) c.title.text = offered[i].title;
            if (c.description != null) c.description.text = offered[i].description;
        }

        Show();
    }

    private void Pick(int index)
    {
        if (!IsShowing || index >= offered.Count) return;
        Blessing b = offered[index];
        Blessings.Add(b);
        if (menuAudio != null) menuAudio.PlayClick();

        // things that happen right away
        if (stats != null && b.startLight != 0f) stats.ChangeStateMeter(b.startLight);
        if (playerMovement != null) playerMovement.speedMultiplier *= b.moveSpeed;

        Hide();
    }

    private void ApplyLevelStart()
    {
        if (stats != null && Blessings.StartLight != 0f) stats.ChangeStateMeter(Blessings.StartLight);
        if (playerMovement != null) playerMovement.speedMultiplier = Blessings.MoveSpeed;
    }

    private void Show()
    {
        IsShowing = true;
        oldTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        panel.alpha = 1f;
        panel.interactable = true;
        panel.blocksRaycasts = true;

        // select the first card so keyboard and gamepad work too
        if (EventSystem.current != null && cards.Length > 0 && cards[0] != null && cards[0].button != null)
            EventSystem.current.SetSelectedGameObject(cards[0].button.gameObject);
    }

    private void Hide()
    {
        if (panel != null)
        {
            panel.alpha = 0f;
            panel.interactable = false;
            panel.blocksRaycasts = false;
        }
        if (IsShowing) Time.timeScale = oldTimeScale;
        IsShowing = false;
    }
}
