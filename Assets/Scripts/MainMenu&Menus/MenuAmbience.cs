using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MenuAmbience : MonoBehaviour
{
    [Header("Rune rings")]
    [SerializeField] private RectTransform ring;
    [SerializeField] private float ringSpeed = 4f; // degress per second
    [SerializeField] private RectTransform ringInner;
    [SerializeField] private float ringInnerSpeed = -7f;

    [Header("Title")]
    [SerializeField] private RectTransform title;
    [SerializeField] private float titleBreathe = 0.015f; // the size change
    [SerializeField] private Image titleGlow;
    [SerializeField] private float glowMin = 0.2f;
    [SerializeField] private float glowMax = 0.45f;
    [SerializeField] private float breatheSpeed = 0.6f;  //breathe per seconds

    [Header("Mouse parallax")]
    [Tooltip("Layers that drift with the mouse. Further-back layers should use smaller strengths.")]
    [SerializeField] private RectTransform[] parallaxLayers;
    [SerializeField] private float[] parallaxStrength = { 8f, 16f };
    [SerializeField] private float parallaxSmoothing = 4f;

    private Vector3 titleBaseScale = Vector3.one;
    private Vector2[] layerBase;
    private Vector2 smoothedMouse;


    private void Awake()
    {
        if (title != null) titleBaseScale = title.localScale;
        if(parallaxLayers != null)
        {
            layerBase = new Vector2[parallaxLayers.Length];
            for (int i = 0; i < parallaxLayers.Length; i++)
                if (parallaxLayers[i] != null) layerBase[i] = parallaxLayers[i].anchoredPosition;
        }
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    private void Update()
    {
        float t = Time.unscaledTime;
        float dt = Time.unscaledDeltaTime;

        if (ring != null) ring.Rotate(0f, 0f, ringSpeed * dt);
        if (ringInner != null) ringInner.Rotate(0f, 0f, ringInnerSpeed * dt);

        float breath = 0.5f + 0.5f * Mathf.Sin(t * breatheSpeed * 2f * Mathf.PI);
        if (title != null) title.localScale = titleBaseScale * (1f + titleBreathe * breath);
        if (titleGlow != null)
        {
            Color c = titleGlow.color;
            c.a = Mathf.Lerp(glowMin, glowMax, breath);
            titleGlow.color = c;
        }
        UpdateParallax(dt);
    }

    private void UpdateParallax(float dt)
    {
        if (parallaxLayers == null || layerBase == null || Mouse.current == null) return;

        Vector2 m = Mouse.current.position.ReadValue();
        Vector2 centered = new Vector2(Screen.width > 0 ? m.x / Screen.width - 0.5f : 0f, Screen.height > 0 ? m.y / Screen.height - 0.5f : 0f) *2f;
        smoothedMouse = Vector2.Lerp(smoothedMouse, centered, 1f - Mathf.Exp(-parallaxSmoothing * dt));

        for (int i = 0; i < parallaxLayers.Length; i++)
        {
            if (parallaxLayers[i] == null) continue;
            float s = (parallaxStrength != null && i < parallaxStrength.Length) ? parallaxStrength[i] : 10f;
            parallaxLayers[i].anchoredPosition = layerBase[i] - smoothedMouse * s;
        }
    }
}
