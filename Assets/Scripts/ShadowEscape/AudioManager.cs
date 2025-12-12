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
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 씬에 이미 있는 AudioManager를 먼저 찾기
                    _instance = UnityObject.FindFirstObjectByType<AudioManager>(FindObjectsInactive.Exclude);
                    if (_instance == null)
                    {
                        Debug.LogWarning("[AudioManager] No AudioManager found in scene. Creating runtime instance (will have no clips configured!)");
                        var go = new GameObject("AudioManager_Runtime");
                        _instance = go.AddComponent<AudioManager>();
                    }
                    else
                    {
                        Debug.Log($"[AudioManager] Found existing AudioManager in scene: {_instance.gameObject.name}");
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

        [Header("Music Categories")]
        [Tooltip("BGM key for menu scenes (00_Home, 01_Menu)")]
        [SerializeField] private string menuBgmKey = "Menu";
        [Tooltip("BGM key for all gameplay levels")]
        [SerializeField] private string gameplayBgmKey = "Gameplay";

        [Header("State")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField] private bool isMuted = false;

        private Dictionary<string, AudioClip> _bgmMap;
        private Dictionary<string, AudioClip> _sfxMap;
        private string _currentBgmCategory = "";

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

        private void Start()
        {
            // 현재 씬의 음악 재생
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            PlayBGMForScene(currentScene);
            Debug.Log($"[AudioManager] Started in scene: {currentScene}");
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
                    Debug.Log($"[AudioManager] Registered BGM: {entry.key} -> {entry.clip.name}");
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
            
            Debug.Log($"[AudioManager] Total BGM clips registered: {_bgmMap.Count}");
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
                Debug.LogWarning($"[AudioManager] BGM clip not found for key '{key}'. Available keys: {string.Join(", ", _bgmMap.Keys)}");
                return;
            }

            if (bgmSource.clip == clip && bgmSource.isPlaying)
            {
                Debug.Log($"[AudioManager] BGM '{key}' already playing");
                return;
            }

            bgmSource.clip = clip;
            bgmSource.loop = true;
            bgmSource.volume = isMuted ? 0f : masterVolume;
            bgmSource.Play();
            Debug.Log($"[AudioManager] Playing BGM '{key}' (clip: {clip.name}, volume: {bgmSource.volume})");
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

            // 씬 카테고리 판단
            string targetCategory = GetSceneCategory(sceneName);
            
            // 같은 카테고리면 음악 유지
            if (targetCategory == _currentBgmCategory && bgmSource != null && bgmSource.isPlaying)
            {
                Debug.Log($"[AudioManager] Keeping current BGM for category '{targetCategory}'");
                return;
            }

            // 카테고리에 맞는 BGM 재생
            string bgmKey = GetBgmKeyForCategory(targetCategory);
            if (!string.IsNullOrEmpty(bgmKey) && _bgmMap.ContainsKey(bgmKey))
            {
                _currentBgmCategory = targetCategory;
                PlayBGM(bgmKey);
                Debug.Log($"[AudioManager] Playing BGM '{bgmKey}' for category '{targetCategory}'");
            }
            else if (!string.IsNullOrEmpty(defaultBgmKey) && _bgmMap.ContainsKey(defaultBgmKey))
            {
                _currentBgmCategory = "default";
                PlayBGM(defaultBgmKey);
            }
            else
            {
                _currentBgmCategory = "";
                StopBGM();
            }
        }

        private string GetSceneCategory(string sceneName)
        {
            // 메뉴 씬 (00_Home, 01_Menu 등)
            if (sceneName.Contains("Home") || sceneName.Contains("Menu") || sceneName.Contains("00_") || sceneName.Contains("01_"))
            {
                return "menu";
            }
            
            // 레벨 씬 (Level_, 숫자로 시작, 등)
            if (sceneName.Contains("Level") || sceneName.StartsWith("02_") || sceneName.StartsWith("03_") || sceneName.StartsWith("04_"))
            {
                return "gameplay";
            }

            return "default";
        }

        private string GetBgmKeyForCategory(string category)
        {
            switch (category)
            {
                case "menu":
                    return menuBgmKey;
                case "gameplay":
                    return gameplayBgmKey;
                default:
                    return defaultBgmKey;
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
