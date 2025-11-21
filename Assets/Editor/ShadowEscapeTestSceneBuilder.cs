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

        var gmGO = new GameObject("GameManager");
        gmGO.AddComponent<ShadowEscape.GameManager>();

        var lmGO = new GameObject("LevelManager");
        var lm = lmGO.AddComponent<ShadowEscape.LevelManager>();

    // Piece (cube) - 시작 위치
    var pieceGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
    pieceGO.name = "Piece_Cube";
    pieceGO.transform.position = new Vector3(-0.5f, 0.5f, 0f);
        var piece = pieceGO.AddComponent<ShadowEscape.Piece>();
        // Collider는 이미 존재 (BoxCollider) - 레이캐스트 선택을 위해 Rigidbody를 추가하지 않음

    // Target (empty) - 바로 정답이 되지 않도록 약간 위치/회전 차이를 둔다.
    var targetGO = new GameObject("Target_Cube");
    // 위치는 약간 offset, 회전도 약간 변경 (Tolerance 기본값을 넘어가도록)
    targetGO.transform.position = pieceGO.transform.position + new Vector3(0.25f, 0.0f, 0.15f);
    targetGO.transform.rotation = pieceGO.transform.rotation * Quaternion.Euler(0f, 25f, 0f);
        var target = targetGO.AddComponent<ShadowEscape.TargetPiece>();
        // target는 보이지 않도록 MeshRenderer 없음

        lm.pairs.Add(new ShadowEscape.LevelManager.PieceTargetPair { piece = piece, target = target });

        string scenePath = "Assets/Scenes/ShadowEscape_TestScene.unity";

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, scenePath);

        EditorSceneManager.OpenScene(scenePath);

        Debug.Log("[ShadowEscapeTestSceneBuilder] Test scene created at: " + scenePath +
                  "\nPiece initial position: " + pieceGO.transform.position.ToString("F3") +
                  "\nTarget initial position: " + targetGO.transform.position.ToString("F3") +
                  "\nRotation offset applied (Y +25°) so puzzle is NOT solved initially.");
    }
}
#endif
