using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// a tutorial sign. it fades in when the player walks into its area and out when they leave.
// write keys as {move} {sprint} {attack} {interact} and they become the player's real keys
// (so it's right after rebinding, and on other keyboard layouts).
public class TutorialHint : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private InputActionAsset inputActions;
    [TextArea(2, 4)]
    [SerializeField] private string message = "{attack}  swing your sword";
    [Tooltip("Optional: hide this hint for good once the player has done this action (e.g. Player/Attack).")]
    [SerializeField] private string hideAfterAction = "";
    [SerializeField] private float fadeSpeed = 4f;

    private bool inside;
    private bool done;
    private float alpha;
    private InputAction doneAction;

    private void Awake()
    {
        if (text == null) Debug.LogError("TutorialHint: drag in the Text.", this);
        if (inputActions != null && !string.IsNullOrEmpty(hideAfterAction))
            doneAction = inputActions.FindAction(hideAfterAction, throwIfNotFound: false);
        SetAlpha(0f);
    }

    private void OnEnable()
    {
        // keys can change in settings, so fill them in each time
        if (text != null) text.text = Fill(message);
    }

    private void Update()
    {
        if (inside && doneAction != null && doneAction.WasPressedThisFrame()) done = true;
        float goal = inside && !done ? 1f : 0f;
        alpha = Mathf.MoveTowards(alpha, goal, fadeSpeed * Time.unscaledDeltaTime);
        SetAlpha(alpha);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerStats>() == null) return;
        inside = true;
        if (text != null) text.text = Fill(message);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerStats>() != null) inside = false;
    }

    // swaps {attack} etc. for the key names
    private string Fill(string s)
    {
        if (inputActions == null) return s;
        foreach (string name in new[] { "Move", "Sprint", "Attack", "Interact" })
        {
            // works with {attack} or {Attack}
            string tag = "{" + name + "}";
            if (s.IndexOf(tag, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
            InputAction a = inputActions.FindAction("Player/" + name, throwIfNotFound: false);
            string key = name == "Move" ? KeybindManager.MoveKeys(a) : KeybindManager.KeyName(a);
            s = System.Text.RegularExpressions.Regex.Replace(s, System.Text.RegularExpressions.Regex.Escape(tag),
                "[" + key + "]", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        return s;
    }

    private void SetAlpha(float a)
    {
        if (text == null) return;
        Color c = text.color;
        c.a = a;
        text.color = c;
    }
}
