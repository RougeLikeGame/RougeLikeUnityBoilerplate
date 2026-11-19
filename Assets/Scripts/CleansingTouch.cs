using System.Collections;
using UnityEngine;

public class CleansingTouch : Ability
{
    private IEnumerator GoldAnim(float duration, PlayerAbilities player)
    {
        float elapsedTime = 0f;
        Color originalColor = player.gameObject.GetComponent<SpriteRenderer>().color;
        Color targetColor = Color.gold;
        while (elapsedTime < duration/2)
        {
            float t = elapsedTime / duration;
            player.gameObject.GetComponent<SpriteRenderer>().color = Color.Lerp(originalColor, targetColor, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        while (elapsedTime < duration && elapsedTime > duration/2)
        {
            float t = (elapsedTime - duration/2) / (duration);
            player.gameObject.GetComponent<SpriteRenderer>().color = Color.Lerp(targetColor, originalColor, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        GetComponent<SpriteRenderer>().color = originalColor;

    }
    public override void Cast(PlayerAbilities player)
    {
        GameObject[] shadowBoundEnemies = GameObject.FindGameObjectsWithTag("ShadowBound");
        foreach (GameObject enemy in shadowBoundEnemies)
        {
            ShadowBoundEnemy shadowBoundEnemy = enemy.GetComponent<ShadowBoundEnemy>();
            if (shadowBoundEnemy != null && player.stats.CurrentPerception.name != Perception.LevelName.Unseen)
            {
                shadowBoundEnemy.Die();
            }
        }
        player.StartCoroutine(GoldAnim(0.5f, player));
    }
}
