using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "InputField", menuName = "ScriptableObjects/InputField", order = 1)]
public class InputField : ScriptableObject
{
    [SerializeField] InputActionAsset inputActions;
    public InputActionReference input;
    public Sprite image;
}
