using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

// soul spawner: gives lost souls a different spot each time they come back.
// make some empty objects as spawn points around the room and drag them in.
// drag this spawner into each soul's "spawner" slot. souls also start on random points.

public class SoulSpawner : MonoBehaviour
{
    [SerializeField] private LostSoul[] souls;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Transform player;

    [Header("Rules")]
    [Tooltip("Don't come back this close to the player.")]
    [SerializeField] private float minDistanceFromPlayer = 3f;
    [Tooltip("Don't come back this close to another soul.")]
    [SerializeField] private float minDistanceFromOtherSouls = 1.5f;
    [Tooltip("Put each soul on a random point when the scene starts.")]
    [SerializeField] private bool randomStartSpots = true;
    [Tooltip("Same number = same layout every run. 0 = random every run.")]
    [SerializeField] private int seed = 0;

    private System.Random rng;

    private void Awake()
    {
        rng = seed != 0 ? new System.Random(seed) : new System.Random();
        if (spawnPoints == null || spawnPoints.Length == 0)
            Debug.LogError("SoulSpawner needs drag insome spawn ponts");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        if (!randomStartSpots || souls == null) return;

        foreach (LostSoul s in souls)
            if (s != null) s.transform.position = PickNewSpot(s);
    }

    //it picks a free point not the one it was on and not the one near the player and not on another soul
    public Vector3 PickNewSpot(LostSoul soul)
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return soul.transform.position;

        var good = new List<Transform>();
        var okay = new List<Transform>();
        foreach (Transform p in spawnPoints)
        {
            if (p == null) continue;
            if (Vector2.Distance(p.position, soul.transform.position) < 0.1f) continue;//same spot
            if (NearOtherSoul(p.position, soul)) continue;
            okay.Add(p);
            if (player == null || Vector2.Distance(p.position, player.position) >= minDistanceFromPlayer)
                good.Add(p);
        }
        List<Transform> pool = good.Count > 0 ? good : okay;
        if (pool.Count == 0) return soul.transform.position;
        return pool[rng.Next(pool.Count)].position;
    }

    private bool NearOtherSoul(Vector3 pos, LostSoul self)
    {
        if (souls == null) return false;
        foreach (LostSoul s in souls)
            if (s != null && s != self && Vector2.Distance(s.transform.position, pos) < minDistanceFromOtherSouls)
                return true;

        return false;
    }

    private void OnDrawGizmos()
    {
        if (spawnPoints == null) return;
        Gizmos.color = new Color(0.55f, 0.85f, 1f, 0.8f);
        foreach (Transform p in spawnPoints)
            if (p != null) Gizmos.DrawWireSphere(p.position, 0.3f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
