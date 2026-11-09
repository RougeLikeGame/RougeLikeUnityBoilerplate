using UnityEngine;
using UnityEngine.UI;

public class FloatingEmbers : MonoBehaviour
{
    [SerializeField] private Image template;
    [SerializeField] private RectTransform area;
    [SerializeField] private int count = 30;
    [SerializeField] private Vector2 riseSpeed = new Vector2(12f, 45f); //the min and max pixels per seocned
    [SerializeField] private Vector2 size = new Vector2(10f, 26f);
    [SerializeField] private float sway = 18f;
    [SerializeField] private Color colorA = new Color(1f, 0.54f, 0.24f);
    [SerializeField] private Color colorB = new Color(1f, 0.8f, 0.45f);

    private RectTransform[] embers;
    private Image[] images;
    private float[] speed, phase, baseX, maxAlpha;



     // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        if (template == null || area == null)
        {
            Debug.LogError("floating Embers drage in the template image and hte area in this");
            enabled = false;
            return;
        }

        template.gameObject.SetActive(false);
        embers = new RectTransform[count];
        images = new Image[count];
        speed = new float[count];
        phase = new float[count];
        baseX = new float[count];
        maxAlpha = new float[count];

        for (int i = 0; i < count; i++)
        {
            Image img = Instantiate(template, area);
            img.gameObject.SetActive(true);
            img.raycastTarget = false;
            images[i] = img;
            embers[i] = img.rectTransform;
            Respawn(i, randomHeight: true);
        }
    }

    // Update is called once per frame
   private void Update()
    {
        if (embers == null) return;
        float dt = Time.unscaledDeltaTime;
        float t = Time.unscaledTime;
        Rect r = area.rect;

        for (int i = 0; i < embers.Length; i++)
        {
            Vector2 p = embers[i].anchoredPosition;
            p.y += speed[i] * dt;
            p.x = baseX[i] + Mathf.Sin(t * 0.8f + phase[i]) * sway;
            embers[i].anchoredPosition = p;

            //for the fade in at the bottom, and the fade out near the top and the flicker
            float h = Mathf.InverseLerp(r.yMin, r.yMax, p.y);
            float fade = Mathf.Clamp01(h * 5f) * Mathf.Clamp01((1f - h) * 3f);
            float flicker = 0.75f + 0.25f * Mathf.Sin(t * 9f + phase[i] * 3f);
            Color c = images[i].color;
            c.a = maxAlpha[i] * fade * flicker;
            images[i].color = c;

            if (p.y > r.yMax) Respawn(i, randomHeight: false);
        }
    }

    private void Respawn(int i, bool randomHeight)
    {
        Rect r = area.rect;
        speed[i] = Random.Range(riseSpeed.x, riseSpeed.y);
        phase[i] = Random.Range(0f, 10f);
        baseX[i] = Random.Range(r.xMin, r.xMax);
        maxAlpha[i] = Random.Range(0.35f, 0.9f);
        float s = Random.Range(size.x, size.y);
        embers[i].sizeDelta = new Vector2(s, s);
        embers[i].anchoredPosition = new Vector2(baseX[i], randomHeight ? Random.Range(r.yMin, r.yMax) : r.yMin);
        Color c = Color.Lerp(colorA, colorB, Random.value);
        c.a = 0f;
        images[i].color = c;
    }
}
