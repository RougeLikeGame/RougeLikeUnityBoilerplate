using UnityEngine;

public class SkyDiverEnemy : EnemyBase
{
    private enum State { Circling, Telegraph, Diving, Grounded, Ascending, Hurt, Dead }

    [Header("Sky Diver: references (drag in)")]
    [Tooltip("Child object holding the enemy sprite. It gets lifted up by its height.")]
    [SerializeField] private Transform visual;
    [Tooltip("Shadow sprite on the ground")]
    [SerializeField] private SpriteRenderer shadowRenderer;
    [Tooltip("Optional ring sprite that marks the landing spot during the warning. Can be a child")]
    [SerializeField] private SpriteRenderer targetRenderer;

    [Header("Sky Diver: flying")]
    [SerializeField] private float cruiseHeight = 4f;
    [Tooltip("Keep this lower than the player's walk speed (4).")]
    [SerializeField] private float followSpeed = 2f;
    [SerializeField] private float circleTimeMin = 2f;
    [SerializeField] private float circleTimeMax = 3.5f;
    [SerializeField] private float bobAmount = 0.2f;

    [Header("Sky Diver: dive")]
    [SerializeField] private float telegraphTime = 1.0f;
    [SerializeField] private float diveTime = 0.25f;
    [SerializeField] private float impactRadius = 0.9f;
    [SerializeField] private float impactDamage = 1f;

    [Header("Sky Diver: after landing")]
    [Tooltip("How long it's stuck on the ground (the player's chance to hit it).")]
    [SerializeField] private float groundedTime = 1.6f;
    [SerializeField] private float ascendTime = 0.6f;
    [SerializeField] private float hurtTime = 0.25f;
    [SerializeField] private float deathFallTime = 0.3f;

    [Header("Sky Diver: shadow & ring")]
    [SerializeField] private float shadowWidthHigh = 0.4f;
    [SerializeField] private float shadowWidthLow = 1.2f;
    [SerializeField, Range(0f, 1f)] private float shadowAlphaHigh = 0.25f;
    [SerializeField, Range(0f, 1f)] private float shadowAlphaLow = 0.7f;
    [SerializeField] private Color targetColor = new Color(1f, 0.25f, 0.2f, 0.7f);

    private State state = State.Circling;
    private float height;
    private Vector2 diveStart;
    private Vector2 diveTarget;
    private float fallFrom;
    private Vector3 visualBasePos;
    private bool ownsTarget;

    private static readonly string[] keys = { "Fly", "DiveWindup", "Dive", "Grounded", "Hurt", "Death" };
    public override string[] AnimationKeys => keys;
    protected override string StateName => state.ToString();
    protected override bool UsesPhysicsMovement => false;

    public override string AnimKey
    {
        get
        {
            switch (state)
            {
                case State.Telegraph: return "DiveWindup";
                case State.Diving: return "Dive";
                case State.Grounded: return "Grounded";
                case State.Hurt: return "Hurt";
                case State.Dead: return "Death";
                default: return "Fly"; //Circling, Ascending
            }
        }
    }

    protected override void Awake()
    {
        base.Awake();
        if (visual == null) Debug.LogError("SkyDiverEnemy: drag in the Visual child.", this);
        if (visual != null) visualBasePos = visual.localPosition;
        if (targetRenderer != null)
        {
            targetRenderer.enabled = false;
            //In a prefab the ring has to be a child. Unparent it so it stays put while the diver moves.
            if (targetRenderer.transform.IsChildOf(transform))
            {
                targetRenderer.transform.SetParent(null, true);
                ownsTarget = true;
            }
        }
    }

    private void OnDestroy()
    {
        if (ownsTarget && targetRenderer != null) Destroy(targetRenderer.gameObject);
    }

    protected override void Start()
    {
        base.Start();
        Enter(State.Circling);
    }

    protected override void Update()
    {
        base.Update();
        UpdateHeightVisuals();
        if (IsDead)
        {
            //Fall to the ground when killed.
            if (height > 0f) height = Mathf.Lerp(0f, fallFrom, Mathf.Clamp01(stateTimer / Mathf.Max(0.01f, deathFallTime)));
            return;
        }

        switch (state)
        {
            case State.Circling:
                height = cruiseHeight + bobAmount * Mathf.Sin(Time.time * 3f);
                FacePlayer();
                if (stateTimer <= 0f && CanTarget) Enter(State.Telegraph);
                break;

            case State.Telegraph:
                height = cruiseHeight;
                UpdateTarget();
                if (stateTimer <= 0f) Enter(State.Diving);
                break;

            case State.Diving:
                //Ease in: slow at the top, fast at the bottom.
                float p = StateProgress01;
                height = cruiseHeight * (1f - p * p);
                if (stateTimer <= 0f) Land();
                break;

            case State.Grounded:
                height = 0f;
                FacePlayer();
                CheckContactDamage();
                if (stateTimer <= 0f) Enter(State.Ascending);
                break;

            case State.Ascending:
                height = cruiseHeight * StateProgress01;
                //Only hittable for the first half of the climb.
                if (StateProgress01 > 0.5f) SetTouchable(false);
                if (stateTimer <= 0f) Enter(State.Circling);
                break;

            case State.Hurt:
                if (stateTimer <= 0f) Enter(State.Ascending);
                break;
        }
    }

    private void FixedUpdate()
    {
        if (IsDead) return;

        if (state == State.Circling && CanTarget)
        {
            MoveKinematic(DirToPlayer * followSpeed);
        }
        else if (state == State.Telegraph)
        {
            //Slide the shadow onto the locked landing spot during the first half of the warning.
            float t = Mathf.Clamp01(StateProgress01 * 2f);
            rb.MovePosition(Vector2.Lerp(diveStart, diveTarget, t));
        }
    }

    private void Enter(State next)
    {
        state = next;
        blink = false;
        FacingLocked = false;
        if (targetRenderer != null && next != State.Telegraph && next != State.Diving) targetRenderer.enabled = false;

        switch (next)
        {
            case State.Circling:
                SetTimer(Random.Range(circleTimeMin, circleTimeMax));
                SetTouchable(false);
                break;

            case State.Telegraph:
                SetTimer(TelegraphTime(telegraphTime));
                diveStart = rb.position;
                diveTarget = player != null ? (Vector2)player.position : rb.position; //locked now
                FaceDirection(diveTarget - diveStart);
                FacingLocked = true;
                blink = true;
                if (targetRenderer != null)
                {
                    targetRenderer.transform.position = new Vector3(diveTarget.x, diveTarget.y, targetRenderer.transform.position.z);
                    targetRenderer.enabled = true;
                    UpdateTarget();
                }
                break;

            case State.Diving:
                SetTimer(diveTime);
                FacingLocked = true;
                break;

            case State.Grounded:
                SetTimer(groundedTime);
                SetTouchable(true);
                break;

            case State.Ascending:
                SetTimer(ascendTime);
                break;

            case State.Hurt:
                SetTimer(hurtTime);
                break;
        }
        NotifyStateChanged();
    }

    private void Land()
    {
        height = 0f;
        if (PlayerWithin(transform.position, impactRadius)) DamagePlayer(impactDamage);
        Enter(State.Grounded);
    }

    private void UpdateHeightVisuals()
    {
        if (visual != null) visual.localPosition = visualBasePos + Vector3.up * Mathf.Max(0f, height);

        if (shadowRenderer != null)
        {
            float h01 = cruiseHeight > 0f ? Mathf.Clamp01(height / cruiseHeight) : 0f;
            float w = Mathf.Lerp(shadowWidthLow, shadowWidthHigh, h01);
            SizeSprite(shadowRenderer, w, w * 0.35f);
            Color c = shadowRenderer.color;
            c.a = Mathf.Lerp(shadowAlphaLow, shadowAlphaHigh, h01);
            if (IsDead) c.a *= Mathf.Clamp01(stateTimer + 1f);
            shadowRenderer.color = c;
        }
    }

    private void UpdateTarget()
    {
        if (targetRenderer == null) return;
        float p = StateProgress01;
        float d = impactRadius * 2f * Mathf.Lerp(1.3f, 1f, p); //ring tightens onto the spot
        SizeSprite(targetRenderer, d, d);
        targetRenderer.color = new Color(targetColor.r, targetColor.g, targetColor.b, targetColor.a * Mathf.Lerp(0.35f, 1f, p));
    }

    protected override void OnHurt(float amount)
    {
        if (state == State.Grounded || state == State.Ascending) Enter(State.Hurt);
    }

    protected override void OnDeath()
    {
        state = State.Dead;
        fallFrom = height;
        SetTimer(deathFallTime);
        if (targetRenderer != null) targetRenderer.enabled = false;
        FacingLocked = true;
    }

    protected override void OnPlayerDied()
    {
        if (!IsDead && state != State.Circling) Enter(State.Ascending);
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, impactRadius);
    }
}
