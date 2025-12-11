using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShadowEscape
{
    public class GameManager : MonoBehaviour
    {
        // 다른 스크립트에서 GameManager.Instance로 접근 가능
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                        _instance = Object.FindFirstObjectByType<GameManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameManager");
                        _instance = go.AddComponent<GameManager>();
                    }
                }
                return _instance;
            }
        }

        // ========== 게임 설정 ==========
        [SerializeField] private int totalLevels = 3; // Easy, Medium, Hard
        public int TotalLevels => totalLevels;
        
    // Pause 상태
    private bool _isPaused = false;
    public bool IsPaused => _isPaused;

        // Test Mode 플래그
        // true: 모든 레벨 잠금 해제
        // false: Story Mode, 순차적 잠금 해제
        public bool IsTester = false; // 빌드에서는 항상 false

        // ========== 저장 데이터 ==========
        // 현재 플레이어의 진행 상황
        public SaveData CurrentSave { get; private set; }

        // ========== Unity 생명주기 ==========
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            DontDestroyOnLoad(gameObject);
            LoadGame();
        }

        // ========== 일시정지 ==========
        public void SetPaused(bool paused)
        {
            if (_isPaused == paused) return;
            _isPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            Debug.Log($"[GameManager] Pause state changed: {paused}");
        }

        // ========== 저장/로드 시스템 ==========
        // JsonUtility로 SaveData를 JSON 문자열로 변환 후 PlayerPrefs에 저장
        public void SaveGame()
        {
            if (CurrentSave == null)
            {
                Debug.LogWarning("저장할 데이터가 없습니다.");
                return;
            }

            string json = JsonUtility.ToJson(CurrentSave);

            PlayerPrefs.SetString("save", json);
            PlayerPrefs.Save(); 

            Debug.Log("게임 저장 완료!");
        }

        // 저장된 진행 상황을 불러오고 없으면 새로운 SaveData를 생성
        public void LoadGame()
        {
            Debug.Log($"[GameManager] LoadGame called. IsTester={IsTester}, TotalLevels={totalLevels}");
            
            if (PlayerPrefs.HasKey("save"))
            {
                string json = PlayerPrefs.GetString("save");
                Debug.Log($"[GameManager] Found existing save data: {json}");
                try
                {
                    CurrentSave = JsonUtility.FromJson<SaveData>(json);
                    Debug.Log($"[GameManager] Loaded save. Level 0 unlocked: {CurrentSave.isLevelAvailable[0]}, Level 1 unlocked: {(CurrentSave.isLevelAvailable.Length > 1 ? CurrentSave.isLevelAvailable[1].ToString() : "N/A")}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("Save data corrupted or incompatible, creating new save. " + ex.Message);
                    CurrentSave = new SaveData(totalLevels);
                    SaveGame();
                    return;
                }

                // Migrate or resize arrays if save format doesn't match current totalLevels
                if (CurrentSave == null)
                {
                    CurrentSave = new SaveData(totalLevels);
                }

                // Ensure arrays have correct lengths
                if (CurrentSave.isLevelAvailable == null || CurrentSave.isLevelAvailable.Length != totalLevels ||
                    CurrentSave.starsEarnedAtLevel == null || CurrentSave.starsEarnedAtLevel.Length != totalLevels)
                {
                    // Preserve what we can
                    bool[] oldAvail = CurrentSave.isLevelAvailable;
                    int[] oldStars = CurrentSave.starsEarnedAtLevel;

                    SaveData newSave = new SaveData(totalLevels);
                    int copyLen = 0;
                    if (oldAvail != null)
                        copyLen = Mathf.Min(oldAvail.Length, newSave.isLevelAvailable.Length);

                    for (int i = 0; i < copyLen; i++)
                        newSave.isLevelAvailable[i] = oldAvail[i];

                    copyLen = 0;
                    if (oldStars != null)
                        copyLen = Mathf.Min(oldStars.Length, newSave.starsEarnedAtLevel.Length);

                    for (int i = 0; i < copyLen; i++)
                        newSave.starsEarnedAtLevel[i] = oldStars[i];

                    CurrentSave = newSave;
                    SaveGame();
                }

                Debug.Log("저장된 게임 불러오기 완료!");
            }
            else
            {
                CurrentSave = new SaveData(totalLevels);
                Debug.Log($"[GameManager] 새 게임 시작! Level 0 unlocked: {CurrentSave.isLevelAvailable[0]}, Total levels: {totalLevels}");
            }
        }

        public void ResetSaveData()
        {
            PlayerPrefs.DeleteKey("save");
            CurrentSave = new SaveData(totalLevels);
            Debug.Log("저장 데이터 초기화 완료!");
        }

        // ========== 레벨 관리 ==========
        // levelIndex: 레벨 인덱스 (0부터 시작)
        public bool IsLevelUnlocked(int levelIndex)
        {
            if (IsTester)
            {
                Debug.Log($"[GameManager] IsLevelUnlocked({levelIndex}) = true (Tester Mode)");
                return true;
            }

            if (levelIndex < 0 || levelIndex >= CurrentSave.isLevelAvailable.Length)
            {
                Debug.LogWarning($"잘못된 레벨 인덱스: {levelIndex}");
                return false;
            }

            bool unlocked = CurrentSave.isLevelAvailable[levelIndex];
            Debug.Log($"[GameManager] IsLevelUnlocked({levelIndex}) = {unlocked}");
            return unlocked;
        }

        // 레벨을 클리어하고 다음 레벨을 잠금 해제
        // levelIndex: 클리어한 레벨 인덱스
        // stars: 획득한 별 개수 (0~3)
        public void CompleteLevel(int levelIndex, int stars)
        {
            // 별 개수는 기존보다 높을 때만 업데이트 (최고 기록 유지)
            if (stars > CurrentSave.starsEarnedAtLevel[levelIndex])
            {
                CurrentSave.starsEarnedAtLevel[levelIndex] = stars;
            }

            int nextLevelIndex = levelIndex + 1;
            if (nextLevelIndex < CurrentSave.isLevelAvailable.Length)
            {
                CurrentSave.isLevelAvailable[nextLevelIndex] = true;
            }

            SaveGame();
        }

        // ========== 씬 관리 ==========
        // sceneName: 씬 이름
        public void LoadScene(string sceneName)
        {
            // 흐름 일원화를 위해 SceneFlowManager를 통해 로드
            // (BGM 전환, CompletionUI/EventSystem 보장 등 공통 처리)
            ShadowEscape.SceneFlowManager.Instance?.LoadScene(sceneName);
        }

        public void RestartCurrentScene()
        {
            // 현재 씬 이름을 가져와 SceneFlowManager에 위임
            var currentScene = SceneManager.GetActiveScene();
            ShadowEscape.SceneFlowManager.Instance?.LoadScene(currentScene.name);
        }
    }
}
