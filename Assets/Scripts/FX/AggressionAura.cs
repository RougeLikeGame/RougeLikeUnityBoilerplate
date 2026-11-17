using UnityEngine;

// dark smoke clings to the player after they hit the boss.
// every hit adds "stain". it fades slowly on its own, and comforting a soul cleans it faster.
// this shows that hitting the boss is wrong, without any text.
// put this on the player and drag in the boss and a smoke prefab.
public class AggressionAura : MonoBehaviour
{
    [SerializeField] private ScarcityRemnant boss;
    [Tooltip("Smoke prefab (SpriteFX). It gets tinted dark by this script.")]
    [SerializeField] private SpriteFX smokePrefab;
    [Tooltip("Optional: a soft dark sprite under the player that gets stronger with the stain.")]
    [SerializeField] private SpriteRenderer auraRenderer;

    [Header("Stain")]
    [SerializeField] private float stainPerHit = 1f;
    [SerializeField] private float maxStain = 4f;
    [Tooltip("How much stain fades each second on its own.")]
    [SerializeField] private float fadePerSecond = 0.15f;
    [Tooltip("How much stain one comfort cleans away.")]
    [SerializeField] private float cleanPerHelp = 2f;

    [Header("Smoke")]
    [Tooltip("Smoke puffs per second at full stain.")]
    [SerializeField] private float maxPuffsPerSecond = 6f;
    [SerializeField] private float spreadRadius = 0.35f;
    [SerializeField] private Vector2 centerOffset = new Vector2(0f, 0.4f);
    [SerializeField] private Color smokeColor = new Color(0.2f, 0.08f, 0.25f, 0.85f);
    [SerializeField] private Color auraColor = new Color(0.15f, 0.02f, 0.2f, 0.6f);

    public float Stain01 => maxStain > 0f ? stain / maxStain : 0f;

    private float stain;
    private float puffTimer;

    private void OnEnable()
    {
        if (boss == null) return;
        boss.OnFed += HandleFed;
        boss.OnWeakened += HandleHelped;
        boss.OnTransformed += Clear;
    }

    private void OnDisable()
    {
        if (boss == null) return;
        boss.OnFed -= HandleFed;
        boss.OnWeakened -= HandleHelped;
        boss.OnTransformed -= Clear;
    }

    private void HandleFed() => stain = Mathf.Min(maxStain, stain + stainPerHit);
    private void HandleHelped() => stain = Mathf.Max(0f, stain - cleanPerHelp);
    private void Clear() => stain = 0f;

    private void Update()
    {
        stain = Mathf.Max(0f, stain - fadePerSecond * Time.deltaTime);
        float s = Stain01;

        if (auraRenderer != null)
        {
            Color c = auraColor;
            c.a *= s;
            auraRenderer.color = c;
        }

        if (smokePrefab == null || s <= 0.01f) return;
        puffTimer -= Time.deltaTime;
        if (puffTimer > 0f) return;
        puffTimer = 1f / Mathf.Max(0.5f, maxPuffsPerSecond * s);

        Vector2 pos = (Vector2)transform.position + centerOffset + Random.insideUnitCircle * spreadRadius;
        SpriteFX puff = Instantiate(smokePrefab, pos, Quaternion.identity);
        puff.SetColor(smokeColor);
    }
}
