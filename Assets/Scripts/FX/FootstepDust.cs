using UnityEngine;

// little dust puffs at the player's feet while sprinting.
// put this on the player and drag in a dust prefab (spritefx).
public class FootstepDust : MonoBehaviour
{
    [SerializeField] private PlayerMovement player;
    [SerializeField] private SpriteFX dustPrefab;
    [Tooltip("Where the feet are, relative to the player.")]
    [SerializeField] private Vector2 feetOffset = new Vector2(0f, 0.05f);
    [Tooltip("Seconds between puffs while sprinting.")]
    [SerializeField] private float interval = 0.18f;
    [Tooltip("Also puff (slower) while just walking.")]
    [SerializeField] private bool puffWhenWalking = false;
    [SerializeField] private float walkInterval = 0.4f;

    private float timer;
    private bool leftFoot;

    private void Update()
    {
        if (player == null || dustPrefab == null) return;
        bool sprinting = player.IsSprinting;
        bool walking = player.IsMoving && puffWhenWalking;
        if (!sprinting && !walking) { timer = 0f; return; }

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = sprinting ? interval : walkInterval;

        // puff a little behind the player, swapping feet each time
        Vector2 back = -player.MoveInput.normalized * 0.15f;
        Vector2 side = new Vector2(leftFoot ? -0.08f : 0.08f, 0f);
        leftFoot = !leftFoot;
        Instantiate(dustPrefab, (Vector2)transform.position + feetOffset + back + side, Quaternion.identity);
    }
}
