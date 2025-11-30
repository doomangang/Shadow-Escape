using System;
using System.Collections.Generic;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace ShadowEscape
{
    [System.Serializable]
    public struct NamedAudioClip
    {
        public string key;
        public AudioClip clip;
    }

    [Serializable]
    public struct SceneBgmEntry
    {
        public string sceneName;
        public string bgmKey;
    }

    /// <summary>
    /// Centralized BGM/SFX controller that satisfies the PRD bonus requirement for audio options.
    /// Lives across scenes and exposes high-level helpers for UI to manipulate.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityObject.FindFirstObjectByType<AudioManager>(FindObjectsInactive.Exclude);
                    if (_instance == null)
                    {
                        var go = new GameObject("AudioManager");
                        _instance = go.AddComponent<AudioManager>();
                    }
                }

                return _instance;
            }
        }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Clip Library")]
    [SerializeField] private List<NamedAudioClip> bgmClips = new List<NamedAudioClip>();
    [SerializeField] private List<NamedAudioClip> sfxClips = new List<NamedAudioClip>();
    [Tooltip("Optional overrides that map a Unity scene to a specific BGM key. If empty, the scene name will be used as the lookup key.")]
    [SerializeField] private List<SceneBgmEntry> sceneBgmOverrides = new List<SceneBgmEntry>();
    [Tooltip("Fallback BGM key used when no scene-specific mapping exists.")]
    [SerializeField] private string defaultBgmKey;

        [Header("State")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField] private bool isMuted = false;

        private Dictionary<string, AudioClip> _bgmMap;
        private Dictionary<string, AudioClip> _sfxMap;

        public float MasterVolume => masterVolume;
        public bool IsMuted => isMuted;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureAudioSources();
            BuildClipLookups();
            ApplyVolumeState();
        }

        private void EnsureAudioSources()
        {
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
            }
        }

        private void BuildClipLookups()
        {
            _bgmMap = new Dictionary<string, AudioClip>();
            foreach (var entry in bgmClips)
            {
                if (!string.IsNullOrEmpty(entry.key) && entry.clip != null)
                {
                    _bgmMap[entry.key] = entry.clip;
                }
            }

            _sfxMap = new Dictionary<string, AudioClip>();
            foreach (var entry in sfxClips)
            {
                if (!string.IsNullOrEmpty(entry.key) && entry.clip != null)
                {
                    _sfxMap[entry.key] = entry.clip;
                }
            }
        }

        private void ApplyVolumeState()
        {
            var effectiveVolume = isMuted ? 0f : masterVolume;
            if (bgmSource != null)
            {
                bgmSource.volume = effectiveVolume;
            }

            if (sfxSource != null)
            {
                sfxSource.volume = effectiveVolume;
            }
        }

        public void PlayBGM(string key)
        {
            if (bgmSource == null)
            {
                EnsureAudioSources();
            }

            if (_bgmMap == null)
            {
                BuildClipLookups();
            }

            if (!_bgmMap.TryGetValue(key, out var clip) || clip == null)
            {
                Debug.LogWarning($"[AudioManager] BGM clip not found for key '{key}'.");
                return;
            }

            if (bgmSource.clip == clip && bgmSource.isPlaying)
            {
                return;
            }

            bgmSource.clip = clip;
            bgmSource.loop = true;
            bgmSource.Play();
        }

        public void PlayBGMForScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            if (_bgmMap == null)
            {
                BuildClipLookups();
            }

            string resolvedKey = ResolveBgmKeyForScene(sceneName);
            if (!string.IsNullOrEmpty(resolvedKey) && _bgmMap.ContainsKey(resolvedKey))
            {
                PlayBGM(resolvedKey);
            }
            else if (!string.IsNullOrEmpty(defaultBgmKey) && _bgmMap.ContainsKey(defaultBgmKey))
            {
                PlayBGM(defaultBgmKey);
            }
            else
            {
                StopBGM();
            }
        }

        private string ResolveBgmKeyForScene(string sceneName)
        {
            if (sceneBgmOverrides != null)
            {
                for (int i = 0; i < sceneBgmOverrides.Count; i++)
                {
                    var entry = sceneBgmOverrides[i];
                    if (!string.IsNullOrEmpty(entry.sceneName) && string.Equals(entry.sceneName, sceneName, StringComparison.Ordinal))
                    {
                        return entry.bgmKey;
                    }
                }
            }

            return sceneName;
        }

        public void StopBGM()
        {
            if (bgmSource != null)
            {
                bgmSource.Stop();
            }
        }

        public void PlaySFX(string key)
        {
            if (sfxSource == null)
            {
                EnsureAudioSources();
            }

            if (_sfxMap == null)
            {
                BuildClipLookups();
            }

            if (!_sfxMap.TryGetValue(key, out var clip) || clip == null)
            {
                Debug.LogWarning($"[AudioManager] SFX clip not found for key '{key}'.");
                return;
            }

            sfxSource.PlayOneShot(clip, isMuted ? 0f : masterVolume);
        }

        public void SetVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
            ApplyVolumeState();
        }

        public void ToggleMute(bool mute)
        {
            isMuted = mute;
            ApplyVolumeState();
        }
    }
}
