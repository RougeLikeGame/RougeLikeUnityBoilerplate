using System;
using System.Collections.Generic;

// the blessings picked this run, and their combined effects.
// it's static, so blessings carry over when the tutorial loads the boss level.
// other scripts just read the numbers, for example: holdtime / blessings.comfortspeed
public static class Blessings
{
    private static readonly List<Blessing> taken = new List<Blessing>();
    public static IReadOnlyList<Blessing> Taken => taken;
    public static event Action OnChanged;

    public static float ComfortSpeed { get; private set; } = 1f;
    public static float HelpStrength { get; private set; } = 1f;
    public static float SoulRestTime { get; private set; } = 1f;
    public static float StartLight { get; private set; } = 0f;
    public static float AttackCooldown { get; private set; } = 1f;
    public static float SwordDamage { get; private set; } = 1f;
    public static float FeedBoss { get; private set; } = 1f;
    public static float MoveSpeed { get; private set; } = 1f;
    public static float ParryWindowBonus { get; private set; } = 0f;

    public static bool Has(Blessing b) => taken.Contains(b);

    public static void Add(Blessing b)
    {
        if (b == null) return;
        taken.Add(b);
        Recalculate();
    }

    // start a new run with nothing
    public static void Reset()
    {
        taken.Clear();
        Recalculate();
    }

    private static void Recalculate()
    {
        ComfortSpeed = HelpStrength = SoulRestTime = AttackCooldown = SwordDamage = FeedBoss = MoveSpeed = 1f;
        StartLight = ParryWindowBonus = 0f;
        foreach (Blessing b in taken)
        {
            // multipliers stack by multiplying, bonuses by adding
            ComfortSpeed *= b.comfortSpeed;
            HelpStrength *= b.helpStrength;
            SoulRestTime *= b.soulRestTime;
            AttackCooldown *= b.attackCooldown;
            SwordDamage *= b.swordDamage;
            FeedBoss *= b.feedBoss;
            MoveSpeed *= b.moveSpeed;
            StartLight += b.startLight;
            ParryWindowBonus += b.parryWindow;
        }
        OnChanged?.Invoke();
    }
}
