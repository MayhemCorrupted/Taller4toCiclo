using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private const string MASTER_PARAM = "MasterVolume";
    private const string MUSIC_PARAM = "MusicVolume";
    private const string ENVIRONMENT_PARAM = "EnvironmentVolume";
    private const string SFX_PARAM = "SFXVolume";
    private const string MISC_PARAM = "MiscVolume";

    private const float DEFAULT_VOLUME = 1.0f;
    private const float MIN_LINEAR_VOLUME = 0.0001f;

    public event Action OnVolumesChanged;

    [Header("Audio Mixer & Buses (Master + 4 Sub-canales = 5 Categorías)")]
    [SerializeField] private AudioMixer _mainMixer;
    [SerializeField] private AudioMixerGroup _musicGroup;
    [SerializeField] private AudioMixerGroup _environmentGroup;
    [SerializeField] private AudioMixerGroup _sfxGroup;
    [SerializeField] private AudioMixerGroup _miscGroup;

    [Header("Audio Sources Globales (2D)")]
    [SerializeField] private AudioSource _musicSourceA;
    [SerializeField] private AudioSource _musicSourceB;
    [SerializeField] private AudioSource _environmentSource;
    [SerializeField] private AudioSource _globalSfxSource;
    [SerializeField] private AudioSource _uiSource;
    [SerializeField] private AudioSource _voiceSource;

    [Header("Transición de Música")]
    [SerializeField] private float _defaultCrossfadeDuration = 1.5f;

    private AudioSource _activeMusicSource;
    private Coroutine _crossfadeCoroutine;
    public float MasterVolume { get; private set; } = DEFAULT_VOLUME;
    public float MusicVolume { get; private set; } = DEFAULT_VOLUME;
    public float EnvironmentVolume { get; private set; } = DEFAULT_VOLUME;
    public float SfxVolume { get; private set; } = DEFAULT_VOLUME;
    public float MiscVolume { get; private set; } = DEFAULT_VOLUME;

    // Propiedades públicas para rutear AudioSources 3D de enemigos, entorno o jugador
    public AudioMixerGroup MusicMixerGroup => _musicGroup;
    public AudioMixerGroup EnvironmentMixerGroup => _environmentGroup;
    public AudioMixerGroup SfxMixerGroup => _sfxGroup;
    public AudioMixerGroup MiscMixerGroup => _miscGroup;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ConfigureInternalSources();
        LoadSavedVolumes();
    }
    private void Start()
    {
        ApplyAllVolumesToMixer();
    }
    private void ConfigureInternalSources()
    {
        SetupSource(_musicSourceA, _musicGroup, loop: true);
        SetupSource(_musicSourceB, _musicGroup, loop: true);
        SetupSource(_environmentSource, _environmentGroup, loop: true);
        SetupSource(_globalSfxSource, _sfxGroup, loop: false);

        SetupSource(_uiSource, _miscGroup, loop: false);
        if (_uiSource != null)
            _uiSource.ignoreListenerPause = true;

        SetupSource(_voiceSource, _miscGroup, loop: false);

        _activeMusicSource = _musicSourceA;
    }
    private static void SetupSource(AudioSource source, AudioMixerGroup group, bool loop)
    {
        if (source == null) return;
        source.outputAudioMixerGroup = group;
        source.loop = loop;
        source.playOnAwake = false;
    }
    public void SetMasterVolume(float normalizedValue)
    {
        MasterVolume = Mathf.Clamp(normalizedValue, MIN_LINEAR_VOLUME, 1f);
        SetMixerDecibels(MASTER_PARAM, MasterVolume);
        PlayerPrefs.SetFloat(MASTER_PARAM, MasterVolume);
        OnVolumesChanged?.Invoke();
    }
    public void SetMusicVolume(float normalizedValue)
    {
        MusicVolume = Mathf.Clamp(normalizedValue, MIN_LINEAR_VOLUME, 1f);
        SetMixerDecibels(MUSIC_PARAM, MusicVolume);
        PlayerPrefs.SetFloat(MUSIC_PARAM, MusicVolume);
        OnVolumesChanged?.Invoke();
    }
    public void SetEnvironmentVolume(float normalizedValue)
    {
        EnvironmentVolume = Mathf.Clamp(normalizedValue, MIN_LINEAR_VOLUME, 1f);
        SetMixerDecibels(ENVIRONMENT_PARAM, EnvironmentVolume);
        PlayerPrefs.SetFloat(ENVIRONMENT_PARAM, EnvironmentVolume);
        OnVolumesChanged?.Invoke();
    }
    public void SetSFXVolume(float normalizedValue)
    {
        SfxVolume = Mathf.Clamp(normalizedValue, MIN_LINEAR_VOLUME, 1f);
        SetMixerDecibels(SFX_PARAM, SfxVolume);
        PlayerPrefs.SetFloat(SFX_PARAM, SfxVolume);
        OnVolumesChanged?.Invoke();
    }
    public void SetMiscVolume(float normalizedValue)
    {
        MiscVolume = Mathf.Clamp(normalizedValue, MIN_LINEAR_VOLUME, 1f);
        SetMixerDecibels(MISC_PARAM, MiscVolume);
        PlayerPrefs.SetFloat(MISC_PARAM, MiscVolume);
        OnVolumesChanged?.Invoke();
    }
    public void SaveAudioPreferences()
    {
        PlayerPrefs.Save();
        GameLogger.LogInfo("Preferencias de audio (5 categorías) guardadas en disco.");
    }
    private void LoadSavedVolumes()
    {
        MasterVolume = PlayerPrefs.GetFloat(MASTER_PARAM, DEFAULT_VOLUME);
        MusicVolume = PlayerPrefs.GetFloat(MUSIC_PARAM, DEFAULT_VOLUME);
        EnvironmentVolume = PlayerPrefs.GetFloat(ENVIRONMENT_PARAM, DEFAULT_VOLUME);
        SfxVolume = PlayerPrefs.GetFloat(SFX_PARAM, DEFAULT_VOLUME);
        MiscVolume = PlayerPrefs.GetFloat(MISC_PARAM, DEFAULT_VOLUME);
    }
    private void ApplyAllVolumesToMixer()
    {
        SetMixerDecibels(MASTER_PARAM, MasterVolume);
        SetMixerDecibels(MUSIC_PARAM, MusicVolume);
        SetMixerDecibels(ENVIRONMENT_PARAM, EnvironmentVolume);
        SetMixerDecibels(SFX_PARAM, SfxVolume);
        SetMixerDecibels(MISC_PARAM, MiscVolume);
    }
    private void SetMixerDecibels(string parameterName, float normalizedValue)
    {
        if (_mainMixer == null) return;
        float decibels = Mathf.Log10(Mathf.Clamp(normalizedValue, MIN_LINEAR_VOLUME, 1f)) * 20f;
        _mainMixer.SetFloat(parameterName, decibels);
    }
    public void PlayMusic(AudioClip newClip, float fadeDuration = -1f)
    {
        if (_musicSourceA == null || _musicSourceB == null) return;
        if (_activeMusicSource != null && _activeMusicSource.clip == newClip && _activeMusicSource.isPlaying)
            return;

        float duration = fadeDuration < 0f ? _defaultCrossfadeDuration : fadeDuration;
        if (_crossfadeCoroutine != null)
            StopCoroutine(_crossfadeCoroutine);

        _crossfadeCoroutine = StartCoroutine(CrossfadeMusicCoroutine(newClip, duration));
    }
    public void PlayEnvironment(AudioClip environmentClip)
    {
        if (_environmentSource == null) return;
        if (environmentClip == null)
        {
            _environmentSource.Stop();
            _environmentSource.clip = null;
            return;
        }

        if (_environmentSource.clip == environmentClip && _environmentSource.isPlaying) return;
        _environmentSource.clip = environmentClip;
        _environmentSource.Play();
    }
    public void PlayGlobalSFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || _globalSfxSource == null) return;
        _globalSfxSource.PlayOneShot(clip, volumeScale);
    }
    public void PlayUISound(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || _uiSource == null) return;
        _uiSource.PlayOneShot(clip, volumeScale);
    }
    public void PlayVoice(AudioClip voiceClip, float volumeScale = 1f)
    {
        if (voiceClip == null || _voiceSource == null) return;
        _voiceSource.Stop();
        _voiceSource.clip = voiceClip;
        _voiceSource.volume = volumeScale;
        _voiceSource.Play();
    }
    public void StopVoice()
    {
        if (_voiceSource != null && _voiceSource.isPlaying)
            _voiceSource.Stop();
    }
    private IEnumerator CrossfadeMusicCoroutine(AudioClip nextClip, float duration)
    {
        AudioSource outgoing = _activeMusicSource;
        AudioSource incoming = (_activeMusicSource == _musicSourceA) ? _musicSourceB : _musicSourceA;

        if (nextClip != null)
        {
            incoming.clip = nextClip;
            incoming.volume = 0f;
            incoming.Play();
        }

        float startVol = outgoing != null && outgoing.isPlaying ? outgoing.volume : 0f;
        float elapsed = 0f;

        if (duration <= 0.01f)
        {
            if (outgoing != null) { outgoing.Stop(); outgoing.volume = 0f; }
            if (nextClip != null) incoming.volume = 1f;
        }
        else
        {
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if (outgoing != null && outgoing.isPlaying)
                    outgoing.volume = Mathf.Lerp(startVol, 0f, t);

                if (nextClip != null)
                    incoming.volume = Mathf.Lerp(0f, 1f, t);

                yield return null;
            }

            if (outgoing != null)
            {
                outgoing.Stop();
                outgoing.clip = null;
                outgoing.volume = 0f;
            }

            if (nextClip != null)
                incoming.volume = 1f;
        }

        _activeMusicSource = nextClip != null ? incoming : outgoing;
        _crossfadeCoroutine = null;
    }
}