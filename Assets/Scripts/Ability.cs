using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class Ability : MonoBehaviour
{
    public float manaCost;
    [SerializeField] InputField[] inputs;
    [SerializeField] Image cooldownBar;
    public virtual void Cast(PlayerAbilities player) 
    { 
    
    }

    public IEnumerator skillCheck(float duration, PlayerAbilities player) 
    {
        float elapsed = 0;
        bool completed = false;
        int inputIndex = 0;

        while(elapsed < duration && !completed)
       { 
        cooldownBar.fillAmount = 1 - (elapsed / duration);
        elapsed += Time.deltaTime;
            if (inputs[inputIndex].input.action.WasPressedThisFrame()) 
            {
                inputIndex++;
                completed = inputIndex >= inputs.Length;
            }
        yield return null;
      }
        if (completed) 
        {
            Cast(player);
        }
    }
}
