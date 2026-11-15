using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemySpawner : MonoBehaviour
{
    public enum StartMode { OnStart, OnPlayerEnterTrigger, Manual }

    [Serializable]
    public class SpawnEntry
    {
        [Tooltip("Drag the enemy PREFAB here from the Project window (e.g. Assets/PreFabs/Walker).")]
        public GameObject prefab;
        [Min(1)] public int count = 1;
    }

    [Serializable]
    public class Wave
    {
        public string name = "Wave";
        public SpawnEntry[] enemies;
        [Tooltip("Seconds to wait before this wave starts.")]
        public float delayBefore = 1f;
    }

    [Header("References (drag in)")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Transform player;
    [Tooltip("Optional: the room's A* grid, given to every spawned enemy.")]
    [SerializeField] private PathGrid pathGrid;
    [Tooltip("Empty objects placed around the room where enemies may appear.")]
    [SerializeField] private Transform[] spawnPoints;
    [Tooltip("Optional: a marker sprite (turned OFF in the scene) copied at each spawn point as a warning.")]
    [SerializeField] private SpriteRenderer spawnMarkerTemplate;

    [Header("Waves")]
    [SerializeField] private Wave[] waves;

    [Header("Rules")]
    [SerializeField] private StartMode startMode = StartMode.OnPlayerEnterTrigger;
    [SerializeField] private float minDistanceFromPlayer = 3f;
    [SerializeField] private float spawnWarningTime = 0.8f;
    [SerializeField] private float delayBetweenSpawns = 0.35f;
    [SerializeField] private int maxAlive = 5;
    [Tooltip("0 = random every run. Any other number = the same layout every time.")]
    [SerializeField] private int seed = 0;

    [Header("Room lock")]
    [Tooltip("Optional: door blockers turned ON while fighting and OFF when cleared.")]
    [SerializeField] private GameObject[] lockObjects;

    [Header("Events")]
    public UnityEvent onStarted;
    public UnityEvent onAllCleared;

    public int Alive { get; private set; }
    public bool Running { get; private set; }
    public bool Cleared { get; private set; }

    private System.Random rng;

    private void Awake()
    {
        if (playerStats == null || player == null)
            Debug.LogError("EnemySpawner: drag in Player Stats and Player.", this);
        if (spawnPoints == null || spawnPoints.Length == 0)
            Debug.LogError("EnemySpawner: add at least one Spawn Point.", this);
        if (spawnMarkerTemplate != null) spawnMarkerTemplate.gameObject.SetActive(false);
        SetLocks(false);
    }

    private void Start()
    {
        if (startMode == StartMode.OnStart) StartSpawning();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (startMode != StartMode.OnPlayerEnterTrigger || Running || Cleared) return;
        if (playerStats != null && other.GetComponentInParent<PlayerStats>() == playerStats)
            StartSpawning();
    }

    //start the encounter 
    public void StartSpawning()
    {
        if (Running || Cleared) return;
        int usedSeed = seed != 0 ? seed : Environment.TickCount;
        rng = new System.Random(usedSeed);
        Debug.Log($"[Spawner] '{name}' started with seed {usedSeed}");
        Running = true;
        SetLocks(true);
        onStarted?.Invoke();
        StartCoroutine(RunWaves());
    }

    private IEnumerator RunWaves()
    {
        if (waves != null)
        {
            foreach (Wave wave in waves)
            {
                yield return new WaitForSeconds(wave.delayBefore);

                List<GameObject> queue = BuildQueue(wave);
                foreach (GameObject prefab in queue)
                {
                    while (Alive >= maxAlive || PlayerIsDead) yield return null;
                    yield return SpawnOne(prefab);
                    yield return new WaitForSeconds(delayBetweenSpawns);
                }

                //wave is done when everything in it is dead.
                while (Alive > 0) yield return null;
            }
        }

        Running = false;
        Cleared = true;
        SetLocks(false);
        Debug.Log($"[Spawner] '{name}' cleared!");
        onAllCleared?.Invoke();
    }

    private List<GameObject> BuildQueue(Wave wave)
    {
        var queue = new List<GameObject>();
        if (wave.enemies != null)
            foreach (SpawnEntry e in wave.enemies)
                if (e != null && e.prefab != null)
                    for (int i = 0; i < e.count; i++) queue.Add(e.prefab);

        //shuffle the order (Fisher-Yates).
        for (int i = queue.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            GameObject t = queue[i]; queue[i] = queue[j]; queue[j] = t;
        }
        return queue;
    }

    private IEnumerator SpawnOne(GameObject prefab)
    {
        Transform point = PickPoint();
        if (point == null) yield break;
        Vector3 pos = point.position;

        //telegraph: a pulsing marker so the player is never surprised.
        SpriteRenderer marker = null;
        if (spawnMarkerTemplate != null)
        {
            marker = Instantiate(spawnMarkerTemplate, pos, Quaternion.identity);
            marker.gameObject.SetActive(true);
            Vector3 baseScale = spawnMarkerTemplate.transform.localScale;
            float t = 0f;
            while (t < spawnWarningTime)
            {
                t += Time.deltaTime;
                float pulse = 0.8f + 0.2f * Mathf.Sin(t * 18f);
                marker.transform.localScale = baseScale * pulse * Mathf.Lerp(0.4f, 1f, t / spawnWarningTime);
                yield return null;
            }
            Destroy(marker.gameObject);
        }
        else
        {
            yield return new WaitForSeconds(spawnWarningTime);
        }

        GameObject enemy = Instantiate(prefab, pos, Quaternion.identity);
        enemy.SetActive(true); //in case the prefab (or a scene copy) was saved switched off

        //The enemy script can be on the root or a child.
        EnemyBase brain = enemy.GetComponentInChildren<EnemyBase>(true);
        if (brain != null) brain.SetTarget(playerStats, player, pathGrid);
        else Debug.LogWarning($"[Spawner] {prefab.name} has no Walker/Burrower/SkyDiver script, so it won't know where the player is.", enemy);
        Alive++;

        EnemyHealth health = enemy.GetComponentInChildren<EnemyHealth>(true);
        if (health != null) health.OnDied += () => Alive = Mathf.Max(0, Alive - 1);
        else Debug.LogWarning($"[Spawner] {prefab.name} has no EnemyHealth, so the spawner can't tell when it dies.", enemy);
    }

    //A random spawn point that's far enough from the player 
    private Transform PickPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return null;

        var fair = new List<Transform>();
        Transform farthest = null;
        float farthestDist = -1f;

        foreach (Transform p in spawnPoints)
        {
            if (p == null) continue;
            float d = player != null ? Vector2.Distance(p.position, player.position) : 999f;
            if (d >= minDistanceFromPlayer) fair.Add(p);
            if (d > farthestDist) { farthestDist = d; farthest = p; }
        }

        return fair.Count > 0 ? fair[rng.Next(fair.Count)] : farthest;
    }

    private bool PlayerIsDead => playerStats != null && playerStats.IsDead;

    private void SetLocks(bool on)
    {
        if (lockObjects == null) return;
        foreach (GameObject g in lockObjects)
            if (g != null) g.SetActive(on);
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnPoints == null) return;
        foreach (Transform p in spawnPoints)
        {
            if (p == null) continue;
            Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.9f);
            Gizmos.DrawWireSphere(p.position, 0.3f);
        }
        if (player != null)
        {
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.4f);
            Gizmos.DrawWireSphere(player.position, minDistanceFromPlayer);
        }
    }
}
