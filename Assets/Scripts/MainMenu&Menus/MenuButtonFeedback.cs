using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public class MenuButtonFeedback : MonoBehaviour,
    IPointerEnterHandler, ISelectHandler, IDeselectHandler,
    IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, ISubmitHandler
{
    public enum ClickSound { Click, Back, Start, Open, None }

    [Header("Parts (all optional)")]
    [Tooltip("What grows and shrinks")]
    [SerializeField] private RectTransform scaleTarget;
    [SerializeField] private TMP_Text label;
    [Tooltip("The button's frame image (gets a warmer tint when highlighted).")]
    [SerializeField] private Image frame;
    [Tooltip("Soft glow image behind the button.")]
    [SerializeField] private Image glow;
    [SerializeField] private Image leftMarker;
    [SerializeField] private Image rightMarker;

    [Header("Feel")]
    [SerializeField] private float hoverScale = 1.06f;
    [SerializeField] private float pressScale = 0.94f;
    [Tooltip("Higher = snappier.")]
    [SerializeField] private float animSpeed = 14f;
    [Tooltip("Extra space between letters when highlighted.")]
    [SerializeField] private float letterSpreadOnHover = 6f;
    [Tooltip("How far the markers slide in from.")]
    [SerializeField] private float markerSlide = 24f;
    [SerializeField] private float markerBob = 3f;

    [Header("Colors")]
    [SerializeField] private Color labelNormal = new Color(0.78f, 0.75f, 0.67f);
    [SerializeField] private Color labelHighlight = new Color(1f, 0.84f, 0.51f);
    [SerializeField] private Color labelDisabled = new Color(0.4f, 0.4f, 0.42f);
    [SerializeField] private Color frameNormal = new Color(0.85f, 0.85f, 0.85f);
    [SerializeField] private Color frameHighlight = Color.white;
    [SerializeField] private Color glowColor = new Color(0.91f, 0.71f, 0.35f);
    [SerializeField, Range(0f, 1f)] private float glowAlpha = 0.55f;

    [Header("Sound")]
    [SerializeField] private MenuAudio menuAudio;
    [SerializeField] private ClickSound clickSound = ClickSound.Click;
    [SerializeField] private bool hoverSound = true;

    //Lets menus select a button without the hover tick
    private static float suppressHoverUntil;
    public static void SuppressHoverSound() => suppressHoverUntil = Time.unscaledTime + 0.1f;

    private Selectable selectable;
    private RectTransform target;
    private Vector3 baseScale;
    private float baseSpacing;
    private Vector2 leftBase, rightBase;

    private bool selected;
    private bool pressed;
    private float highlight;   //0..1 smoothed
    private float flash;       //1 on click, fades to 0
    private float bounce;      //1 on click, fades to 0

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
        target = scaleTarget != null ? scaleTarget : (RectTransform)transform;
        baseScale = target.localScale;
        if (label != null) baseSpacing = label.characterSpacing;
        if (leftMarker != null) leftBase = leftMarker.rectTransform.anchoredPosition;
        if (rightMarker != null) rightBase = rightMarker.rectTransform.anchoredPosition;
        Apply(0f);
    }

    private void OnDisable()
    {
        //Reset so a button doesn't come back stuck "highlighted" when a panel reopens.
        selected = false;
        pressed = false;
        highlight = 0f;
        flash = 0f;
        bounce = 0f;
        Apply(0f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!selectable.IsInteractable() || EventSystem.current == null) return;
        if (EventSystem.current.currentSelectedGameObject != gameObject)
            EventSystem.current.SetSelectedGameObject(gameObject); //triggers OnSelect
    }

    public void OnSelect(BaseEventData eventData)
    {
        selected = true;
        if (hoverSound && menuAudio != null && Time.unscaledTime > suppressHoverUntil && selectable.IsInteractable())
            menuAudio.PlayHover();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        selected = false;
        pressed = false;
    }

    public void OnPointerDown(PointerEventData eventData) => pressed = selectable.IsInteractable();
    public void OnPointerUp(PointerEventData eventData) => pressed = false;

    public void OnPointerClick(PointerEventData eventData) => Clicked();
    public void OnSubmit(BaseEventData eventData) => Clicked();

    private void Clicked()
    {
        if (!selectable.IsInteractable()) return;
        flash = 1f;
        bounce = 1f;
        if (menuAudio == null) return;
        switch (clickSound)
        {
            case ClickSound.Click: menuAudio.PlayClick(); break;
            case ClickSound.Back: menuAudio.PlayBack(); break;
            case ClickSound.Start: menuAudio.PlayStart(); break;
            case ClickSound.Open: menuAudio.PlayOpen(); break;
        }
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        bool on = selected && selectable.IsInteractable();
        highlight = Mathf.Lerp(highlight, on ? 1f : 0f, 1f - Mathf.Exp(-animSpeed * dt));
        flash = Mathf.MoveTowards(flash, 0f, dt * 3f);
        bounce = Mathf.MoveTowards(bounce, 0f, dt * 5f);
        Apply(highlight);
    }

    private void Apply(float h)
    {
        bool interactable = selectable == null || selectable.IsInteractable();

        //Size: grow on hover, squish while held, bounce after a click.
        float s = Mathf.Lerp(1f, hoverScale, h);
        if (pressed) s = pressScale;
        s *= 1f + 0.06f * Mathf.Sin(bounce * Mathf.PI);
        if (target != null) target.localScale = baseScale * s;

        if (label != null)
        {
            Color c = interactable ? Color.Lerp(labelNormal, labelHighlight, h) : labelDisabled;
            label.color = Color.Lerp(c, Color.white, flash * 0.7f);
            label.characterSpacing = baseSpacing + letterSpreadOnHover * h;
        }

        if (frame != null)
        {
            Color fc = Color.Lerp(frameNormal, frameHighlight, h);
            if (!interactable) fc *= 0.6f;
            fc.a = 1f;
            frame.color = fc;
        }

        if (glow != null)
        {
            Color g = glowColor;
            g.a = Mathf.Max(glowAlpha * h, flash);
            glow.color = g;
        }

        float bob = Mathf.Sin(Time.unscaledTime * 4f) * markerBob * h;
        SetMarker(leftMarker, leftBase + Vector2.left * (markerSlide * (1f - h) + bob), h);
        SetMarker(rightMarker, rightBase + Vector2.right * (markerSlide * (1f - h) + bob), h);
    }

    private static void SetMarker(Image marker, Vector2 pos, float alpha)
    {
        if (marker == null) return;
        marker.rectTransform.anchoredPosition = pos;
        Color c = marker.color;
        c.a = alpha;
        marker.color = c;
    }
}
