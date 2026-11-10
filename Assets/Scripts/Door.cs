using Unity.Jobs;
using UnityEngine;

public class Door : MonoBehaviour
{
    enum DoorSide
    {
        North,
        South,
        East,
        West
    };
    [SerializeField] DoorSide doorSide;

    Vector3 northTransform = new Vector3(0, 2f,0);
    Vector3 southTransform = new Vector3(0, -2f, 0);
    Vector3 eastTransform = new Vector3(2f, 0, 0);
    Vector3 westTransform = new Vector3(-2f, 0, 0);

    [SerializeField] BoxCollider2D doorCollider;
    [SerializeField] Camera mainCamera;
    [SerializeField] GameObject room;
    [SerializeField] SpriteRenderer doorSprite;

    private void Start()
    {
        if(doorCollider == null)
        {
            doorCollider = GetComponent<BoxCollider2D>();
        }

        if(doorSprite == null)
        {
            doorSprite = GetComponent<SpriteRenderer>();
        }

        if(room == null) 
        { 
        gameObject.SetActive(false);

        }
    }

    public void OpenDoor()
    {
        doorCollider.enabled = true;
        doorSprite.color = new Color(0, 0, 0 , 1);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Player"))
        {
            room.gameObject.SetActive(true);
            mainCamera.transform.position = room.transform.position + new Vector3(0, 0, mainCamera.transform.position.z);
            switch(doorSide)
            {
                case DoorSide.North:
                    collision.transform.position += northTransform;
                    break;
                case DoorSide.South:
                    collision.gameObject.transform.position += southTransform;
                    break;
                case DoorSide.East:
                    collision.gameObject.transform.position += eastTransform;
                    break;
                case DoorSide.West:
                    collision.gameObject.transform.position += westTransform;
                    break;
            }
            transform.parent.gameObject.SetActive(false);
        }
    }


}
