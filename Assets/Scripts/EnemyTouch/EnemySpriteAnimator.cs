using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpriteAnimator : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        public string key;
        public Sprite[] frames;
        public float fps = 10f;
        public bool loop = true;
        [Tooltip("Play the whole clip over the state's duration (ignores FPS). For wind-ups, emerge, burrow.")]
        public bool stretchToState;
        public bool HasFrames => frames != null && frames.Length > 0;
    }

    [SerializeField] private EnemyBase enemy;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("Tick if the art faces RIGHT in the sprite sheet.")]
    [SerializeField] private bool artFacesRight = true;
    [SerializeField] private List<Entry> animations = new List<Entry>();

    private string currentKey;
    private Entry current;
    private float clipTime;

    private void Awake()
    {
        if (enemy == null || spriteRenderer == null)
        {
            Debug.LogError("EnemySpriteAnimator: drag in the Enemy and its Sprite Renderer.", this);
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        string key = enemy.AnimKey;
        if (key != currentKey)
        {
            currentKey = key;
            current = Find(key);
            clipTime = 0f;
        }

        clipTime += Time.deltaTime;

        if (current != null && current.HasFrames)
        {
            int count = current.frames.Length;
            int i;
            if (current.stretchToState)
                i = Mathf.Min(count - 1, Mathf.FloorToInt(enemy.StateProgress01 * count));
            else
            {
                i = Mathf.FloorToInt(clipTime * Mathf.Max(0.01f, current.fps));
                i = current.loop ? i % count : Mathf.Min(i, count - 1);
            }
            spriteRenderer.sprite = current.frames[i];
        }

        //Facing
        float x = enemy.Facing.x;
        if (Mathf.Abs(x) > 0.01f)
            spriteRenderer.flipX = artFacesRight ? x < 0f : x > 0f;
    }

    private Entry Find(string key)
    {
        foreach (Entry e in animations)
            if (e.HasFrames && string.Equals(e.key, key, StringComparison.OrdinalIgnoreCase))
                return e;

        //Fallback: first entry with frames, so the enemy never goes blank.
        foreach (Entry e in animations)
            if (e.HasFrames) return e;

        return null;
    }

    [ContextMenu("Fill Animation Names From Enemy")]
    private void FillAnimationNames()
    {
        if (enemy == null)
        {
            Debug.LogWarning("Drag the enemy into the Enemy slot first.", this);
            return;
        }

        foreach (string key in enemy.AnimationKeys)
        {
            bool exists = false;
            foreach (Entry e in animations)
                if (string.Equals(e.key, key, StringComparison.OrdinalIgnoreCase)) exists = true;
            if (!exists) animations.Add(new Entry { key = key });
        }
    }
}
