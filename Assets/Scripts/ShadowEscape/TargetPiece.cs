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
    }
}
