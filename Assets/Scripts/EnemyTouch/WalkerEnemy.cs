using System.Collections.Generic;
using UnityEngine;

public class WalkerEnemy : EnemyBase
{
    private enum State { Idle, Chase, Windup, Attack, Recover, Hurt, Dead }

    [Header("Walker: senses")]
    [SerializeField] private float detectRange = 5f;
    [SerializeField] private float loseRange = 8f;
    [Tooltip("Layers that block its sight (e.g. Walls). Leave as Nothing to skip the line-of-sight check.")]
    [SerializeField] private LayerMask sightBlockers;

    [Header("Walker: movement")]
    [SerializeField] private float moveSpeed = 2.2f;
    [Tooltip("How often (seconds) it recalculates its A* path while the way is blocked.")]
    [SerializeField] private float repathInterval = 0.35f;
    [SerializeField] private float waypointReachDistance = 0.2f;

    [Header("Walker: attack")]
    [Tooltip("Starts winding up when the player is this close (from its body edge).")]
    [SerializeField] private float attackRange = 0.8f;
    [SerializeField] private float windupTime = 0.5f;
    [SerializeField] private float attackTime = 0.25f;
    [SerializeField] private float attackLungeSpeed = 5f;
    [Tooltip("How far past its body the swing reaches.")]
    [SerializeField] private float attackReach = 0.6f;
    [SerializeField] private float attackDamage = 1f;
    [SerializeField] private float recoverTime = 0.6f;
    [SerializeField] private float attackCooldown = 1.0f;

    [Header("Walker: getting hit")]
    [SerializeField] private float hurtTime = 0.3f;
    [SerializeField] private float knockbackSpeed = 4f;

    private State state = State.Idle;
    private float cooldownTimer;
    private Vector2 attackDir;
    private bool attackLanded;
    private readonly List<Vector2> path = new List<Vector2>();
    private int pathIndex;
    private float repathTimer;
    private Vector2 moveDir;

    private static readonly string[] keys = { "Idle", "Walk", "Windup", "Attack", "Hurt", "Death" };
    public override string[] AnimationKeys => keys;
    protected override string StateName => state.ToString();

    public override string AnimKey
    {
        get
        {
            switch (state)
            {
                case State.Chase: return "Walk";
                case State.Windup: return "Windup";
                case State.Attack: return "Attack";
                case State.Hurt: return "Hurt";
                case State.Dead: return "Death";
                default: return "Idle"; //Idle, Recover
            }
        }
    }

    protected override void Update()
    {
        base.Update();
        if (IsDead) return;
        cooldownTimer -= Time.deltaTime;

        switch (state)
        {
            case State.Idle:
                if (CanTarget && DistToPlayer <= detectRange && HasLineOfSight())
                    Enter(State.Chase);
                break;

            case State.Chase:
                if (!CanTarget || DistToPlayer > loseRange) { Enter(State.Idle); break; }
                moveDir = ChaseDirection();
                FaceDirection(moveDir);
                CheckContactDamage();
                if (cooldownTimer <= 0f && DistToPlayer <= bodyRadius + attackRange)
                    Enter(State.Windup);
                break;

            case State.Windup:
                if (stateTimer <= 0f) Enter(State.Attack);
                break;

            case State.Attack:
                if (!attackLanded && PlayerWithin(transform.position, bodyRadius + attackReach))
                    attackLanded = DamagePlayer(attackDamage);
                if (stateTimer <= 0f) Enter(State.Recover);
                break;

            case State.Recover:
                if (stateTimer <= 0f) Enter(CanTarget ? State.Chase : State.Idle);
                break;

            case State.Hurt:
                if (stateTimer <= 0f) Enter(CanTarget ? State.Chase : State.Idle);
                break;
        }
    }

    private void FixedUpdate()
    {
        Vector2 v = Vector2.zero;
        switch (state)
        {
            case State.Chase:
                v = moveDir * moveSpeed;
                break;
            case State.Attack:
                v = attackDir * attackLungeSpeed;
                break;
            case State.Hurt:
                float t = stateDuration > 0f ? stateTimer / stateDuration : 0f;
                v = -DirToPlayer * knockbackSpeed * Mathf.Clamp01(t);
                break;
        }
        SetVelocity(v);
    }

    private void Enter(State next)
    {
        state = next;
        blink = false;
        FacingLocked = false;

        switch (next)
        {
            case State.Windup:
                SetTimer(TelegraphTime(windupTime));
                attackDir = DirToPlayer;
                FaceDirection(attackDir);
                FacingLocked = true;
                blink = true;
                break;
            case State.Attack:
                SetTimer(attackTime);
                attackLanded = false;
                FacingLocked = true;
                break;
            case State.Recover:
                SetTimer(recoverTime);
                cooldownTimer = attackCooldown;
                break;
            case State.Hurt:
                SetTimer(hurtTime);
                break;
            default:
                SetTimer(0f);
                break;
        }
        NotifyStateChanged();
    }

    //Straight at the player if the way is clear, otherwise along an A* path
    private Vector2 ChaseDirection()
    {
        Vector2 pos = transform.position;
        Vector2 target = player.position;

        if (pathGrid == null || pathGrid.IsClear(pos, target, bodyRadius))
        {
            path.Clear();
            return DirToPlayer;
        }

        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f || pathIndex >= path.Count)
        {
            repathTimer = repathInterval;
            pathGrid.FindPath(pos, target, path);
            pathIndex = 0;
        }

        while (pathIndex < path.Count && Vector2.Distance(pos, path[pathIndex]) < waypointReachDistance)
            pathIndex++;

        if (pathIndex >= path.Count) return DirToPlayer; //no path found: just try going straight
        Vector2 d = path[pathIndex] - pos;
        return d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.zero;
    }

    private bool HasLineOfSight()
    {
        if (sightBlockers.value == 0 || player == null) return true;
        RaycastHit2D hit = Physics2D.Linecast(transform.position, player.position, sightBlockers);
        return hit.collider == null;
    }

    protected override void OnHurt(float amount) => Enter(State.Hurt); //interrupts wind-ups
    protected override void OnDeath() { state = State.Dead; FacingLocked = true; }
    protected override void OnPlayerDied() { if (!IsDead) Enter(State.Idle); }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, bodyRadius + attackReach);

        //Current A* path (cyan)
        Gizmos.color = new Color(0.3f, 1f, 1f, 0.9f);
        Vector3 prev = transform.position;
        for (int i = pathIndex; i < path.Count; i++)
        {
            Gizmos.DrawLine(prev, path[i]);
            Gizmos.DrawWireSphere(path[i], 0.08f);
            prev = path[i];
        }
    }
}
