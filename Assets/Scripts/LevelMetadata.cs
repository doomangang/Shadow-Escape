using UnityEngine;

namespace ShadowEscape
{
    // 레벨별 힌트 & 난이도 &(추후 tolerance 주입 예정)
    public enum DifficultyTier { Easy, Medium, Hard }

    public class LevelMetadata : MonoBehaviour
    {
        [Header("Identification")]
        [Tooltip("레벨 인덱스(저장/언락과 매칭)")] public int levelIndex = 0;
        [Tooltip("힌트(Subject: 퍼즐 이름이 실루엣에 대한 단서 제공)")] public string titleHint = "";

        [Header("Difficulty")]
        [Tooltip("난이도 단계: Easy=수평 회전만, Medium=수평+수직 회전, Hard=회전+이동")] public DifficultyTier difficulty = DifficultyTier.Easy;
        
        [Header("Validation Tolerances (Unified)")]
        [Tooltip("Global position tolerance (meters) applied to all TargetPiece instances at runtime.")] public float positionTolerance = 0.1f;
        [Tooltip("Global rotation tolerance (degrees) applied to all TargetPiece instances at runtime.")] public float rotationTolerance = 5f;
    }
}
