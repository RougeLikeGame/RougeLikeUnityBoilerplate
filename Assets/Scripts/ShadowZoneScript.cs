using UnityEngine;

public class ShadowZoneScript : MonoBehaviour
{
    [SerializeField] PlayerStats player;
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] SpriteMask shadowMask;

    [SerializeField] float opacityMin = 0.7f;
    [SerializeField] float visibleMax = 0.5f;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(player.CurrentPerception.name == Perception.LevelName.Unseen) 
        {
            shadowMask.transform.localScale = new Vector3(0.2f, 0.2f, 1);
            sprite.color = new Color(0f, 0f, 0f, 1);

        }else if (player.CurrentPerception.name == Perception.LevelName.Visible) 
        {
            shadowMask.transform.localScale = new Vector3(visibleMax,visibleMax, 1);
            sprite.color = new Color(0f, 0f, 0f, opacityMin);

        }
        else 
        {
            shadowMask.transform.localScale = new Vector3(visibleMax,visibleMax, 1);
            sprite.color = new Color(0f, 0f, 0f, Mathf.Clamp(1 - player.CurrentPerception.value,opacityMin,1));
        }
    }
}
