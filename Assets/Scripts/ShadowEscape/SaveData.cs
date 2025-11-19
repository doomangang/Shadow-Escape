using System;

namespace ShadowEscape
{
    [Serializable]
    public class SaveData
    {
        // 각 레벨의 잠금 해제 여부
        public bool[] isLevelAvailable;

        // 각 레벨에서 획득한 별 개수 (0~3)
        public int[] starsEarnedAtLevel;

        // 생성자: 초기 저장 데이터를 만듭니다.
        // totalLevels: 게임의 총 레벨 개수
        public SaveData(int totalLevels)
        {
            isLevelAvailable = new bool[totalLevels];
            starsEarnedAtLevel = new int[totalLevels];

            // 첫 번째 레벨(인덱스 0)만 처음부터 잠금 해제
            if (totalLevels > 0)
            {
                isLevelAvailable[0] = true;
            }
        }
    }
}
