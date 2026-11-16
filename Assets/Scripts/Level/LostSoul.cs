using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

// lost soul: the helpful action.
// walk up to it and hold interact (e) to comfort it. this weakens the boss and raises your state meter.
// letting go, walking away or getting hit cancels it, so helping is a risk and not a free action.
// after it's comforted it fades away, rests, and (with a soul spawner) comes back somewhere else.
// with no boss dragged in (like in the tutorial) it just raises the state meter.
public class LostSoul : MonoBehaviour
{
    private enum State { Hidden, Ready, Comforting, Helped, Resting, Freed }

    [Header("References (drag in)")]
    [Tooltip("Leave empty in the tutorial: comforting then only raises the State Meter.")]
    [SerializeField] private ScarcityRemnant boss;
    [SerializeField] private Transform player;
    [Tooltip("Optional: slows the player while they comfort.")]
    [SerializeField] private PlayerMovement playerMovement;
    [Tooltip("The soul's sprite (a child, so it can bob and rise).")]
    [SerializeField] private SpriteRenderer body;
    [Tooltip("Optional soft glow behind the soul.")]
    [SerializeField] private SpriteRenderer glow;
    [Tooltip("Optional world-space TextMeshPro prompt above the soul.")]
    [SerializeField] private TMP_Text prompt;
    [Tooltip("Optional world-space Image (Image Type: Filled, Radial 360) that fills while you hold.")]
    [SerializeField] private Image holdRing;
    [SerializeField] private InputActionAsset inputActions;
    [Tooltip("Optional: moves the soul to a new spot each time it rests.")]
    [SerializeField] private SoulSpawner spawner;

    [Header("Animation (optional)")]
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private float fps = 6f;
    [SerializeField] private float bobAmount = 0.08f;
    [SerializeField] private float bobSpeed = 2f;

    [Header("Help")]
    [SerializeField] private float interactRadius = 1.1f;
    [Tooltip("Seconds you must hold Interact to comfort the soul.")]
    [SerializeField] private float holdTime = 1.2f;
    [Tooltip("Player speed while comforting (0 = can't move, 1 = normal).")]
    [SerializeField, Range(0f, 1f)] private float moveSpeedWhileComforting = 0.3f;
    [Tooltip("How strong one comfort is (1 = one normal helpful action).")]
    [SerializeField] private float helpStrength = 1f;
    [Tooltip("State Meter gained when there is NO boss (tutorial).")]
    [SerializeField] private float stateGainWithoutBoss = 10f;
    [Tooltip("Seconds the soul rests before it can be helped again.")]
    [SerializeField] private float restTime = 14f;
    [SerializeField] private float helpedAnimTime = 0.8f;
    [Tooltip("Only usable while the boss fight is active. Ignored when there is no boss.")]
    [SerializeField] private bool onlyDuringEncounter = true;
    [Tooltip("Hide the soul completely until the boss wakes up.")]
    [SerializeField] private bool hideUntilEncounter = true;

    [Header("Prompt")]
    [Tooltip("{0} becomes the player's Interact key.")]
    [SerializeField] private string promptFormat = "hold {0}  comfort";

    [Header("Colors")]
    [SerializeField] private Color readyColor = new Color(0.65f, 0.9f, 1f, 0.85f);
    [SerializeField] private Color restingColor = new Color(0.6f, 0.65f, 0.75f, 0.25f);
    [SerializeField] private Color helpedColor = new Color(1f, 0.85f, 0.45f, 1f);
    [SerializeField] private Color glowColor = new Color(0.55f, 0.85f, 1f, 0.5f);

    [Header("Sound (optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip helpClip;
    [SerializeField] private AudioClip cancelClip;
    [SerializeField] private AudioClip readyAgainClip;

    public bool IsReady => state == State.Ready;

    // blessings can make comforting faster and souls come back sooner
    private float HoldTime => holdTime / Mathf.Max(0.1f, Blessings.ComfortSpeed);
    private float RestTime => restTime * Blessings.SoulRestTime;
    public event Action OnComforted;

    // only one soul can be comforted at a time
    private static LostSoul activeSoul;

    private State state = State.Ready;
    private float timer;
    private float holdTimer;
    private InputAction interact;
    private bool createdAction;
    private PlayerStats playerStats;
    private Vector3 bodyBasePos;
    private Vector3 bodyBaseScale;
    private float promptAlpha;
    private float animTime;
    private float savedSpeed = 1f;

    private void Awake()
    {
        if (player == null || body == null)
            Debug.LogError("LostSoul: drag in Player and Body.", this);
        if (player != null) playerStats = player.GetComponentInParent<PlayerStats>();

        if (inputActions != null)
            interact = inputActions.FindAction("Player/Interact", throwIfNotFound: false);
        if (interact == null)
        {
            interact = new InputAction("Interact", InputActionType.Button);
            interact.AddBinding("<Keyboard>/e");
            interact.AddBinding("<Gamepad>/buttonSouth");
            createdAction = true;
        }

        if (body != null)
        {
            bodyBasePos = body.transform.localPosition;
            bodyBaseScale = body.transform.localScale;
        }
        SetPromptAlpha(0f);
        SetRing(0f);

        if (boss != null && hideUntilEncounter) state = State.Hidden;
    }

    private void OnEnable()
    {
        interact?.Enable();
        if (boss != null)
        {
            boss.OnTransformed += HandleBossTransformed;
            boss.OnEncounterStarted += HandleEncounterStarted;
        }
        if (playerStats != null) playerStats.OnDamaged += HandlePlayerHit;
    }

    private void OnDisable()
    {
        if (boss != null)
        {
            boss.OnTransformed -= HandleBossTransformed;
            boss.OnEncounterStarted -= HandleEncounterStarted;
        }
        if (playerStats != null) playerStats.OnDamaged -= HandlePlayerHit;
        if (state == State.Comforting) Cancel(false);
    }

    private void OnDestroy()
    {
        if (createdAction) interact?.Dispose();
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        animTime += dt;
        AnimateIdle();

        bool blocked = PauseMenu.IsPaused || OutcomeScreen.IsShowing;
        bool inRange = player != null && Vector2.Distance(player.position, transform.position) <= interactRadius;
        bool fightOk = boss == null || !onlyDuringEncounter || boss.IsActive;
        bool canUse = state == State.Ready && !blocked && inRange && fightOk
                      && (activeSoul == null || activeSoul == this);

        // prompt fades in and out
        promptAlpha = Mathf.MoveTowards(promptAlpha, canUse || state == State.Comforting ? 1f : 0f, dt * 6f);
        SetPromptAlpha(promptAlpha);
        if (canUse && prompt != null) prompt.text = string.Format(promptFormat, KeyName());

        // start holding
        if (canUse && interact != null && interact.WasPressedThisFrame())
            StartComfort();

        switch (state)
        {
            case State.Hidden:
                SetColors(new Color(readyColor.r, readyColor.g, readyColor.b, 0f), 0f);
                break;

            case State.Ready:
                SetColors(readyColor, glowColor.a * (0.7f + 0.3f * Mathf.Sin(Time.time * 3f)));
                SetRise(0f, 1f);
                break;

            case State.Comforting:
                // cancel if you let go, walk away, or the fight ends
                if (blocked || !inRange || !fightOk || interact == null || !interact.IsPressed())
                {
                    Cancel(true);
                    break;
                }
                holdTimer += dt;
                float h = Mathf.Clamp01(holdTimer / HoldTime);
                SetRing(h);
                SetColors(Color.Lerp(readyColor, helpedColor, h), glowColor.a * (1f + h));
                SetRise(h * 0.15f, 1f + h * 0.1f);
                if (holdTimer >= HoldTime) Help();
                break;

            case State.Helped:
                timer -= dt;
                float p = 1f - Mathf.Clamp01(timer / helpedAnimTime);
                // glow gold, float up and fade out
                SetColors(Color.Lerp(helpedColor, new Color(helpedColor.r, helpedColor.g, helpedColor.b, 0f), p), glowColor.a * (1f - p) * 1.5f);
                SetRise(p * 0.6f, 1f + p * 0.3f);
                if (timer <= 0f) StartResting();
                break;

            case State.Resting:
                timer -= dt;
                float r = 1f - Mathf.Clamp01(timer / RestTime);
                // slowly fades back in
                SetColors(Color.Lerp(new Color(restingColor.r, restingColor.g, restingColor.b, 0f), restingColor, r), 0f);
                SetRise(0f, Mathf.Lerp(0.6f, 1f, r));
                if (timer <= 0f)
                {
                    state = State.Ready;
                    Play(readyAgainClip);
                }
                break;

            case State.Freed:
                SetColors(helpedColor, glowColor.a);
                SetRise(0.1f + 0.05f * Mathf.Sin(Time.time * 1.5f), 1f);
                break;
        }
    }

    private void StartComfort()
    {
        state = State.Comforting;
        activeSoul = this;
        holdTimer = 0f;
        if (playerMovement != null)
        {
            savedSpeed = playerMovement.speedMultiplier;
            playerMovement.speedMultiplier = savedSpeed * moveSpeedWhileComforting;
        }
    }

    private void EndComfort()
    {
        if (activeSoul == this) activeSoul = null;
        if (playerMovement != null) playerMovement.speedMultiplier = savedSpeed;
        SetRing(0f);
    }

    private void Cancel(bool playSound)
    {
        EndComfort();
        state = State.Ready;
        if (playSound) Play(cancelClip);
    }

    private void Help()
    {
        EndComfort();

        float strength = helpStrength * Blessings.HelpStrength;
        if (boss != null) boss.ReceiveHelp(strength);
        else if (playerStats != null)
        {
            playerStats.ChangeStateMeter(stateGainWithoutBoss * strength);
            playerStats.RegisterHelpfulAction();
        }

        Play(helpClip);
        state = State.Helped;
        timer = helpedAnimTime;
        OnComforted?.Invoke();
    }

    private void StartResting()
    {
        state = State.Resting;
        timer = RestTime;
        // it's invisible right now, so this is a good moment to move it
        if (spawner != null) transform.position = spawner.PickNewSpot(this);
    }

    private void HandlePlayerHit(float amount)
    {
        if (state == State.Comforting) Cancel(true);
    }

    private void HandleEncounterStarted()
    {
        if (state == State.Hidden) state = State.Ready;
    }

    private void HandleBossTransformed()
    {
        if (state == State.Comforting) EndComfort();
        state = State.Freed; // every soul is at peace once the boss transforms
    }

    // the key name the player actually uses (works with rebinding and other keyboard layouts)
    private string KeyName()
    {
        string s = KeybindManager.KeyName(interact);
        return string.IsNullOrEmpty(s) ? "E" : s;
    }


    private void AnimateIdle()
    {
        if (body == null) return;
        if (idleFrames != null && idleFrames.Length > 0)
            body.sprite = idleFrames[Mathf.FloorToInt(animTime * fps) % idleFrames.Length];
    }

    private void SetRise(float rise, float scale)
    {
        if (body == null) return;
        float bob = Mathf.Sin(Time.time * bobSpeed + transform.position.x) * bobAmount;
        body.transform.localPosition = bodyBasePos + Vector3.up * (rise + bob);
        body.transform.localScale = bodyBaseScale * scale;
    }

    private void SetColors(Color bodyColor, float glowAlpha)
    {
        if (body != null) body.color = bodyColor;
        if (glow != null)
        {
            Color g = state == State.Helped || state == State.Freed ? helpedColor : glowColor;
            g.a = Mathf.Clamp01(glowAlpha);
            glow.color = g;
        }
    }

    private void SetPromptAlpha(float a)
    {
        if (prompt == null) return;
        Color c = prompt.color;
        c.a = a;
        prompt.color = c;
    }

    private void SetRing(float fill)
    {
        if (holdRing == null) return;
        holdRing.fillAmount = fill;
        holdRing.enabled = fill > 0f;
    }

    private void Play(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 0.9f, 1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
