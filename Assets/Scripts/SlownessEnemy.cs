using UnityEngine;
using UnityEngine.InputSystem;

public class SlownessEnemy : MonoBehaviour
{
    [SerializeField] PlayerStats player;
    [SerializeField] PlayerMovement playerMove;
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] float movementPunish = 0.75f;

    private float originalSpeedMultiplier;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        originalSpeedMultiplier = playerMove.speedMultiplier;
        playerMove.speedMultiplier *= movementPunish; 
    }

    // Update is called once per frame
    void Update()
    {
        if(player.CurrentPerception.name == Perception.LevelName.Unseen) 
        {
            sprite.color = new Color(1f, 1f, 1f, 0.0f);
        }
        else
        {
            sprite.color = new Color(1f, 1f, 1f, player.CurrentPerception.value);
        }
        Keyboard kb = Keyboard.current;
        if (kb == null) return;
        if (kb.fKey.wasPressedThisFrame) 
        {
            Die();
        }
    }

    void Die() 
    {
        playerMove.speedMultiplier = originalSpeedMultiplier;
        GameObject.Destroy(this.gameObject);
    
    }
}
