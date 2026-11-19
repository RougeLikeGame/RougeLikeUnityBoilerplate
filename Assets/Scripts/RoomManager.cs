using UnityEngine;

public class RoomManager : MonoBehaviour
{
   [SerializeField] Door NorthDoor;
   [SerializeField] Door SouthDoor;
   [SerializeField] Door EastDoor;
   [SerializeField] Door WestDoor;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        unlockDoors();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void unlockDoors()
    {
        NorthDoor.OpenDoor();
        SouthDoor.OpenDoor();
        EastDoor.OpenDoor();
        WestDoor.OpenDoor();
    }
}
