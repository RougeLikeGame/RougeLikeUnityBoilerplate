
//using NUnit.Framework;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MenuAudio : MonoBehaviour
{
    [Header("Clips")]
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip backClip;
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip startClip;

    [Header("Mix")]
    [SerializeField, Range(0f, 1f)] private float volume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float hoverVolume = 0.5f;
    [SerializeField, Range(0f, 0.2f)] private float pitchVariation = 0.04f;
    [SerializeField] private float minTimeBetweenHovers = 0.05f;

    private AudioSource source;
    private float lastHoverTime = -1f;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.ignoreListenerPause = true; //still plays when the game is paused

    }

    public void PlayHover()
    {
        if (Time.unscaledTime - lastHoverTime < minTimeBetweenHovers) return;
        lastHoverTime = Time.unscaledTime;
        Play(hoverClip, hoverVolume);
    }

    public void PlayClick() => Play(clickClip, 1f);
    public void PlayBack() => Play(backClip, 1f);
    public void PlayOpen() => Play(openClip, 1f);
    public void PlayStart() => Play(startClip, 1f);

    public void KeepAliveThroughLoad(float seconds)
    {
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        Destroy(gameObject, seconds);
    }

    private void Play(AudioClip clip, float volumeScale)
    {
        if (clip == null || source == null) return;
        source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        source.PlayOneShot(clip, volume * volumeScale);
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
