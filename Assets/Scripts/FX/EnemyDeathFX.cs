using System.Collections;
using UnityEngine;

// makes enemy deaths land: a smoke puff, the sprite squashes flat, and a soul wisp drifts up.
// put this on each enemy prefab (next to enemyhealth) and drag in the prefabs.
public class EnemyDeathFX : MonoBehaviour
{
    [SerializeField] private EnemyHealth health;
    [Tooltip("The enemy's sprite (the one that should squash).")]
    [SerializeField] private SpriteRenderer body;

    [Header("Prefabs (SpriteFX)")]
    [SerializeField] private SpriteFX smokePrefab;
    [SerializeField] private SpriteFX wispPrefab;

    [Header("Where")]
    [Tooltip("Smoke spawns here, relative to the enemy (its feet).")]
    [SerializeField] private Vector2 smokeOffset = new Vector2(0f, 0.2f);
    [Tooltip("The wisp starts here (about chest height).")]
    [SerializeField] private Vector2 wispOffset = new Vector2(0f, 0.6f);
    [SerializeField] private int smokeCount = 2;

    [Header("Squash")]
    [Tooltip("Wait this long first so a death animation can play (0 = squash right away).")]
    [SerializeField] private float squashDelay = 0.15f;
    [SerializeField] private float squashTime = 0.18f;
    [Tooltip("Hide the sprite after it's flat.")]
    [SerializeField] private bool hideWhenFlat = true;

    private void Awake()
    {
        if (health == null) health = GetComponent<EnemyHealth>();
        if (health == null || body == null) Debug.LogError("EnemyDeathFX: drag in Health and Body.", this);
    }

    private void OnEnable()  { if (health != null) health.OnDied += Play; }
    private void OnDisable() { if (health != null) health.OnDied -= Play; }

    private void Play() => StartCoroutine(DeathRoutine());

    private IEnumerator DeathRoutine()
    {
        // smoke puff around the feet
        for (int i = 0; i < smokeCount; i++)
        {
            if (smokePrefab == null) break;
            Vector2 jitter = Random.insideUnitCircle * 0.25f;
            Instantiate(smokePrefab, (Vector2)transform.position + smokeOffset + jitter, Quaternion.identity);
        }

        // the soul leaves the body
        if (wispPrefab != null)
            Instantiate(wispPrefab, (Vector2)transform.position + wispOffset, Quaternion.identity);

        if (body == null) yield break;
        if (squashDelay > 0f) yield return new WaitForSeconds(squashDelay);

        // squash flat and wide, from the feet
        Transform t = body.transform;
        Vector3 start = t.localScale;
        Vector3 flat = new Vector3(start.x * 1.4f, start.y * 0.05f, start.z);
        float time = 0f;
        while (time < squashTime)
        {
            time += Time.deltaTime;
            float p = time / squashTime;
            t.localScale = Vector3.Lerp(start, flat, p * p);
            yield return null;
        }
        if (hideWhenFlat) body.enabled = false;
    }
}
