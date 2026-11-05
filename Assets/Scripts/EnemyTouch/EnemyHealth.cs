using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 3f;

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    //Set by the enemy when it can't be hit (underground, in the sky)
    public bool Invulnerable { get; set; }

    public event Action<float> OnHurt;   //damage taken
    public event Action OnDied;

    //Every living enemy, so debug tools (and later, room-clear checks) can find them.
    private static readonly List<EnemyHealth> all = new List<EnemyHealth>();
    public static IReadOnlyList<EnemyHealth> All => all;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    private void OnEnable()  => all.Add(this);
    private void OnDisable() => all.Remove(this);

    //Returns true if the hit actually landed
    public bool TakeDamage(float amount)
    {
        if (IsDead || Invulnerable || amount <= 0f) return false;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        OnHurt?.Invoke(amount);

        if (CurrentHealth <= 0f)
        {
            IsDead = true;
            OnDied?.Invoke();
        }
        return true;
    }
}
