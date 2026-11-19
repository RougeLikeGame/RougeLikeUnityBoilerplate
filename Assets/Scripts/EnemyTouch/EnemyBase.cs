using System;
using System.Collections;
using UnityEngine;

// shared code for all regular enemies (walker, burrower, sky diver).
// each enemy has its own state machine; this base class handles the boring shared parts:
//   - references to the player (dragged in, nothing is found automatically)
//   - state timers and progress (used by enemyspriteanimator for wind-ups)
//   - damaging the player (respects the player's invincibility frames)
//   - hurt / death / player-death reactions
//   - flash + blink colors for telegraphs and hits
//   - a debug label in the scene view
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
    [Tooltip("Optional: the room's A* grid, so ground enemies walk around obstacles. The spawner fills this in for spawned enemies.")]
    [SerializeField] protected PathGrid pathGrid;

    [SerializeField] protected GameManager gameManager;

    [Header("Shared")]
    [SerializeField] protected float contactDamage = 1f;
    [Tooltip("Rough radius of the enemy's body, used for touch and attack hits.")]
    [SerializeField] protected float bodyRadius = 0.4f;
    [Tooltip("Rough radius of the player, to hit checks.")]
    [SerializeField] protected float playerRadius = 0.3f;
    [Tooltip("No attack warning is ever shorter than this (fairness).")]
    [SerializeField] protected float minTelegraphTime = 0.6f;
    [Tooltip("Seconds after death before the enemy is removed. 0 = keep the body.")]
    [SerializeField] protected float destroyAfterDeath = 2f;
    [Tooltip("Flash colour when the player parries this enemy.")]
    [SerializeField] protected Color parriedFlashColor = new Color(1f, 0.85f, 0.3f);
    private PlayerParry parry;
    [SerializeField] protected Color hurtFlashColor = new Color(1f, 1f, 1f);
    [SerializeField] protected Color windupColor = new Color(1f, 0.35f, 0.25f);

    [SerializeField] protected float stunDuration = 1.5f;

    // ---------------- for the animator / other scripts ----------------
    public abstract string AnimKey { get; }
    // all animation names this enemy uses (for the animator's "fill animation names" menu).
    public abstract string[] AnimationKeys { get; }
    public float StateProgress01 => stateDuration > 0f ? Mathf.Clamp01(1f - stateTimer / stateDuration) : 1f;
    public Vector2 Facing { get; protected set; } = Vector2.right;
    public bool FacingLocked { get; protected set; }
    public bool IsDead { get; private set; }

    public bool IsStunned { get; private set; }
    protected GameObject lastDamageSource;

    public event Action<string> OnStateChanged;

    // ---------------- for subclasses ----------------
    protected Rigidbody2D rb;
    protected float stateTimer;
    protected float stateDuration;
    protected bool playerDead;
    // set true during a wind-up to make the enemy blink.
    protected bool blink;
    // alpha of the body sprite (for fading in/out).
    protected float bodyAlpha = 1f;

    private Color flashColor;
    private float flashTimer;
    private const float FlashDuration = 0.15f;

    protected abstract string StateName { get; }
    // true = normal physics (bumps into walls). false = kinematic (ignores walls: burrowing, flying).
    protected virtual bool UsesPhysicsMovement => true;

    // ======================================================================

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = UsesPhysicsMovement ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    // checked in start (not awake) so a spawner has time to call settarget first.
    protected virtual void Start()
    {
        if (playerStats == null || player == null)
            Debug.LogError($"{GetType().Name} on '{name}': drag in Player Stats and Player (or spawn it with an EnemySpawner).", this);
        if (health == null)
            Debug.LogWarning($"{GetType().Name} on '{name}': no EnemyHealth assigned, so it can't be hurt.", this);
    }

    // gives a spawned enemy its targets. prefabs can't store references to objects in a scene,
    // so enemyspawner calls this right after creating the enemy.
    public void SetTarget(PlayerStats stats, Transform playerTransform, PathGrid grid = null)
    {
        if (playerStats != null && isActiveAndEnabled) playerStats.OnDied -= HandlePlayerDied;
        playerStats = stats;
        player = playerTransform;
        if (grid != null) pathGrid = grid;
        playerDead = playerStats != null && playerStats.IsDead;
        if (playerStats != null && isActiveAndEnabled) playerStats.OnDied += HandlePlayerDied;
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

    // ---------------- helpers ----------------

    protected void SetTimer(float seconds)
    {
        stateTimer = seconds;
        stateDuration = seconds;
    }

    // makes sure a warning is never shorter than mintelegraphtime.
    protected float TelegraphTime(float wanted) => Mathf.Max(minTelegraphTime, wanted);

    protected bool CanTarget => player != null && playerStats != null && !playerDead && !IsDead && !IsStunned;

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

    // is the player within this distance of a point (plus the player's radius)?
    protected bool PlayerWithin(Vector2 point, float radius)
    {
        if (player == null) return false;
        return Vector2.Distance(player.position, point) <= radius + playerRadius;
    }

    // damages the player. returns true if it landed (not blocked by i-frames).
    protected bool DamagePlayer(float amount)
    {
        if (!CanTarget || playerStats.IsDead || playerStats.IsInvincible || amount <= 0f) return false;

        //the player can parry the hit
        if (parry == null) parry = playerStats.GetComponent<PlayerParry>();
        if (parry != null && parry.TryParry())
        {
            OnParried();
            return false;
        }

        playerStats.TakeDamage(amount);
        return true;
    }

    // what happens when the player parries this enemy. by default it flashes gold and staggers
    // (each enemy's normal hurt reaction, but with no damage).
    protected virtual void OnParried()
    {
        Flash(parriedFlashColor);
        OnHurt(0f);
        StartCoroutine(Stun(stunDuration));
    }

    private IEnumerator Stun(float duration) 
    {
        float elapsedTime = 0f;
        IsStunned = true;

        while (elapsedTime < duration) 
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        IsStunned = false;
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

    // for dynamic bodies (walker).
    protected void SetVelocity(Vector2 v)
    {
        if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic) return;
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = v;
#else
        rb.velocity = v;
#endif
    }

    // for kinematic bodies (burrower, sky diver): move ignoring walls. call from fixedupdate.
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

    // sizes a sprite in world units, whatever size the sprite file is.
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

    private void HandleHurt(float amount, GameObject source)
    {
        if (IsDead) return;
        Flash(hurtFlashColor);
        OnHurt(amount);
        lastDamageSource = source;
    }

    private void HandleDeath()
    {
        if (IsDead) return;
        IsDead = true;
        blink = false;
        SetVelocity(Vector2.zero);
        if (bodyCollider != null) bodyCollider.enabled = false;
        if (lastDamageSource.CompareTag("Player")) 
        {
            if (IsStunned)
            {
                gameManager.ChangeState(GameManager.StateSource.Parry);
            }
            else 
            {
                gameManager.ChangeState(GameManager.StateSource.Player);
            }

        
        }else if (lastDamageSource.CompareTag("Trap"))
        {
                
                gameManager.ChangeState(GameManager.StateSource.Trap);


        }


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

    // called when the enemy takes damage but doesn't die.
    protected abstract void OnHurt(float amount);
    // called once when the enemy dies.
    protected abstract void OnDeath();
    // called when the player dies (stop attacking).
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






















/*using System;
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
    [Tooltip("Optional: the room's A* grid, so ground enemies walk around obstacles. The spawner fills this in for spawned enemies.")]
    [SerializeField] protected PathGrid pathGrid;

    [Header("Shared")]
    [SerializeField] protected float contactDamage = 1f;
    [Tooltip("Rough radius of the enemy's body, used for touch and attack hits.")]
    [SerializeField] protected float bodyRadius = 0.4f;
    [Tooltip("Rough radius of the player,  to hit checks.")]
    [SerializeField] protected float playerRadius = 0.3f;
    [Tooltip("No attack warning is ever shorter than this (fairness).")]
    [SerializeField] protected float minTelegraphTime = 0.6f;
    [Tooltip("Seconds after death before the enemy is removed. 0 = keep the body.")]
    [SerializeField] protected float destroyAfterDeath = 2f;
    [SerializeField] protected Color hurtFlashColor = new Color(1f, 1f, 1f);
    [SerializeField] protected Color windupColor = new Color(1f, 0.35f, 0.25f);

    public abstract string AnimKey { get; }
    //All animation names this enemy uses (for the animator's "Fill Animation Names" menu)
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
    //Set true during a wind-up to make the enemy blink
    protected bool blink;
    //Alpha of the body sprite (for fading in/out)
    protected float bodyAlpha = 1f;

    private Color flashColor;
    private float flashTimer;
    private const float FlashDuration = 0.15f;

    protected abstract string StateName { get; }
    //True = normal physics (bumps into walls). False = kinematic (ignores walls: burrowing, flying)
    protected virtual bool UsesPhysicsMovement => true;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = UsesPhysicsMovement ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    //Checked in Start (not Awake) so a spawner has time to call SetTarget first
    protected virtual void Start()
    {
        if (playerStats == null || player == null)
            Debug.LogError($"{GetType().Name} on '{name}': drag in Player Stats and Player (or spawn it with an EnemySpawner).", this);
        if (health == null)
            Debug.LogWarning($"{GetType().Name} on '{name}': no EnemyHealth assigned, so it can't be hurt.", this);
    }

    //Gives a spawned enemy its targets. Prefabs can't store references to objects in a scene,
    //so EnemySpawner calls this right after creating the enemy.
    public void SetTarget(PlayerStats stats, Transform playerTransform, PathGrid grid = null)
    {
        if (playerStats != null && isActiveAndEnabled) playerStats.OnDied -= HandlePlayerDied;
        playerStats = stats;
        player = playerTransform;
        if (grid != null) pathGrid = grid;
        playerDead = playerStats != null && playerStats.IsDead;
        if (playerStats != null && isActiveAndEnabled) playerStats.OnDied += HandlePlayerDied;
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

    //Makes sure a warning is never shorter than minTelegraphTime
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

    //is the player within this distance of a point
    protected bool PlayerWithin(Vector2 point, float radius)
    {
        if (player == null) return false;
        return Vector2.Distance(player.position, point) <= radius + playerRadius;
    }

    //Damages the player. Returns true if it landed (not blocked by i-frames)
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

    //For Dynamic bodies (Walker)
    protected void SetVelocity(Vector2 v)
    {
        if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic) return;
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = v;
#else
        rb.velocity = v;
#endif
    }

    //for Kinematic bodies (Burrower, Sky Diver): move ignoring walls. Call from FixedUpdate
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

    //sizes a sprite in world units, whatever size the sprite file is
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

    //Called when the enemy takes damage but doesn't die
    protected abstract void OnHurt(float amount);
    //called once when the enemy dies
    protected abstract void OnDeath();
    //Called when the player dies (stop attacking)
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
*/