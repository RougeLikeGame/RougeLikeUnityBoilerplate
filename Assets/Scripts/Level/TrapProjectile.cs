using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class TrapProjectile : MonoBehaviour
{
    [SerializeField] private float lifeTime = 4f;
    [SerializeField] private bool hurtEnemies = true;
    [Tooltip("layers that stop the arrows walls and obstacles")]
    [SerializeField] private LayerMask blockingLayers;
    [Tooltip("the art pots left by defualt tick if it is pointign right ")]
    [SerializeField] private bool artPointsRight = false;

    private Rigidbody2D rb;
    private float damage = 1f;
    private GameObject owner;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    public void Launch(Vector2 velocity, float dmg, GameObject from)
    {
        damage = dmg;
        owner = from;
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = velocity;
#else
    rb.velocity = velocity;
#endif
        //turn the sprite to pace where it is flying
        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        if (!artPointsRight) angle += 180f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null && other.transform.IsChildOf(owner.transform)) return;

        PlayerStats ps = other.GetComponentInParent<PlayerStats>();
        if(ps != null)
        {
            if (!ps.IsDead && !ps.IsInvincible) ps.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if(hurtEnemies)
        {
            EnemyHealth eh = other.GetComponentInParent<EnemyHealth>();
            if(eh != null && eh.TakeDamage(damage,this.gameObject)) { Destroy(gameObject); return; }
        }

        //if it hits a wall or rock
        if ((blockingLayers.value & (1 << other.gameObject.layer)) != 0)
            Destroy(gameObject);

    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
