using UnityEngine;
using UnityEngine.UI;

public class AttackReadyIndicator : MonoBehaviour
{
    [SerializeField] private PlayerMovement player;
    [Tooltip("fillimage that shows the cooldown")]
    [SerializeField] private Image cooldownFill;
    [Tooltip("hide the fill when the sword is ready")]
    [SerializeField] private bool hideWhenReady = true;
    [Tooltip("the player sprote flases when the sword is ready")]
    [SerializeField] private SpriteRenderer playerSprite;
    [SerializeField] private Color readyFlashColor = new Color(1f, 0.95f, 0.7f);
    [SerializeField] private float flashTime = 0.12f;

    private bool wasReady = true;
    private float flash;

    // Update is called once per frame
   private void Update()
    {
        if (player == null) return;
        float ready = player.AttackReady01;
        bool isReady = ready >= 1f;

        if(cooldownFill != null)
        {
            cooldownFill.fillAmount = ready;
            cooldownFill.enabled = !(hideWhenReady && isReady);
        }

        if (isReady && !wasReady) flash = flashTime;
        wasReady = isReady;

        if(playerSprite != null && flash > 0f)
        {
            flash -= Time.deltaTime;
            playerSprite.color = flash > 0f ? readyFlashColor : Color.white;
        }
    }
}
