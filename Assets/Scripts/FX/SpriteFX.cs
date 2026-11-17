using UnityEngine;

// plays a list of frames once, then removes itself.
// used for smoke puffs, dust, wisps, parry flashes and boss eruptions.
// make a prefab: an object with a sprite renderer and this script, fill in the frames, save it.
public class SpriteFX : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float fps = 12f;
    [Tooltip("Keep looping until Life Time runs out (for things like a rising wisp).")]
    [SerializeField] private bool loop = false;
    [Tooltip("Only used when Loop is on. 0 = one pass of the frames.")]
    [SerializeField] private float lifeTime = 0f;

    [Header("Motion")]
    [Tooltip("Units per second it drifts (e.g. 0, 1 for a wisp floating up).")]
    [SerializeField] private Vector2 drift = Vector2.zero;
    [Tooltip("Random extra sideways drift, so a bunch of them don't look the same.")]
    [SerializeField] private float randomDrift = 0f;
    [SerializeField] private float startScale = 1f;
    [SerializeField] private float endScale = 1f;

    [Header("Look")]
    [SerializeField] private Color color = Color.white;
    [Tooltip("Fade out over the last part of its life (0 = no fade, 0.5 = fade over the second half).")]
    [SerializeField, Range(0f, 1f)] private float fadeOut = 0.4f;
    [SerializeField] private bool randomFlip = true;

    private float age;
    private float life;
    private Vector2 velocity;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        float pass = frames != null && frames.Length > 0 ? frames.Length / Mathf.Max(1f, fps) : 0.5f;
        life = loop && lifeTime > 0f ? lifeTime : pass;
        velocity = drift + new Vector2(Random.Range(-randomDrift, randomDrift), 0f);
        if (spriteRenderer != null && randomFlip) spriteRenderer.flipX = Random.value < 0.5f;
        Apply(0f);
    }

    // lets other scripts change the tint when they spawn it (like dark smoke)
    public void SetColor(Color c)
    {
        color = c;
        Apply(age / life);
    }

    private void Update()
    {
        age += Time.deltaTime;
        transform.position += (Vector3)(velocity * Time.deltaTime);
        float t = Mathf.Clamp01(age / life);
        Apply(t);
        if (age >= life) Destroy(gameObject);
    }

    private void Apply(float t)
    {
        if (spriteRenderer == null) return;
        if (frames != null && frames.Length > 0)
        {
            int i = Mathf.FloorToInt(age * fps);
            spriteRenderer.sprite = frames[loop ? i % frames.Length : Mathf.Min(i, frames.Length - 1)];
        }
        transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, t);
        Color c = color;
        if (fadeOut > 0f && t > 1f - fadeOut) c.a *= Mathf.Clamp01((1f - t) / fadeOut);
        spriteRenderer.color = c;
    }
}
