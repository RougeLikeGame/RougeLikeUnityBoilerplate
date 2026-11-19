using UnityEngine;

public class GameManager : MonoBehaviour
{
    private float stateForRoom = 0.0f;
    [SerializeField] PlayerStats player;

    public enum StateSource 
    { 
        Player = -5,
        Trap = 0,
        Parry = 1,
        HealingWave = 6,
        Assist = 4
    }


    public void ChangeState(StateSource src) 
    {
        stateForRoom += ((int)src);
    }

    public void FinishRoom() 
    {
        player.ChangeStateMeter(stateForRoom);
        stateForRoom = 0;
    }
}
