using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyDebugKeys : MonoBehaviour
{
    [SerializeField] private float damage = 1f;

    private void Update()
    {
        if (!Debug.isDebugBuild) return;
        Keyboard kb = Keyboard.current;
        if (kb == null || !kb.digit6Key.wasPressedThisFrame) return;

        int hits = 0;
        foreach (EnemyHealth e in new List<EnemyHealth>(EnemyHealth.All))
            if (e.TakeDamage(damage)) hits++;

        Debug.Log($"[Enemies] Debug hit: {hits} enemies took {damage} damage.");
    }
}
