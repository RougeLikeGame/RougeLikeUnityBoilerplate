using System.Collections.Generic;
using UnityEngine;

public class SpikeTrap : MonoBehaviour
{
    public enum Mode { Timed, Pressure }
    private enum State { Down, Warning, Up, Retracting }

    [Header("Art")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("Frames from fully DOWN (first) to fully UP (last).")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private Color warningTint = new Color(1f, 0.75f, 0.6f);

    [Header("Behaviour")]
    [SerializeField] private Mode mode = Mode.Timed;
    [SerializeField] private float downTime = 1.6f;
    [Tooltip("The tell. Keep this at 0.5 or more so it's fair.")]
    [SerializeField] private float warningTime = 0.6f;
    [SerializeField] private float upTime = 0.8f;
    [SerializeField] private float retractTime = 0.15f;
    [Tooltip("Timed mode: shifts this trap's cycle (seconds) so rows of traps can ripple.")]
    [SerializeField] private float startOffset = 0f;

    [Header("Damage")]
    [Tooltip("Size of the damaging area (world units). Usually one tile, e.g. 1 x 1.")]
    [SerializeField] private Vector2 areaSize = new Vector2(0.9f, 0.9f);
    [SerializeField] private float playerDamage = 1f;
    [SerializeField] private bool hurtEnemies = true;
    [SerializeField] private float enemyDamage = 1f;

    [Header("Sound (optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip warningClip;
    [SerializeField] private AudioClip upClip;

    private State state = State.Down;
    private float timer;
    private readonly Collider2D[] results = new Collider2D[16];
    private readonly HashSet<EnemyHealth> hitThisCycle = new HashSet<EnemyHealth>();

    private void Start()
    {
        timer = mode == Mode.Timed ? downTime - startOffset : 0f;
        SetFrame(0);
    }

    private void Update()
    {
        switch (state)
        {
            case State.Down:
                SetFrame(0);
                if (mode == Mode.Timed)
                {
                    timer -= Time.deltaTime;
                    if (timer <= 0f) Enter(State.Warning, warningTime);
                }
                else if (PlayerOnTrap())
                {
                    Enter(State.Warning, warningTime);
                }
                break;

            case State.Warning:
                timer -= Time.deltaTime;
                //Rattle between the first two frames so it's readable.
                SetFrame(Mathf.FloorToInt(Time.time * 14f) % 2 == 0 ? 0 : Mathf.Min(1, LastFrame));
                if (spriteRenderer != null) spriteRenderer.color = warningTint;
                if (timer <= 0f) Enter(State.Up, upTime);
                break;

            case State.Up:
                timer -= Time.deltaTime;
                SetFrame(LastFrame);
                if (timer <= 0f) Enter(State.Retracting, retractTime);
                break;

            case State.Retracting:
                timer -= Time.deltaTime;
                float p = retractTime > 0f ? Mathf.Clamp01(timer / retractTime) : 0f;
                SetFrame(Mathf.RoundToInt(p * LastFrame));
                if (timer <= 0f) Enter(State.Down, downTime);
                break;
        }
    }

    private void FixedUpdate()
    {
        if (state == State.Up) DealDamage();
    }

    private void Enter(State next, float time)
    {
        state = next;
        timer = time;
        if (spriteRenderer != null && next != State.Warning) spriteRenderer.color = Color.white;

        if (next == State.Warning) Play(warningClip);
        if (next == State.Up)
        {
            hitThisCycle.Clear();
            Play(upClip);
            DealDamage();
        }
    }

    private void DealDamage()
    {
        int n = Physics2D.OverlapBoxNonAlloc(transform.position, areaSize, 0f, results);
        for (int i = 0; i < n; i++)
        {
            Collider2D c = results[i];
            if (c == null) continue;

            PlayerStats ps = c.GetComponentInParent<PlayerStats>();
            if (ps != null)
            {
                if (!ps.IsDead && !ps.IsInvincible) ps.TakeDamage(playerDamage);
                continue;
            }

            if (!hurtEnemies) continue;
            EnemyHealth eh = c.GetComponentInParent<EnemyHealth>();
            if (eh != null && hitThisCycle.Add(eh)) eh.TakeDamage(enemyDamage);
        }
    }

    private bool PlayerOnTrap()
    {
        int n = Physics2D.OverlapBoxNonAlloc(transform.position, areaSize, 0f, results);
        for (int i = 0; i < n; i++)
            if (results[i] != null && results[i].GetComponentInParent<PlayerStats>() != null) return true;
        return false;
    }

    private int LastFrame => frames != null && frames.Length > 0 ? frames.Length - 1 : 0;

    private void SetFrame(int i)
    {
        if (spriteRenderer == null || frames == null || frames.Length == 0) return;
        spriteRenderer.sprite = frames[Mathf.Clamp(i, 0, frames.Length - 1)];
    }

    private void Play(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.7f);
        Gizmos.DrawWireCube(transform.position, new Vector3(areaSize.x, areaSize.y, 0f));
    }
}
