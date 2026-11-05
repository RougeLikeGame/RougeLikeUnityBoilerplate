using System;
using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("References (drag these in)")]
    [SerializeField] protected PlayerStats playerStats;
    [SerializeField] protected Transform player;
    [SerializeField] protected EnemyHealth health;
    [Tooltip("The enemy's main collider. Turned off while it can't be touched (underground, in the sky, dead).")]
    [SerializeField] protected Collider2D bodyCollider;
    [Tooltip("The SpriteRenderer showing the enemy art (used for flashes and blinking).")]
    [SerializeField] protected SpriteRenderer bodyRenderer;

    [Header("Shared")]
    [SerializeField] protected float contactDamage = 1f;
    [Tooltip("Rough radius of the enemy's body, used for touch and attack hits.")]
    [SerializeField] protected float bodyRadius = 0.4f;
    [Tooltip("Rough radius of the player, added to hit checks.")]
    [SerializeField] protected float playerRadius = 0.3f;
    [Tooltip("No attack warning is ever shorter than this (fairness).")]
    [SerializeField] protected float minTelegraphTime = 0.6f;
    [Tooltip("Seconds after death before the enemy is removed. 0 = keep the body.")]
    [SerializeField] protected float destroyAfterDeath = 2f;
    [SerializeField] protected Color hurtFlashColor = new Color(1f, 1f, 1f);
    [SerializeField] protected Color windupColor = new Color(1f, 0.35f, 0.25f);


    public abstract string AnimKey { get; }
    public abstract string[] AnimationKeys { get; }
    public float StateProgress01 => stateDuration > 0f ? Mathf.Clamp01(1f - stateTimer / stateDuration) : 1f;
    public Vector2 Facing { get; protected set; } = Vector2.right;
    public bool FacingLocked { get; protected set; }
    public bool IsDead { get; private set; }

    public event Action<string> OnStateChanged;

    protected Rigidbody2D rb;
    protected float stateTimer;
    protected float stateDuration;
    protected bool playerDead;
    protected bool blink;
    protected float bodyAlpha = 1f;

    private Color flashColor;
    private float flashTimer;
    private const float FlashDuration = 0.15f;

    protected abstract string StateName { get; }
    protected virtual bool UsesPhysicsMovement => true;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = UsesPhysicsMovement ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (playerStats == null || player == null)
            Debug.LogError($"{GetType().Name} on '{name}': drag in Player Stats and Player.", this);
        if (health == null)
            Debug.LogWarning($"{GetType().Name} on '{name}': no EnemyHealth assigned, so it can't be hurt.", this);
    }

    protected virtual void OnEnable()
    {
        if (playerStats != null) playerStats.OnDied += HandlePlayerDied;
        if (health != null)
        {
            health.OnHurt += HandleHurt;
            health.OnDied += HandleDeath;
        }
    }

    protected virtual void OnDisable()
    {
        if (playerStats != null) playerStats.OnDied -= HandlePlayerDied;
        if (health != null)
        {
            health.OnHurt -= HandleHurt;
            health.OnDied -= HandleDeath;
        }
    }

    protected virtual void Update()
    {
        stateTimer -= Time.deltaTime;
        UpdateColor();
    }

    protected void SetTimer(float seconds)
    {
        stateTimer = seconds;
        stateDuration = seconds;
    }

    protected float TelegraphTime(float wanted) => Mathf.Max(minTelegraphTime, wanted);

    protected bool CanTarget => player != null && playerStats != null && !playerDead && !IsDead;

    protected float DistToPlayer => player != null ? Vector2.Distance(player.position, transform.position) : 9999f;

    protected Vector2 DirToPlayer
    {
        get
        {
            if (player == null) return Vector2.zero;
            Vector2 d = (Vector2)player.position - (Vector2)transform.position;
            return d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.zero;
        }
    }

    protected bool PlayerWithin(Vector2 point, float radius)
    {
        if (player == null) return false;
        return Vector2.Distance(player.position, point) <= radius + playerRadius;
    }

    protected bool DamagePlayer(float amount)
    {
        if (!CanTarget || playerStats.IsDead || playerStats.IsInvincible || amount <= 0f) return false;
        playerStats.TakeDamage(amount);
        return true;
    }

    protected void CheckContactDamage()
    {
        if (contactDamage > 0f && PlayerWithin(transform.position, bodyRadius))
            DamagePlayer(contactDamage);
    }

    protected void FacePlayer()
    {
        if (FacingLocked || player == null) return;
        Vector2 d = DirToPlayer;
        if (Mathf.Abs(d.x) > 0.05f) Facing = d;
    }

    protected void FaceDirection(Vector2 d)
    {
        if (!FacingLocked && Mathf.Abs(d.x) > 0.05f) Facing = d;
    }

    protected void SetVelocity(Vector2 v)
    {
        if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic) return;
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = v;
#else
        rb.velocity = v;
#endif
    }

    protected void MoveKinematic(Vector2 velocity)
    {
        if (rb == null) return;
        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
    }

    protected void SetTouchable(bool touchable)
    {
        if (bodyCollider != null) bodyCollider.enabled = touchable;
        if (health != null) health.Invulnerable = !touchable;
    }

    protected void Flash(Color c)
    {
        flashColor = c;
        flashTimer = FlashDuration;
    }

    protected void NotifyStateChanged() => OnStateChanged?.Invoke(StateName);
    protected static void SizeSprite(SpriteRenderer sr, float width, float height)
    {
        if (sr == null || sr.sprite == null) return;
        Vector2 size = sr.sprite.bounds.size;
        if (size.x <= 0f || size.y <= 0f) return;
        sr.transform.localScale = new Vector3(width / size.x, height / size.y, 1f);
    }

    private void UpdateColor()
    {
        if (bodyRenderer == null) return;

        Color c = Color.white;
        if (blink && Mathf.FloorToInt(Time.time * 12f) % 2 == 0) c = windupColor;
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            c = flashColor;
        }
        c.a = bodyAlpha;
        bodyRenderer.color = c;
    }


    private void HandleHurt(float amount)
    {
        if (IsDead) return;
        Flash(hurtFlashColor);
        OnHurt(amount);
    }

    private void HandleDeath()
    {
        if (IsDead) return;
        IsDead = true;
        blink = false;
        SetVelocity(Vector2.zero);
        if (bodyCollider != null) bodyCollider.enabled = false;
        OnDeath();
        NotifyStateChanged();
        if (destroyAfterDeath > 0f) Destroy(gameObject, destroyAfterDeath);
    }

    private void HandlePlayerDied()
    {
        playerDead = true;
        blink = false;
        OnPlayerDied();
    }

    protected abstract void OnHurt(float amount);
    protected abstract void OnDeath();
    protected abstract void OnPlayerDied();

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, bodyRadius);
#if UNITY_EDITOR
        if (Application.isPlaying)
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.2f, StateName);
#endif
    }
}
