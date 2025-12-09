using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShadowEscape.Editor
{
    /// <summary>
    /// Scene 복사 후 필수 체크를 자동으로 수행하는 도구
    /// </summary>
    public class SceneSetupValidator : EditorWindow
    {
        [MenuItem("Shadow Escape/Validate Scene Setup")]
        public static void ShowWindow()
        {
            GetWindow<SceneSetupValidator>("Scene Validator");
        }

        private Vector2 scrollPos;

        private void OnGUI()
        {
            GUILayout.Label("Scene Setup Validation", EditorStyles.boldLabel);
            
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            
            // 1. Build Settings 체크
            GUILayout.Space(10);
            GUILayout.Label("1. Build Settings", EditorStyles.boldLabel);
            
            int sceneCount = SceneManager.sceneCountInBuildSettings;
            EditorGUILayout.LabelField("Scenes in Build Settings:", sceneCount.ToString());
            
            for (int i = 0; i < sceneCount; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                EditorGUILayout.LabelField($"  [{i}] {name}");
            }
            
            // 2. SceneFlowManager 체크
            GUILayout.Space(10);
            GUILayout.Label("2. SceneFlowManager", EditorStyles.boldLabel);
            
            var manager = FindFirstObjectByType<SceneFlowManager>();
            if (manager != null)
            {
                EditorGUILayout.LabelField("✅ Found in scene");
                EditorGUILayout.LabelField("Title Scene:", manager.titleSceneName ?? "NOT SET");
                EditorGUILayout.LabelField("Level Select Scene:", manager.levelSelectSceneName ?? "NOT SET");
                
                if (manager.levelSceneNames != null && manager.levelSceneNames.Count > 0)
                {
                    EditorGUILayout.LabelField($"Level Scenes: {manager.levelSceneNames.Count}");
                    foreach (var level in manager.levelSceneNames)
                    {
                        EditorGUILayout.LabelField($"  - {level}");
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Level Scenes: (will auto-populate at runtime)");
                }
            }
            else
            {
                EditorGUILayout.HelpBox("⚠️ SceneFlowManager not found in current scene", MessageType.Warning);
            }
            
            // 3. 중복 Scene 체크
            GUILayout.Space(10);
            GUILayout.Label("3. Duplicate Check", EditorStyles.boldLabel);
            
            string[] allScenes = System.IO.Directory.GetFiles("Assets/Scenes", "*.unity");
            var sceneNames = new System.Collections.Generic.HashSet<string>();
            var duplicates = new System.Collections.Generic.List<string>();
            
            foreach (var scenePath in allScenes)
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(scenePath).ToLower();
                if (sceneNames.Contains(name))
                {
                    duplicates.Add(name);
                }
                sceneNames.Add(name);
            }
            
            if (duplicates.Count > 0)
            {
                EditorGUILayout.HelpBox($"⚠️ Potential duplicates found: {string.Join(", ", duplicates)}", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField("✅ No duplicates found");
            }
            
            GUILayout.EndScrollView();
            
            // 액션 버튼들
            GUILayout.Space(20);
            if (GUILayout.Button("Open Build Settings"))
            {
                EditorApplication.ExecuteMenuItem("File/Build Settings...");
            }
            
            if (GUILayout.Button("Refresh"))
            {
                Repaint();
            }
        }
    }
}
