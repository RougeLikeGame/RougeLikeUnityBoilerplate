using UnityEngine;
using System.Collections;
public class HealingWave : MonoBehaviour
{
    private IEnumerator Wave(float duration) 
    { 
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            // Heal the player over time
            elapsedTime += Time.deltaTime;
            gameObject.transform.localScale += new Vector3(10f, 10f, 0f) * Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }

    
    void Start()
    {
        StartCoroutine(Wave(1f));
    }

   
}
