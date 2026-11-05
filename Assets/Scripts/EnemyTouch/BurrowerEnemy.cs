using UnityEngine;

public class BurrowerEnemy : EnemyBase
{
    private enum State { Underground, Telegraph, Emerging, Exposed, Burrowing, Hurt, Dead }

    [Header("Burrower: references (drag in)")]
    [Tooltip("Child object holding the worm sprite. It slides up out of the ground. (Put a SpriteMask on the enemy so the part below ground is hidden.)")]
    [SerializeField] private Transform visual;
    [Tooltip("Dirt-bump sprite that shows where the worm is while underground.")]
    [SerializeField] private SpriteRenderer moundRenderer;
    [Tooltip("Circle sprite that grows on the ground before it bursts up.")]
    [SerializeField] private SpriteRenderer telegraphRenderer;

    [Header("Burrower: underground")]
    [Tooltip("Keep this lower than the player's walk speed (4) so the player can outrun it.")]
    [SerializeField] private float undergroundSpeed = 2.5f;
    [Tooltip("Starts its warning when it gets this close to the player.")]
    [SerializeField] private float strikeDistance = 0.5f;
    [Tooltip("Minimum time underground before it can strike again.")]
    [SerializeField] private float minTrackTime = 1.5f;
    [Tooltip("If it hasn't reached the player by now, it strikes wherever it is.")]
    [SerializeField] private float maxTrackTime = 5f;

    [Header("Burrower: strike")]
    [SerializeField] private float telegraphTime = 1.0f;
    [SerializeField] private float emergeTime = 0.2f;
    [SerializeField] private float strikeRadius = 0.8f;
    [SerializeField] private float strikeDamage = 1f;

    [Header("Burrower: exposed")]
    [Tooltip("How long it stays up (the player's chance to hit it).")]
    [SerializeField] private float exposedTime = 2.0f;
    [SerializeField] private float burrowTime = 0.4f;
    [SerializeField] private float hurtTime = 0.25f;

    [Header("Burrower: look")]
    [Tooltip("How far below the ground the worm sprite starts (world units).")]
    [SerializeField] private float riseDepth = 1.2f;
    [SerializeField] private float moundSize = 0.7f;
    [SerializeField] private Color telegraphColor = new Color(0.55f, 0.35f, 0.15f, 0.7f);

    private State state = State.Underground;
    private float trackTimer;
    private Vector3 visualBasePos;
    private Vector3 moundBasePos;

    private static readonly string[] keys = { "Emerge", "Exposed", "Burrow", "Hurt", "Death" };
    public override string[] AnimationKeys => keys;
    protected override string StateName => state.ToString();
    protected override bool UsesPhysicsMovement => false;

    public override string AnimKey
    {
        get
        {
            switch (state)
            {
                case State.Emerging: return "Emerge";
                case State.Burrowing: return "Burrow";
                case State.Hurt: return "Hurt";
                case State.Dead: return "Death";
                default: return "Exposed";
            }
        }
    }

    protected override void Awake()
    {
        base.Awake();
        if (visual == null) Debug.LogError("BurrowerEnemy: drag in the Visual child.", this);
        if (visual != null) visualBasePos = visual.localPosition;
        if (moundRenderer != null) moundBasePos = moundRenderer.transform.localPosition;
    }

    private void Start() => Enter(State.Underground);

    protected override void Update()
    {
        base.Update();
        if (IsDead) return;

        switch (state)
        {
            case State.Underground:
                trackTimer += Time.deltaTime;
                AnimateMound(0.08f, 0f);
                if (!CanTarget) break;
                bool closeEnough = trackTimer >= minTrackTime && DistToPlayer <= strikeDistance;
                if (closeEnough || trackTimer >= maxTrackTime) Enter(State.Telegraph);
                break;

            case State.Telegraph:
                AnimateMound(0.15f, 0.06f); //shakes hard
                UpdateTelegraph();
                if (stateTimer <= 0f) Enter(State.Emerging);
                break;

            case State.Emerging:
                SetRise(StateProgress01);
                if (stateTimer <= 0f) Enter(State.Exposed);
                break;

            case State.Exposed:
                FacePlayer();
                CheckContactDamage();
                if (stateTimer <= 0f) Enter(State.Burrowing);
                break;

            case State.Burrowing:
                SetRise(1f - StateProgress01);
                if (stateTimer <= 0f) Enter(State.Underground);
                break;

            case State.Hurt:
                if (stateTimer <= 0f) Enter(State.Burrowing);
                break;
        }
    }

    private void FixedUpdate()
    {
        if (state == State.Underground && CanTarget && !IsDead)
            MoveKinematic(DirToPlayer * undergroundSpeed);
    }

    private void Enter(State next)
    {
        state = next;
        blink = false;
        if (telegraphRenderer != null && next != State.Emerging) telegraphRenderer.enabled = false;

        switch (next)
        {
            case State.Underground:
                SetTimer(0f);
                trackTimer = 0f;
                SetTouchable(false);
                SetRise(0f);
                if (bodyRenderer != null) bodyRenderer.enabled = false;
                if (moundRenderer != null) moundRenderer.enabled = true;
                break;

            case State.Telegraph:
                SetTimer(TelegraphTime(telegraphTime));
                if (telegraphRenderer != null) telegraphRenderer.enabled = true;
                UpdateTelegraph();
                break;

            case State.Emerging:
                SetTimer(emergeTime);
                FacePlayer();
                if (bodyRenderer != null) bodyRenderer.enabled = true;
                if (moundRenderer != null) moundRenderer.enabled = false;
                SetTouchable(true);
                //the hit happens the moment it bursts out, on the locked spot.
                if (PlayerWithin(transform.position, strikeRadius)) DamagePlayer(strikeDamage);
                if (telegraphRenderer != null)
                {
                    telegraphRenderer.color = new Color(telegraphColor.r, telegraphColor.g, telegraphColor.b, 0.9f);
                    SizeSprite(telegraphRenderer, strikeRadius * 2f, strikeRadius * 2f);
                }
                break;

            case State.Exposed:
                SetTimer(exposedTime);
                SetRise(1f);
                break;

            case State.Burrowing:
                SetTimer(burrowTime);
                break;

            case State.Hurt:
                SetTimer(hurtTime);
                break;
        }
        NotifyStateChanged();
    }

    private void SetRise(float t)
    {
        if (visual == null) return;
        visual.localPosition = visualBasePos + Vector3.down * (riseDepth * (1f - Mathf.Clamp01(t)));
    }

    private void AnimateMound(float wobble, float shake)
    {
        if (moundRenderer == null) return;
        float s = moundSize * (1f + wobble * Mathf.Sin(Time.time * 14f));
        SizeSprite(moundRenderer, s, s * 0.5f);
        float jx = shake * Mathf.Sin(Time.time * 70f);
        float jy = shake * Mathf.Sin(Time.time * 53f);
        moundRenderer.transform.localPosition = moundBasePos + new Vector3(jx, jy, 0f);
    }

    private void UpdateTelegraph()
    {
        if (telegraphRenderer == null) return;
        float p = StateProgress01;
        float d = strikeRadius * 2f * Mathf.Lerp(0.3f, 1f, p);
        SizeSprite(telegraphRenderer, d, d);
        telegraphRenderer.color = new Color(telegraphColor.r, telegraphColor.g, telegraphColor.b,
                                            telegraphColor.a * Mathf.Lerp(0.3f, 1f, p));
    }

    protected override void OnHurt(float amount)
    {
        if (state == State.Exposed || state == State.Emerging) Enter(State.Hurt);
    }

    protected override void OnDeath()
    {
        state = State.Dead;
        SetRise(1f);
        if (bodyRenderer != null) bodyRenderer.enabled = true;
        if (moundRenderer != null) moundRenderer.enabled = false;
        if (telegraphRenderer != null) telegraphRenderer.enabled = false;
        FacingLocked = true;
    }

    protected override void OnPlayerDied()
    {
        if (!IsDead && state != State.Underground) Enter(State.Burrowing);
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = new Color(0.8f, 0.5f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, strikeRadius);
    }
}
