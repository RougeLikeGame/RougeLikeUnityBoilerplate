using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    private const string VolumeKey = "ascendant.volume";
    private const string FullscreenKey = "ascendant.fullscreen";
    private const float DefaultVolume = 0.8f;

    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Text volumeValueText;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private MenuAudio menuAudio;

    private void Awake()
    {
        if(volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
    }

    private void OnEnable()
    {
        //show the same values with out triggering thge change events
        float v = PlayerPrefs.GetFloat(VolumeKey, DefaultVolume);
        if (volumeSlider != null) volumeSlider.SetValueWithoutNotify(v);
        UpdateVolumeText(v);
        if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
    }

    public static void ApplySaved()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, DefaultVolume);
    }

    private void OnVolumeChanged(float v)
    {
        AudioListener.volume = v;
        PlayerPrefs.SetFloat(VolumeKey, v);
        UpdateVolumeText(v);
        if (menuAudio != null) menuAudio.PlayHover();
    }

    private void OnFullscreenChanged(bool on)
    {
        Screen.fullScreen = on;
        PlayerPrefs.SetInt(FullscreenKey, on ? 1 : 0);
        if (menuAudio != null) menuAudio.PlayClick();

    }

    private void UpdateVolumeText(float v)
    {
        if (volumeValueText != null) volumeValueText.text = $"{Mathf.RoundToInt(v * 100f)}%";
    }

    private void OnDisable() => PlayerPrefs.Save();
}
