using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

// one row in the controls menu: "attack   [f]". click it, press a new key, done.
// press esc to cancel. the new key is saved right away.

public class RebindButton : MonoBehaviour
{
    public static bool IsRebinding { get; private set; }
    public static int LastRebindFrame { get; private set; } = -10;

    [SerializeField] private InputActionAsset inputActions;
    [Tooltip("e.g. Player/Attack, Player/Interact, Player/Sprint, Player/Move")]
    [SerializeField] private string actionName = "Player/Attack";
    [Tooltip("For Move only: up, down, left or right. Leave empty for everything else.")]
    [SerializeField] private string compositePart = "";
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text keyText;
    [Tooltip("What the row is called. Leave empty to use the action's name.")]
    [SerializeField] private string displayName = "";
    [SerializeField] private string waitingText = "press a key...";
    [SerializeField] private MenuAudio menuAudio;

    private InputAction action;
    private int bindingIndex = -1;
    private InputActionRebindingExtensions.RebindingOperation operation;

    private void Awake()
    {
        if (inputActions == null || button == null || keyText == null)
        {
            Debug.LogError("RebindButton: drag in Input Actions, Button and Key Text.", this);
            return;
        }
        action = inputActions.FindAction(actionName, throwIfNotFound: false);
        if (action == null) { Debug.LogError($"RebindButton: no action called {actionName}.", this); return; }

        bindingIndex = FindKeyboardBinding();
        if (bindingIndex < 0) Debug.LogError($"RebindButton: {actionName} {compositePart} has no keyboard binding.", this);

        button.onClick.AddListener(StartRebind);
        if (nameText != null)
            nameText.text = !string.IsNullOrEmpty(displayName) ? displayName
                : string.IsNullOrEmpty(compositePart) ? action.name.ToLower() : $"move {compositePart}";
    }

    private void OnEnable()
    {
        KeybindManager.OnBindingsChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        KeybindManager.OnBindingsChanged -= Refresh;
        if (operation != null) Finish(false);
    }

    private int FindKeyboardBinding()
    {
        for(int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding b = action.bindings[i];
            if (!KeybindManager.IsKeyboard(b)) continue;
            if(string.IsNullOrEmpty(compositePart))
            {
                if (!b.isComposite && !b.isPartOfComposite) return i;

            }
            else if (b.isPartOfComposite && b.name.ToLower() == compositePart.ToLower())
            {
                return i;
            }
        }
        return -1;
    }

    public void Refresh()
    {
        if (action == null || bindingIndex < 0 || keyText == null) return;
        keyText.text = action.GetBindingDisplayString(bindingIndex);
    }

    private void StartRebind()
    {
        if (action == null || bindingIndex < 0 || IsRebinding) return;

        IsRebinding = true;
        keyText.text = waitingText;
        if (menuAudio != null) menuAudio.PlayClick();

        //the action has to be off while it listens for a new kwy
        bool wasEnabled = action.enabled;
        action.Disable();

        operation = action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsHavingToMatchPath("<Keyboard>")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(op => { Finish(true); if (wasEnabled) action.Enable(); })
            .OnCancel(op => { Finish(false); if (wasEnabled) action.Enable(); })
            .Start();

    }


    private void Finish(bool saved)
    {
        operation?.Dispose();
        operation = null;
        IsRebinding = false;
        LastRebindFrame = Time.frameCount;
        if (saved) KeybindManager.Save(inputActions); //this also resers every row
        else Refresh();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
