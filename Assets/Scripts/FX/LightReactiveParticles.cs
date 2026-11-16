using UnityEngine;

// makes room dust or embers react to your light (the state meter).
// low light: few, dim, cold motes. high light: more, warm, bright motes.
// put this on a particle system and drag in the player's playerstats.
[RequireComponent(typeof(ParticleSystem))]
public class LightReactiveParticles : MonoBehaviour
{
    [SerializeField] private PlayerStats stats;
    [SerializeField] private Color lowLightColor = new Color(0.55f, 0.5f, 0.7f, 0.35f);
    [SerializeField] private Color highLightColor = new Color(1f, 0.85f, 0.55f, 0.9f);
    [SerializeField] private float lowRate = 4f;
    [SerializeField] private float highRate = 14f;
    [Tooltip("How quickly it changes. Lower = slower, smoother.")]
    [SerializeField] private float blendSpeed = 1.5f;

    private ParticleSystem ps;
    private float shown;
    private float target;

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    private void Start()
    {
        if (stats == null) return;
        target = stats.MaxStateMeter > 0f ? stats.StateMeter / stats.MaxStateMeter : 0.5f;
        shown = target;
        stats.OnStateMeterChanged += HandleMeter;
    }

    private void OnDestroy()
    {
        if (stats != null) stats.OnStateMeterChanged -= HandleMeter;
    }

    private void HandleMeter(float current, float max) => target = max > 0f ? current / max : 0f;

    private void Update()
    {
        shown = Mathf.MoveTowards(shown, target, blendSpeed * Time.deltaTime);
        var main = ps.main;
        main.startColor = Color.Lerp(lowLightColor, highLightColor, shown);
        var emission = ps.emission;
        emission.rateOverTime = Mathf.Lerp(lowRate, highRate, shown);
    }
}
