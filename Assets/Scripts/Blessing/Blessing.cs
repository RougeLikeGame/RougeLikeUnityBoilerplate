using UnityEngine;

// one blessing the player can pick after clearing a room.
// make them in the project window: right click > create > ascendant > blessing.
// 1 = no change for multipliers, 0 = no change for bonuses.
[CreateAssetMenu(menuName = "Ascendant/Blessing", fileName = "Blessing")]
public class Blessing : ScriptableObject
{
    [Header("Card")]
    public string title = "Gentle Hands";
    [TextArea(2, 3)] public string description = "comfort the lost faster";
    public Sprite icon;
    [Tooltip("Can this be picked more than once in a run?")]
    public bool stackable = false;

    [Header("Kindness")]
    [Tooltip("2 = comforting takes half as long.")]
    public float comfortSpeed = 1f;
    [Tooltip("How strong each comfort is against the boss.")]
    public float helpStrength = 1f;
    [Tooltip("0.6 = souls come back 40% sooner.")]
    public float soulRestTime = 1f;
    [Tooltip("Light added at the start of each level (and right away when picked).")]
    public float startLight = 0f;

    [Header("Blade")]
    [Tooltip("0.6 = attack cooldown is 40% shorter.")]
    public float attackCooldown = 1f;
    [Tooltip("Damage to regular enemies.")]
    public float swordDamage = 1f;
    [Tooltip("How much each hit feeds the boss. Above 1 is the cost of a strong blade.")]
    public float feedBoss = 1f;

    [Header("Body")]
    public float moveSpeed = 1f;
    [Tooltip("Seconds added to the parry window.")]
    public float parryWindow = 0f;
}
