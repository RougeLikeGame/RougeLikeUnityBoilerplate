using UnityEngine;
using UnityEngine.InputSystem;

// saves and loads the player's custom keys.
// put one on an object in every scene (main menu and game), with your playercontrols asset dragged in.
// it loads the saved keys when the scene starts. the reset button in settings calls resetall().

public class KeybindManager : MonoBehaviour
{
    private const string SaveKey = "ascendant_keybinds";
    [SerializeField] private InputActionAsset inputActions;
    public static event System.Action OnBindingsChanged;

    private void Awake()
    {
        if(inputActions == null)
        {
            Debug.LogError("KeybindManager drag int he paleyr controls asset");
            return;
        }
        Load(inputActions);
    }

    //hook this to a rest to default button
    public void ResetAll()
    {
        if (inputActions == null) return;
        inputActions.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(SaveKey);
        OnBindingsChanged?.Invoke();
    }

    public static void Load(InputActionAsset asset)
    {
        string jason = PlayerPrefs.GetString(SaveKey, "");
        if (!string.IsNullOrEmpty(jason)) asset.LoadBindingOverridesFromJson(jason);
    }

    public static void Save(InputActionAsset asset)
    {
        PlayerPrefs.SetString(SaveKey, asset.SaveBindingOverridesAsJson());
        PlayerPrefs.Save();
        OnBindingsChanged?.Invoke();
    }

    //the keyboard key for an action like e or spacce
    public static string KeyName(InputAction action)
    {
        if (action == null) return "?";
        for(int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding b = action.bindings[i];
            if (b.isComposite || b.isPartOfComposite) continue;
            if (IsKeyboard(b)) return action.GetBindingDisplayString(i);
        }
        return action.GetBindingDisplayString();
    }

    //the four move kes like wasd
    public static string MoveKeys(InputAction move)
    {
        if (move == null) return "?";
        string up = "", left = "", down = "", right = "";
        for(int i = 0; i<move.bindings.Count; i++)
        {
            InputBinding b = move.bindings[i];
            if (!b.isPartOfComposite || !IsKeyboard(b)) continue;
            string k = move.GetBindingDisplayString(i);
            switch (b.name.ToLower())
            {
                case "up": if (up == "") up = k; break;
                case "left": if (left == "") left = k; break;
                case "down": if (down == "") down = k; break;
                case "right": if (right == "") right = k; break;
            }
        }
        if (up == "") return move.GetBindingDisplayString();
        return $"{up} {left} {down} {right}";
    }

    public static bool IsKeyboard(InputBinding b) => b.effectivePath != null && b.effectivePath.StartsWith("<Keyboard>");

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
