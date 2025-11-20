using UnityEngine;

namespace ShadowEscape
{
    // 정답 판정용 타겟 객체
    // TargetPiece의 Transform이 정답 위치/회전을 정의.
    public class TargetPiece : MonoBehaviour
    {
        [Header("Validation Tolerances")]
        [Tooltip("Position tolerance in meters. Increase if matching is too strict.")]
        public float positionTolerance = 0.1f; // meter

        [Tooltip("Rotation tolerance in degrees. Increase if matching is too strict.")]
        public float rotationTolerance = 5f;    // degree

        // 지정한 pieceTransform이 정답 범위에 들어오는지 검사
        public bool CheckIsCorrect(Transform pieceTransform)
        {
            float posDist = Vector3.Distance(transform.position, pieceTransform.position);
            float rotAngle = Quaternion.Angle(transform.rotation, pieceTransform.rotation);

            return posDist <= positionTolerance && rotAngle <= rotationTolerance;
        }
    }
}
