using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
public class PlayerAbilities : MonoBehaviour
{
    [SerializeField] Ability[] abilities;
    float mana = 100f;
    float maxMana = 100f;
    float soulFrequency = 10f;
    [SerializeField] InputActionAsset inputActions;
    InputAction ability1Action, ability2Action;
    public PlayerStats stats;
    [SerializeField] Image manaBar;

    void Start()
    {
        ability1Action = inputActions.FindAction("Ability 1");
        ability2Action = inputActions.FindAction("Ability 2");
        inputActions.Enable();
        stats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        mana += soulFrequency * Time.deltaTime;
        mana = Mathf.Clamp(mana, 0, maxMana);

        if(ability1Action.triggered && abilities[0]!= null)
        {
            if (mana >= abilities[0].manaCost)
            {
                abilities[0].Cast(this);
                mana -= abilities[0].manaCost;
            }
        }
        if(ability2Action.triggered && abilities[1]!= null)
        {
            if (mana >= abilities[1].manaCost)
            {
                abilities[1].Cast(this);
                mana -= abilities[1].manaCost;
            }
        }
        manaBar.fillAmount = mana / maxMana;
    }
}
