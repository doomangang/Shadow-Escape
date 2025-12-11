using UnityEngine;

namespace ShadowEscape
{
    // 정답 판정용 타겟 객체
    // TargetPiece의 Transform이 정답 위치/회전을 정의.
    public class TargetPiece : MonoBehaviour
    {
        [Header("Validation Tolerances (Runtime Injected)")]
        [Tooltip("Position tolerance (meters) injected from LevelMetadata; local edit ignored.")]
        public float positionTolerance = 0.1f; // Will be overridden

        [Tooltip("Rotation tolerance (degrees) injected from LevelMetadata; local edit ignored.")]
        public float rotationTolerance = 5f;    // Will be overridden

        // 지정한 pieceTransform이 정답 범위에 들어오는지 검사
        public bool CheckIsCorrect(Transform pieceTransform)
        {
            float posDist = Vector3.Distance(transform.position, pieceTransform.position);
            float rotAngle = Quaternion.Angle(transform.rotation, pieceTransform.rotation);

            return posDist <= positionTolerance && rotAngle <= rotationTolerance;
        }

        // 정확도 계산 (0~1, 1이 완벽한 정답)
        // Spotlight 색상 피드백 등에 사용
        // includePosition: Hard 난이도에서만 true (Easy/Medium은 회전만)
        public float CalculateAccuracy(Transform pieceTransform, bool includePosition = true)
        {
            float rotAngle = Quaternion.Angle(transform.rotation, pieceTransform.rotation);
            
            // 회전 정확도: tolerance 이내면 1, 멀수록 0에 가까워짐
            float rotAccuracy = Mathf.Clamp01(1f - (rotAngle / (rotationTolerance * 3f)));

            if (!includePosition)
            {
                // Easy/Medium: 회전만 평가
                return rotAccuracy;
            }

            // Hard: 위치 + 회전 평가
            float posDist = Vector3.Distance(transform.position, pieceTransform.position);
            float posAccuracy = Mathf.Clamp01(1f - (posDist / (positionTolerance * 3f)));
            
            // 위치와 회전의 평균
            return (posAccuracy + rotAccuracy) / 2f;
        }
    }
}
