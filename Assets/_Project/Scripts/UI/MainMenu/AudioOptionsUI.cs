using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class AudioOptionsUI : MonoBehaviour
{
    private const int TOTAL_BLOCKS = 10;

    [Header("Sliders de Audio (Segmentados en 10 bloques: 0 a 10)")]
    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _musicSlider;
    [SerializeField] private Slider _environmentSlider;
    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private Slider _miscSlider;

    [Header("Textos de Porcentaje (Opcional)")]
    [SerializeField] private TMP_Text _masterPercentText;
    [SerializeField] private TMP_Text _musicPercentText;
    [SerializeField] private TMP_Text _environmentPercentText;
    [SerializeField] private TMP_Text _sfxPercentText;
    [SerializeField] private TMP_Text _miscPercentText;

    [Header("Sonido de Prueba al Cambiar de Bloque (Opcional)")]
    [SerializeField] private AudioClip _sfxPreviewClip;
    [SerializeField] private AudioClip _miscPreviewClip;
    [SerializeField] private float _previewCooldown = 0.08f;
    private float _lastPreviewTime;
    private void Awake()
    {
        ConfigureAllSliders();
    }
    private void OnEnable()
    {
        RefreshSliders();
        RegisterListeners();

        if (AudioManager.Instance != null)
            AudioManager.Instance.OnVolumesChanged += RefreshSliders;
    }
    private void OnDisable()
    {
        UnregisterListeners();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnVolumesChanged -= RefreshSliders;
            AudioManager.Instance.SaveAudioPreferences();
        }
    }
    /// <summary>
    /// Fuerza cada Slider a trabajar con números enteros de 0 a 10 (10 bloques de 10%).
    /// </summary>
    private void ConfigureAllSliders()
    {
        ConfigureSliderSteps(_masterSlider);
        ConfigureSliderSteps(_musicSlider);
        ConfigureSliderSteps(_environmentSlider);
        ConfigureSliderSteps(_sfxSlider);
        ConfigureSliderSteps(_miscSlider);
    }
    private static void ConfigureSliderSteps(Slider slider)
    {
        if (slider == null) return;
        slider.wholeNumbers = true;
        slider.minValue = 0f;
        slider.maxValue = TOTAL_BLOCKS;
    }
    private void RefreshSliders()
    {
        if (AudioManager.Instance == null) return;

        SetSliderVisual(_masterSlider, _masterPercentText, AudioManager.Instance.MasterVolume);
        SetSliderVisual(_musicSlider, _musicPercentText, AudioManager.Instance.MusicVolume);
        SetSliderVisual(_environmentSlider, _environmentPercentText, AudioManager.Instance.EnvironmentVolume);
        SetSliderVisual(_sfxSlider, _sfxPercentText, AudioManager.Instance.SfxVolume);
        SetSliderVisual(_miscSlider, _miscPercentText, AudioManager.Instance.MiscVolume);
    }
    private static void SetSliderVisual(Slider slider, TMP_Text label, float normalizedValue)
    {
        int blockStep = Mathf.Clamp(Mathf.RoundToInt(normalizedValue * TOTAL_BLOCKS), 0, TOTAL_BLOCKS);

        if (slider != null)
            slider.SetValueWithoutNotify(blockStep);

        if (label != null)
            label.text = $"{blockStep * 10}%";
    }
    private void RegisterListeners()
    {
        if (_masterSlider != null) _masterSlider.onValueChanged.AddListener(OnMasterChanged);
        if (_musicSlider != null) _musicSlider.onValueChanged.AddListener(OnMusicChanged);
        if (_environmentSlider != null) _environmentSlider.onValueChanged.AddListener(OnEnvironmentChanged);
        if (_sfxSlider != null) _sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        if (_miscSlider != null) _miscSlider.onValueChanged.AddListener(OnMiscChanged);
    }
    private void UnregisterListeners()
    {
        if (_masterSlider != null) _masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
        if (_musicSlider != null) _musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
        if (_environmentSlider != null) _environmentSlider.onValueChanged.RemoveListener(OnEnvironmentChanged);
        if (_sfxSlider != null) _sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
        if (_miscSlider != null) _miscSlider.onValueChanged.RemoveListener(OnMiscChanged);
    }
    private void OnMasterChanged(float stepValue)
    {
        if (AudioManager.Instance == null) return;
        float normalized = StepToNormalized(stepValue);
        AudioManager.Instance.SetMasterVolume(normalized);
        SetSliderVisual(null, _masterPercentText, normalized);
    }
    private void OnMusicChanged(float stepValue)
    {
        if (AudioManager.Instance == null) return;
        float normalized = StepToNormalized(stepValue);
        AudioManager.Instance.SetMusicVolume(normalized);
        SetSliderVisual(null, _musicPercentText, normalized);
    }
    private void OnEnvironmentChanged(float stepValue)
    {
        if (AudioManager.Instance == null) return;
        float normalized = StepToNormalized(stepValue);
        AudioManager.Instance.SetEnvironmentVolume(normalized);
        SetSliderVisual(null, _environmentPercentText, normalized);
    }
    private void OnSfxChanged(float stepValue)
    {
        if (AudioManager.Instance == null) return;
        float normalized = StepToNormalized(stepValue);
        AudioManager.Instance.SetSFXVolume(normalized);
        SetSliderVisual(null, _sfxPercentText, normalized);
        PlayPreview(_sfxPreviewClip, isMisc: false);
    }
    private void OnMiscChanged(float stepValue)
    {
        if (AudioManager.Instance == null) return;
        float normalized = StepToNormalized(stepValue);
        AudioManager.Instance.SetMiscVolume(normalized);
        SetSliderVisual(null, _miscPercentText, normalized);
        PlayPreview(_miscPreviewClip, isMisc: true);
    }
    private static float StepToNormalized(float stepValue)
    {
        int blockStep = Mathf.Clamp(Mathf.RoundToInt(stepValue), 0, TOTAL_BLOCKS);
        return blockStep / (float)TOTAL_BLOCKS;
    }
    private void PlayPreview(AudioClip clip, bool isMisc)
    {
        if (clip == null || AudioManager.Instance == null) return;
        if (Time.unscaledTime - _lastPreviewTime < _previewCooldown) return;

        _lastPreviewTime = Time.unscaledTime;
        if (isMisc) AudioManager.Instance.PlayUISound(clip);
        else AudioManager.Instance.PlayGlobalSFX(clip);
    }
}