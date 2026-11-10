using UnityEngine;

public class Breakable : MonoBehaviour
{
    [SerializeField] private int hitsToBreak = 2;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("Optional sprite shown after breaking (debris). Leave empty to hide the object.")]
    [SerializeField] private Sprite brokenSprite;
    [Tooltip("The collider that blocks movement. Turned off when broken.")]
    [SerializeField] private Collider2D blockingCollider;
    [Tooltip("Optional: the room's grid, rebuilt when this breaks.")]
    [SerializeField] private PathGrid pathGrid;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hitClip;
    [SerializeField] private AudioClip breakClip;

    public bool IsBroken { get; private set; }

    private int hits;
    private float shake;
    private Vector3 basePos;

    private void Awake()
    {
        if (spriteRenderer != null) basePos = spriteRenderer.transform.localPosition;
    }

    //it need to be called by the sword in playermovemnt and returns true if the hit couted
    public bool Hit()
    {
        if (IsBroken) return false;
        hits++;
        shake = 0.15f;

        if(hits < hitsToBreak)
        {
            Play(hitClip);
            return true;

        }

        IsBroken = true;
        Play(breakClip);
        if (blockingCollider != null) blockingCollider.enabled = false;
        if (spriteRenderer != null)
        {
            if (brokenSprite != null) spriteRenderer.sprite = brokenSprite;
            else spriteRenderer.enabled = false;
        }
        if (pathGrid != null) pathGrid.Rebuild();
        return true;
    }

   private void Update()
    {
        if (spriteRenderer == null || shake <= 0f) return;
        shake -= Time.deltaTime;
        float s = Mathf.Max(0f, shake) * 0.4f;
        spriteRenderer.transform.localPosition = basePos + new Vector3(Mathf.Sin(Time.time * 90f) * s, 0f, 0f);
        if (shake <= 0f) spriteRenderer.transform.localPosition = basePos;
    }

    private void Play(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }

}

