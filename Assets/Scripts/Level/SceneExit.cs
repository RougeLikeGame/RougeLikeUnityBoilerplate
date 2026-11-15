using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// walk into this to go to the next scene (for example from the tutorial to the boss level).
public class SceneExit : MonoBehaviour
{
    [SerializeField] private string sceneName = "BossLevel";
    [Tooltip("Optional: a full-screen black CanvasGroup to fade out with.")]
    [SerializeField] private CanvasGroup fade;
    [SerializeField] private float fadeTime = 0.5f;
    [Tooltip("Optional: only open once this spawner is cleared.")]
    [SerializeField] private EnemySpawner needCleared;

    private bool leaving;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (leaving || other.GetComponentInParent<PlayerStats>() == null) return;
        if (needCleared != null && !needCleared.Cleared) return;
        StartCoroutine(Leave());
    }

    private IEnumerator Leave()
    {
        leaving = true;
        if (fade != null)
        {
            fade.blocksRaycasts = true;
            float t = 0f;
            while (t < fadeTime)
            {
                t += Time.unscaledDeltaTime;
                fade.alpha = Mathf.Clamp01(t / fadeTime);
                yield return null;
            }
        }
        SceneManager.LoadScene(sceneName);
    }
}
