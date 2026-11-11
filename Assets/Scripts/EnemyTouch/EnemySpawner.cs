using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemySpawner : MonoBehaviour
{
    public enum StartMode { OnStart, OnPlayerEnterTrigger, Manual}
    [Serializable]
    public class SpawnEntry
    {
        public EnemyBase prefab;
        [Min(1)] public int count = 1;
    }

    [Serializable]
    public class Wave
    {
        public string name = "Wave";
        public SpawnEntry[] enemies;
        [Tooltip("Seconds to wait before this wave starts")]
        public float delayBefore = 1f;
    }

    [Header("References the drag in stuff")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Transform player;
    [Tooltip("the room is a A* grid given oto everu spawned enemy")]
    [SerializeField] private PathGrid pathGrid;
    [Tooltip("empty objects palced around the room where eneimes may apper")]
    [SerializeField] private Transform[] spawnPoints;
    [Tooltip("a marker sprite that is turned off and copied at each spaw point as a possible warning")]
    [SerializeField] private SpriteRenderer spawnMarkerTemplate;

    [Header("Waves")]
    [SerializeField] private Wave[] waves;

    [Header("Rules")]
    [SerializeField] private StartMode startMode = StartMode.OnPlayerEnterTrigger;
    [SerializeField] private float minDistanceFromPlayer = 3f;
    [SerializeField] private float spawnWarningTime = 0.8f;
    [SerializeField] private float delayBetweenSpawns = 0.35f;
    [SerializeField] private int maxAlive = 5;
    [Tooltip("0=random every run and any other number = the smae layout every time")]
    [SerializeField] private int seed = 0;

    [Header("Room lock")]
    [Tooltip("door blockers turned on while fihgting and off when cleared")]
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
            Debug.LogError("EnemySpawner need drag in player stats and player");

        if (spawnPoints == null || spawnPoints.Length == 0)
            Debug.LogError("EnemySpawner need to add at least one spawn point");

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

    //it starts the encounter 
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
            foreach(Wave wave in waves)
            {
                yield return new WaitForSeconds(wave.delayBefore);

                List<EnemyBase> queue = BuildQueue(wave);
                foreach(EnemyBase prefab in queue)
                {
                    while (Alive >= maxAlive || PlayerIsDead) yield return null;
                    yield return SpawnOne(prefab);
                    yield return new WaitForSeconds(delayBetweenSpawns);
                }
                //the wave is done when everythign in it is dead 
                while (Alive > 0) yield return null;
            }
        }
        Running = false;
        Cleared = true;
        Debug.Log($"[Spawner] '{name}' cleared");
        onAllCleared?.Invoke();
    }

    private List<EnemyBase> BuildQueue(Wave wave)
    {
        var queue = new List<EnemyBase>();
        if (wave.enemies != null)
            foreach (SpawnEntry e in wave.enemies)
                if (e != null && e.prefab != null)
                    for (int i = 0; i < e.count; i++) queue.Add(e.prefab);

        //shuffle the order 
        for(int i = queue.Count - 1;  i > 0; i--)
        {
            int j = rng.Next(i + 1);
            EnemyBase t = queue[i]; queue[i] = queue[j]; queue[j] = t;
        }
        return queue;
    }

    private IEnumerator SpawnOne(EnemyBase prefab)
    {
        Transform point = PickPoint();
        if (point == null) yield break;
        Vector3 pos = point.position;

        // Telegraph: a pulsing marker so the player is never surprised.
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

        EnemyBase enemy = Instantiate(prefab, pos, Quaternion.identity);
        enemy.SetTarget(playerStats, player, pathGrid);
        Alive++;

        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (health != null) health.OnDied += () => Alive = Mathf.Max(0, Alive - 1);
        else Debug.LogWarning($"[Spawner] {prefab.name} has no EnemyHealth on its root, so the spawner can't tell when it dies.", enemy);
    }

    //a reandom spawn point that is far enoguh from the palyer 
    private Transform PickPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return null;

        var fair = new List<Transform>();
        Transform farthest = null;
        float farthestDist = -1f;

        foreach(Transform p in spawnPoints)
        {
            if (p == null) continue;
            float d = player != null ? Vector2.Distance(p.position.normalized, player.position) : 999f;
            if (d >= minDistanceFromPlayer) fair.Add(p);
            if(d > farthestDist) { farthestDist = d; farthest = p; }
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
