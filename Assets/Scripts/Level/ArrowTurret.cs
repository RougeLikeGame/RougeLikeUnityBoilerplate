using UnityEngine;

// arrow turret: pops up, aims, fires an arrow in one direction, then hides again.
// the pop-up and aim frames are the warning, so the player always has time to step out of the line.
// it can be timed (loops) or wait until the player walks into its line.
public class ArrowTurret : MonoBehaviour
{
    public enum Mode { Timed, WhenPlayerInLine }
    private enum State { Hidden, Rising, Aiming, Firing, Lowering }

    [Header("Art (Art/Level/Traps/ArrowTurret)")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("arrowturret_f2 - f4 (pops up).")]
    [SerializeField] private Sprite[] riseFrames;
    [Tooltip("arrowturret_f5 - f7 (aims, mouth open). This is the warning.")]
    [SerializeField] private Sprite[] aimFrames;
    [Tooltip("arrowturret_f8 - f12 (shoots).")]
    [SerializeField] private Sprite[] fireFrames;
    [Tooltip("arrowturret_f13 - f16, then f0 (goes back down).")]
    [SerializeField] private Sprite[] lowerFrames;
    [Tooltip("arrowturret_f0 (hidden).")]
    [SerializeField] private Sprite hiddenSprite;
    [SerializeField] private float fps = 12f;
    [SerializeField] private Color warningTint = new Color(1f, 0.75f, 0.6f);

    [Header("Shooting")]
    [Tooltip("The arrow prefab (with TrapProjectile on it).")]
    [SerializeField] private TrapProjectile arrowPrefab;
    [Tooltip("Where the arrow comes out (child at the turret's mouth).")]
    [SerializeField] private Transform muzzle;
    [Tooltip("Which way it shoots. The art faces LEFT, so (-1, 0) needs no flipping.")]
    [SerializeField] private Vector2 direction = Vector2.left;
    [SerializeField] private float arrowSpeed = 7f;
    [SerializeField] private float arrowDamage = 1f;

    [Header("Timing")]
    [SerializeField] private Mode mode = Mode.Timed;
    [SerializeField] private float hiddenTime = 2f;
    [Tooltip("The tell. Keep this at 0.5 or more so it's fair.")]
    [SerializeField] private float aimTime = 0.7f;
    [Tooltip("Timed mode: shifts this turret's cycle so rows of turrets take turns.")]
    [SerializeField] private float startOffset = 0f;

    [Header("When Player In Line")]
    [Tooltip("How far it can see down its line.")]
    [SerializeField] private float sightRange = 8f;
    [SerializeField] private float sightWidth = 0.8f;
    [SerializeField] private LayerMask blockingLayers;

    [Header("Sound (optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip aimClip;
    [SerializeField] private AudioClip fireClip;

    private State state = State.Hidden;
    private float timer;
    private float animTime;
    private bool fired;
    private readonly Collider2D[] hits = new Collider2D[8];

    private void Start()
    {
        timer = hiddenTime - startOffset;
        if (spriteRenderer != null && hiddenSprite != null) spriteRenderer.sprite = hiddenSprite;
        if (arrowPrefab == null) Debug.LogError("ArrowTurret: drag in the Arrow Prefab.", this);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        timer -= dt;
        animTime += dt;

        switch (state)
        {
            case State.Hidden:
                bool go = mode == Mode.Timed ? timer <= 0f : timer <= 0f && PlayerInLine();
                if (go) Enter(State.Rising, Length(riseFrames));
                break;

            case State.Rising:
                Show(riseFrames, false);
                if (timer <= 0f) Enter(State.Aiming, aimTime);
                break;

            case State.Aiming:
                Show(aimFrames, true);
                if (spriteRenderer != null) spriteRenderer.color = warningTint;
                if (timer <= 0f) Enter(State.Firing, Length(fireFrames));
                break;

            case State.Firing:
                Show(fireFrames, false);
                if (!fired) { fired = true; Fire(); }
                if (timer <= 0f) Enter(State.Lowering, Length(lowerFrames));
                break;

            case State.Lowering:
                Show(lowerFrames, false);
                if (timer <= 0f)
                {
                    Enter(State.Hidden, hiddenTime);
                    if (spriteRenderer != null && hiddenSprite != null) spriteRenderer.sprite = hiddenSprite;
                }
                break;
        }
    }

    private void Enter(State next, float time)
    {
        state = next;
        timer = time;
        animTime = 0f;
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
        if (next == State.Aiming) Play(aimClip);
        if (next == State.Firing) fired = false;
    }

    private void Fire()
    {
        Play(fireClip);
        if (arrowPrefab == null) return;
        Vector3 pos = muzzle != null ? muzzle.position : transform.position;
        TrapProjectile arrow = Instantiate(arrowPrefab, pos, Quaternion.identity);
        arrow.Launch(direction.normalized * arrowSpeed, arrowDamage, gameObject);
    }

    private bool PlayerInLine()
    {
        Vector2 dir = direction.normalized;
        Vector2 center = (Vector2)transform.position + dir * sightRange * 0.5f;
        Vector2 size = Mathf.Abs(dir.x) > Mathf.Abs(dir.y)
            ? new Vector2(sightRange, sightWidth) : new Vector2(sightWidth, sightRange);
        int n = Physics2D.OverlapBoxNonAlloc(center, size, 0f, hits);
        for (int i = 0; i < n; i++)
        {
            if (hits[i] == null || hits[i].GetComponentInParent<PlayerStats>() == null) continue;
            // a wall in the way blocks its view
            float dist = Vector2.Distance(transform.position, hits[i].transform.position);
            return !Physics2D.Raycast(transform.position, dir, dist, blockingLayers);
        }
        return false;
    }

    // plays a list of frames once (or loops them)
    private void Show(Sprite[] frames, bool loop)
    {
        if (spriteRenderer == null || frames == null || frames.Length == 0) return;
        int i = Mathf.FloorToInt(animTime * fps);
        i = loop ? i % frames.Length : Mathf.Min(i, frames.Length - 1);
        spriteRenderer.sprite = frames[i];
    }

    private float Length(Sprite[] frames) => frames != null && frames.Length > 0 ? frames.Length / fps : 0.1f;

    private void Play(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.7f);
        Vector3 from = muzzle != null ? muzzle.position : transform.position;
        Gizmos.DrawLine(from, from + (Vector3)(direction.normalized * sightRange));
    }
}
