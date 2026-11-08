using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerSpriteAnimator : MonoBehaviour
{
    [System.Serializable]
    public class DirectionFrames
    {
        public Sprite[] idle;
        public Sprite[] walk;
        public Sprite[] attack;
    }

    [Header("References")]
    [Tooltip("The SpriteRenderer that shows the character (can be on a child object).")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Frames")]
    [SerializeField] private DirectionFrames down;
    [SerializeField] private DirectionFrames up;
    [SerializeField] private DirectionFrames left;
    [SerializeField] private DirectionFrames right;

    [Header("Speed (frames per second)")]
    [SerializeField] private float idleFps = 10f;
    [SerializeField] private float walkFps = 10f;
    [SerializeField] private float attackFps = 10f;
    [Tooltip("Walk animation plays this many times faster while sprinting.")]
    [SerializeField] private float sprintFpsMultiplier = 1.5f;

    public enum Facing { Down, Right, Up, Left }
    public Facing CurrentFacing { get; private set; } = Facing.Down;

    private PlayerMovement movement;
    private Sprite[] currentFrames;
    private int frameIndex;
    private float frameTimer;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        movement.attackDuration =  GetFrames(Facing.Down, false, true, out bool flipX).Length/ attackFps;
    }

    //LateUpdate runs after PlayerMovement.Update, so facing is already up to date.
    private void LateUpdate()
    {
        if (spriteRenderer == null) return;

        CurrentFacing = GetFacing(movement.FacingDirection);

        bool moving = movement.IsMoving;
        bool attacking = movement.IsAttacking;
        Sprite[] frames = GetFrames(CurrentFacing, moving, attacking, out bool flipX);

        //Restart the animation when switching to a different set of frames.
        if (frames != currentFrames)
        {
            currentFrames = frames;
            frameIndex = 0;
            frameTimer = 0f;
        }

        if (GetFacing(movement.FacingDirection) != Facing.Left)
            spriteRenderer.flipX = flipX;

        if (currentFrames == null || currentFrames.Length == 0) return;

        float fps = attacking ? attackFps : moving ? walkFps : idleFps;
        if (movement.IsSprinting) fps *= sprintFpsMultiplier;

        frameTimer += Time.deltaTime;
        float frameTime = 1f / Mathf.Max(0.01f, fps);
        while (frameTimer >= frameTime)
        {
            frameTimer -= frameTime;
            frameIndex = (frameIndex + 1) % currentFrames.Length;
        }

        spriteRenderer.sprite = currentFrames[frameIndex];
    }

    private static Facing GetFacing(Vector2 dir)
    {
        //Pure diagonals (e.g. W + D on keyboard) count as up/down.
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            return dir.x < 0f ? Facing.Left : Facing.Right;
        return dir.y > 0f ? Facing.Up : Facing.Down;
    }

    private Sprite[] GetFrames(Facing facing, bool moving, bool attacking, out bool flipX)
    {
        flipX = false;
        DirectionFrames set = facing switch
        {
            Facing.Up => up,
            Facing.Left => left,
            Facing.Right => right,
            _ => down
        };

        if (facing == Facing.Left && !HasFrames(left))
        {
            set = right;
            flipX = true;
        }

        Sprite[] frames = attacking ? set?.attack : moving ? set?.walk : set?.idle;

        //If a walk or idle array is missing, use the other one so something still shows.
        if (frames == null || frames.Length == 0)
            frames = moving ? set?.idle : set?.walk;

        return frames;
    }

    private static bool HasFrames(DirectionFrames d) =>
        d != null && ((d.idle != null && d.idle.Length > 0) || (d.walk != null && d.walk.Length > 0) || (d.attack != null && d.attack.Length > 0));
}