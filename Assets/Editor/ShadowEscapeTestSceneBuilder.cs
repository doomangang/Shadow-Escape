#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ShadowEscapeTestSceneBuilder
{
    [MenuItem("Tools/ShadowEscape/Create Test Scene")]
    public static void CreateTestScene()
    {

        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Cannot create scene while Playing",
                "Please exit Play Mode before creating a test scene.\n(Or run a runtime scene creation path if you need it during Play Mode)",
                "OK");
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // 메인 카메라 조정 (기존 카메라가 있어도 URP 추가 데이터 컴포넌트 시도)
        var cam = Camera.main;
        if (cam == null)
        {
            var camGO = new GameObject("Main Camera");
            cam = camGO.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.transform.position = new Vector3(0, 2, -5);
            cam.transform.LookAt(Vector3.zero);
        }
        // URP AdditionalCameraData 부착 (Reflection 사용, 중복시 무시)
        var urpType = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
        if (urpType != null && cam.GetComponent(urpType) == null)
        {
            cam.gameObject.AddComponent(urpType);
        }

        // Directional Light
        if (GameObject.FindObjectOfType<Light>() == null)
        {
            var lightGO = new GameObject("Directional Light");
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        // GameManager 생성
        var gmGO = new GameObject("GameManager");
        gmGO.AddComponent<ShadowEscape.GameManager>();

        // LevelManager 생성
        var lmGO = new GameObject("LevelManager");
        var lm = lmGO.AddComponent<ShadowEscape.LevelManager>();

        // Piece (cube)
        var pieceGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pieceGO.name = "Piece_Cube";
        pieceGO.transform.position = new Vector3(-0.5f, 0.5f, 0f);
        var piece = pieceGO.AddComponent<ShadowEscape.Piece>();
        // Collider는 이미 존재 (BoxCollider) - 레이캐스트 선택을 위해 Rigidbody를 추가하지 않음

        // Target (empty)
        var targetGO = new GameObject("Target_Cube");
        targetGO.transform.position = pieceGO.transform.position; // 정답 위치로 동일하게 세팅
        var target = targetGO.AddComponent<ShadowEscape.TargetPiece>();
        // target는 보이지 않도록 MeshRenderer 없음

        // 페어 추가
        lm.pairs.Add(new ShadowEscape.LevelManager.PieceTargetPair { piece = piece, target = target });

        // 씬 저장
        string scenePath = "Assets/Scenes/ShadowEscape_TestScene.unity";

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, scenePath);

        // 포커스: 씬을 에디터에서 열어줌
        EditorSceneManager.OpenScene(scenePath);

        Debug.Log("ShadowEscape test scene created at " + scenePath + " — open it in the Editor to test.");
    }
}
#endif
