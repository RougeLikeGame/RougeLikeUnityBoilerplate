using System.Collections.Generic;
using UnityEngine;

// spinning saw that slides back and forth between points.
// you can always see where it's going (it moves at a steady speed along a line you can read),
// so it's fair without a warning. it pauses a little at each end.
// put this on the saw, add a trigger collider, and drag in 2+ empty objects as the path.
public class SawHazard : MonoBehaviour
{
    [Header("Art (Art/Level/Traps/Saw)")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("saw_f0 - f15.")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float fps = 16f;

    [Header("Path")]
    [Tooltip("Empty objects the saw moves between, in order. Keep them OUT of the saw (siblings, not children).")]
    [SerializeField] private Transform[] points;
    [SerializeField] private float speed = 2.5f;
    [SerializeField] private float pauseAtEnds = 0.4f;
    [Tooltip("On = go 1-2-3-1-2-3. Off = go 1-2-3-2-1.")]
    [SerializeField] private bool loop = false;

    [Header("Damage")]
    [SerializeField] private float playerDamage = 1f;
    [SerializeField] private bool hurtEnemies = true;
    [SerializeField] private float enemyDamage = 1f;
    [Tooltip("An enemy can only be cut once every this many seconds.")]
    [SerializeField] private float enemyHitCooldown = 0.8f;

    [Header("Path line (optional)")]
    [Tooltip("A LineRenderer that draws the saw's track on the floor, so players can read it.")]
    [SerializeField] private LineRenderer track;

    private int target = 1;
    private int step = 1;
    private float pause;
    private float animTime;
    private readonly Dictionary<EnemyHealth, float> lastEnemyHit = new Dictionary<EnemyHealth, float>();

    private void Start()
    {
        if (points == null || points.Length < 2)
        {
            Debug.LogError("SawHazard: drag in at least 2 points.", this);
            enabled = false;
            return;
        }
        transform.position = points[0].position;

        if (track != null)
        {
            track.positionCount = points.Length + (loop ? 1 : 0);
            for (int i = 0; i < points.Length; i++) track.SetPosition(i, points[i].position);
            if (loop) track.SetPosition(points.Length, points[0].position);
        }
    }

    private void Update()
    {
        animTime += Time.deltaTime;
        if (spriteRenderer != null && frames != null && frames.Length > 0)
            spriteRenderer.sprite = frames[Mathf.FloorToInt(animTime * fps) % frames.Length];

        if (pause > 0f) { pause -= Time.deltaTime; return; }

        Vector3 goal = points[target].position;
        transform.position = Vector3.MoveTowards(transform.position, goal, speed * Time.deltaTime);
        if ((transform.position - goal).sqrMagnitude < 0.0001f) NextPoint();
    }

    private void NextPoint()
    {
        if (loop)
        {
            target = (target + 1) % points.Length;
            if (target == 0 || target == 1) pause = pauseAtEnds;
            return;
        }

        if (target + step >= points.Length || target + step < 0)
        {
            step = -step;
            pause = pauseAtEnds;
        }
        target += step;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        PlayerStats ps = other.GetComponentInParent<PlayerStats>();
        if (ps != null)
        {
            // the player's own i-frames stop this from hitting every frame
            if (!ps.IsDead && !ps.IsInvincible) ps.TakeDamage(playerDamage);
            return;
        }

        if (!hurtEnemies) return;
        EnemyHealth eh = other.GetComponentInParent<EnemyHealth>();
        if (eh == null) return;
        if (lastEnemyHit.TryGetValue(eh, out float t) && Time.time - t < enemyHitCooldown) return;
        lastEnemyHit[eh] = Time.time;
        eh.TakeDamage(enemyDamage);
    }

    private void OnDrawGizmos()
    {
        if (points == null) return;
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
        for (int i = 0; i + 1 < points.Length; i++)
            if (points[i] != null && points[i + 1] != null)
                Gizmos.DrawLine(points[i].position, points[i + 1].position);
        if (loop && points.Length > 2 && points[0] != null && points[points.Length - 1] != null)
            Gizmos.DrawLine(points[points.Length - 1].position, points[0].position);
    }
}
