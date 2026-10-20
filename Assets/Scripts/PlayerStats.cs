using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 6f; 
    [SerializeField] private float currentHealth = 6f;
    [Tooltip("Seconds of invincibility after taking a hit")]
    [SerializeField] private float invincibilityTime = 1f;

    [Header("State Meter")]
    [SerializeField] private float maxStateMeter = 100f;
    [SerializeField] private float stateMeter = 50f;

    [Tooltip("At or above this, hidden entities start to be glimpsed.")]
    [SerializeField] private float glimpsedThreshold = 40f;
    [Tooltip("At or above this, hidden entities are fully visible.")]
    [SerializeField] private float visibleThreshold = 75f;

    [Header("Other (not used yet)")]
    [SerializeField] private float damageTakenMultiplier = 1f; 
    [SerializeField] private int helpfulActionsDone;          
    [SerializeField] private int aggressiveActionsDone;     

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0f;
    public bool IsInvincible => invincibleTimer > 0f;

    public float StateMeter => stateMeter;
    public float MaxStateMeter => maxStateMeter;
    public float StateMeter01 => maxStateMeter > 0f ? stateMeter / maxStateMeter : 0f;

    public float DamageTakenMultiplier
    {
        get => damageTakenMultiplier;
        set => damageTakenMultiplier = Mathf.Max(0f, value);
    }

    public int HelpfulActionsDone => helpfulActionsDone;
    public int AggressiveActionsDone => aggressiveActionsDone;

    public PerceptionLevel CurrentPerception
    {
        get
        {
            if (stateMeter >= visibleThreshold) return PerceptionLevel.Visible;
            if (stateMeter >= glimpsedThreshold) return PerceptionLevel.Glimpsed;
            return PerceptionLevel.Unseen;
        }
    }

    public event Action<float, float> OnHealthChanged;   
    public event Action<float> OnDamaged;                 
    public event Action<float> OnHealed;                    
    public event Action OnDied;
    public event Action<float, float> OnStateMeterChanged;
    public event Action<PerceptionLevel> OnPerceptionChanged;

    private float invincibleTimer;
    private PerceptionLevel lastPerception;

    private void Awake()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        stateMeter = Mathf.Clamp(stateMeter, 0f, maxStateMeter);
        lastPerception = CurrentPerception;
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnStateMeterChanged?.Invoke(stateMeter, maxStateMeter);
    }

    private void Update()
    {
        if (invincibleTimer > 0f)
            invincibleTimer -= Time.deltaTime;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || IsInvincible || amount <= 0f) return;

        float finalAmount = amount * damageTakenMultiplier;
        currentHealth = Mathf.Max(0f, currentHealth - finalAmount);
        invincibleTimer = invincibilityTime;

        OnDamaged?.Invoke(finalAmount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (IsDead)
            OnDied?.Invoke();
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;

        float before = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

        OnHealed?.Invoke(currentHealth - before);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void SetMaxHealth(float newMax, bool fillToMax = false)
    {
        maxHealth = Mathf.Max(1f, newMax);
        currentHealth = fillToMax ? maxHealth : Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }


    public void ChangeStateMeter(float amount)
    {
        SetStateMeter(stateMeter + amount);
    }

    public void SetStateMeter(float value)
    {
        stateMeter = Mathf.Clamp(value, 0f, maxStateMeter);
        OnStateMeterChanged?.Invoke(stateMeter, maxStateMeter);

        PerceptionLevel now = CurrentPerception;
        if (now != lastPerception)
        {
            lastPerception = now;
            OnPerceptionChanged?.Invoke(now);
        }
    }

    public void RegisterHelpfulAction() => helpfulActionsDone++;
    public void RegisterAggressiveAction() => aggressiveActionsDone++;

#if UNITY_EDITOR

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        maxStateMeter = Mathf.Max(1f, maxStateMeter);
        stateMeter = Mathf.Clamp(stateMeter, 0f, maxStateMeter);
        visibleThreshold = Mathf.Max(glimpsedThreshold, visibleThreshold);
    }
#endif
}

public enum PerceptionLevel
{
    Unseen,
    Glimpsed,
    Visible
}
