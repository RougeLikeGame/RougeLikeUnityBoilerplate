using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class LostSoul : MonoBehaviour
{
    private enum State { Ready, Helped, Resting, Freed }

    [Header("References (drag in)")]
    [SerializeField] private ScarcityRemnant boss;
    [SerializeField] private Transform player;
    [Tooltip("The soul's sprite (a child, so it can bob and rise).")]
    [SerializeField] private SpriteRenderer body;
    [Tooltip("Optional soft glow behind the soul (ui_glow works well).")]
    [SerializeField] private SpriteRenderer glow;
    [Tooltip("Optional world-space TextMeshPro prompt, e.g. \"E  Comfort\".")]
    [SerializeField] private TMP_Text prompt;
    [SerializeField] private InputActionAsset inputActions;

    [Header("Animation (optional)")]
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private float fps = 6f;
    [SerializeField] private float bobAmount = 0.08f;
    [SerializeField] private float bobSpeed = 2f;

    [Header("Help")]
    [SerializeField] private float interactRadius = 1.1f;
    [Tooltip("How strong one comfort is (1 = one normal helpful action).")]
    [SerializeField] private float helpStrength = 1f;
    [Tooltip("Seconds the soul rests before it can be helped again.")]
    [SerializeField] private float restTime = 7f;
    [SerializeField] private float helpedAnimTime = 0.8f;
    [Tooltip("Only usable while the boss fight is active.")]
    [SerializeField] private bool onlyDuringEncounter = true;

    [Header("Colors")]
    [SerializeField] private Color readyColor = new Color(0.65f, 0.9f, 1f, 0.85f);
    [SerializeField] private Color restingColor = new Color(0.6f, 0.65f, 0.75f, 0.25f);
    [SerializeField] private Color helpedColor = new Color(1f, 0.85f, 0.45f, 1f);
    [SerializeField] private Color glowColor = new Color(0.55f, 0.85f, 1f, 0.5f);

    [Header("Sound (optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip helpClip;
    [SerializeField] private AudioClip readyAgainClip;

    public bool IsReady => state == State.Ready;

    //Only one soul can be helped per key press, even if two are in range.
    private static int lastHelpFrame = -1;

    private State state = State.Ready;
    private float timer;
    private InputAction interact;
    private bool createdAction;
    private Vector3 bodyBasePos;
    private Vector3 bodyBaseScale;
    private float promptAlpha;
    private float animTime;

    private void Awake()
    {
        if (boss == null || player == null || body == null)
            Debug.LogError("LostSoul: drag in Boss, Player and Body.", this);

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
    }

    private void OnEnable()
    {
        interact?.Enable();
        if (boss != null) boss.OnTransformed += HandleBossTransformed;
    }

    private void OnDisable()
    {
        if (boss != null) boss.OnTransformed -= HandleBossTransformed;
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
        bool canUse = state == State.Ready && !blocked && player != null
                      && (!onlyDuringEncounter || (boss != null && boss.IsActive))
                      && Vector2.Distance(player.position, transform.position) <= interactRadius;

        //Prompt fades in/out.
        promptAlpha = Mathf.MoveTowards(promptAlpha, canUse ? 1f : 0f, dt * 6f);
        SetPromptAlpha(promptAlpha);

        if (canUse && interact != null && interact.WasPressedThisFrame() && lastHelpFrame != Time.frameCount)
        {
            lastHelpFrame = Time.frameCount;
            Help();
        }

        switch (state)
        {
            case State.Ready:
                SetColors(readyColor, glowColor.a * (0.7f + 0.3f * Mathf.Sin(Time.time * 3f)));
                SetRise(0f, 1f);
                break;

            case State.Helped:
                timer -= dt;
                float p = 1f - Mathf.Clamp01(timer / helpedAnimTime);
                //Glow gold, drift upward and fade out.
                SetColors(Color.Lerp(helpedColor, new Color(helpedColor.r, helpedColor.g, helpedColor.b, 0f), p), glowColor.a * (1f - p) * 1.5f);
                SetRise(p * 0.6f, 1f + p * 0.3f);
                if (timer <= 0f) { state = State.Resting; timer = restTime; }
                break;

            case State.Resting:
                timer -= dt;
                float r = 1f - Mathf.Clamp01(timer / restTime);
                //Slowly gathers itself back together.
                SetColors(Color.Lerp(new Color(restingColor.r, restingColor.g, restingColor.b, 0f), restingColor, r), 0f);
                SetRise(0f, Mathf.Lerp(0.6f, 1f, r));
                if (timer <= 0f)
                {
                    state = State.Ready;
                    if (audioSource != null && readyAgainClip != null) audioSource.PlayOneShot(readyAgainClip);
                }
                break;

            case State.Freed:
                SetColors(helpedColor, glowColor.a);
                SetRise(0.1f + 0.05f * Mathf.Sin(Time.time * 1.5f), 1f);
                break;
        }
    }

    private void Help()
    {
        if (boss != null) boss.ReceiveHelp(helpStrength);
        if (audioSource != null && helpClip != null) audioSource.PlayOneShot(helpClip);
        state = State.Helped;
        timer = helpedAnimTime;
        promptAlpha = 0f;
        SetPromptAlpha(0f);
    }

    private void HandleBossTransformed()
    {
        state = State.Freed; //every soul is at peace once the Remnant transforms
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 0.9f, 1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
