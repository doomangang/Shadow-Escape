using UnityEngine;

namespace ShadowEscape
{
    // 플레이어가 조작하는 퍼즐 조각
    // LevelManager가 입력을 처리하고 Rotate/Move를 호출
    public class Piece : MonoBehaviour
    {
        // 정답 여부 (LevelManager 또는 TargetPiece 체크로 설정)
        public bool IsCorrect { get; set; }

        // 난이도/설정용 플래그
        [SerializeField] private bool canMove = true;
        [SerializeField] private bool canRotate = true;

        // 회전: LevelManager에서 마우스 드래그 델타를 전달
        // deltaX: 수평 드래그량, deltaY: 수직 드래그량
        public void Rotate(float deltaX, float deltaY)
        {
            if (!canRotate) return;

            transform.Rotate(Vector3.up, deltaX, Space.World);
            transform.Rotate(Vector3.right, -deltaY, Space.World);
        }

        // 이동: LevelManager에서 Shift+드래그일 때 호출
        // deltaX, deltaY는 스크린/월드 스케일에 맞춰 LevelManager에서 조절
        public void Move(float deltaX, float deltaY)
        {
            if (!canMove) return;

            // 월드 X/Y 평면으로 이동
            transform.Translate(new Vector3(deltaX, deltaY, 0f), Space.World);
        }
    }
}
