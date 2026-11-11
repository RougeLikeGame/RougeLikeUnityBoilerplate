using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SlownessEnemy : ShadowBoundEnemy
{
    [SerializeField] PlayerStats player;
    [SerializeField] PlayerMovement playerMove;
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] float movementPunish = 0.75f;
    [SerializeField] SpriteRenderer deathSprite;

    private float originalSpeedMultiplier;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private IEnumerator deathAnimation(float duration) 
    { 
        float elapsedTime = 0f;
        Color originalColor = deathSprite.color;
      
        while (elapsedTime < duration/2)
        {
            float t = elapsedTime / (duration/2);
            deathSprite.color = new Color(originalColor.r, originalColor.g, originalColor.b, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        while (elapsedTime < duration && elapsedTime > duration/2)
        {
            float t = (elapsedTime - duration/2) / (duration/2);
            deathSprite.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1-t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        GameObject.Destroy(this.gameObject);
    }
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

    public override void Die() 
    {
        playerMove.speedMultiplier = originalSpeedMultiplier;
        StartCoroutine(deathAnimation(0.5f));

    }
}
