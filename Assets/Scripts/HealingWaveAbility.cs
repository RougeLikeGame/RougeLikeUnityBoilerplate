using UnityEngine;

public class HealingWaveAbility : Ability
{
    [SerializeField] float healAmount = 2f;
    [SerializeField] GameObject healingWavePrefab;
    public override void Cast(PlayerAbilities player)
    {
        PlayerStats playerStats = player.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.Heal(healAmount);
        }
        Instantiate(healingWavePrefab, player.transform.position, Quaternion.identity);
    }
}
