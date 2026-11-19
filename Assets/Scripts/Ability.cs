using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using System.Linq;

public class Ability : MonoBehaviour
{
    public float manaCost;
    [SerializeField] InputField[] inputs;
    [SerializeField] UnityEngine.UI.Image cooldownBar;
    [SerializeField] UnityEngine.UI.Image[] uiImagesTotal;

    [SerializeField] GameObject InputSection;
    public virtual void Cast(PlayerAbilities player) 
    { 
    
    }

    public IEnumerator SkillCheck(float duration, PlayerAbilities player) 
    {
        float oldMod = player.move.speedMultiplier;
        player.move.speedMultiplier = 0;
        float elapsed = 0;
        bool completed = false;
        int inputIndex = 0;
        InputSection.SetActive(true);
        UnityEngine.UI.Image[] uiImages = new UnityEngine.UI.Image[inputs.Length];

        int j = 0;
        int startIndex = (uiImagesTotal.Length - inputs.Length) / 2;

        for (int i = startIndex; i < inputs.Length + startIndex; i++) 
        {
            uiImagesTotal[i].enabled = true;
            uiImagesTotal[i].sprite = inputs[j].image;
            uiImagesTotal[i].color = Color.white;
            uiImages[j] = uiImagesTotal[i];

            j++;
        }
        

        while(elapsed < duration && !completed)
       { 
        
        elapsed += Time.deltaTime;
        cooldownBar.fillAmount = 1 - (elapsed / duration);
            if (inputs[inputIndex].input.action.WasPressedThisFrame()) 
            {
                uiImages[inputIndex].color = Color.gold;
                inputIndex++;
                completed = inputIndex >= inputs.Length;
            }
        yield return null;
      }
        if (completed) 
        {
            Cast(player);
        }
        InputSection.SetActive(false);
        player.mana -= manaCost;
        player.move.speedMultiplier = oldMod;
    }
}
