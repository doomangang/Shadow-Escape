using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace ShadowEscape.Environment
{
    /// <summary>
    /// 모든 레벨에 동일한 환경 설정을 적용하는 매니저
    /// </summary>
    public class EnvironmentSetupManager : MonoBehaviour
    {
        [Header("Lighting Settings")]
        [Tooltip("Scene에 적용할 Lighting Settings Asset")]
        public UnityEngine.Object lightingSettings;
        
        [Header("Post Processing")]
        [Tooltip("Volume Profile (Post-Processing 설정)")]
        public UnityEngine.Rendering.VolumeProfile volumeProfile;
        
        [Header("Fog Settings")]
        public bool enableFog = true;
        public Color fogColor = Color.gray;
        public float fogDensity = 0.01f;
        
        [Header("Ambient Light")]
        public UnityEngine.Rendering.AmbientMode ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        public Color ambientSkyColor = Color.gray;
        public Color ambientEquatorColor = Color.gray;
        public Color ambientGroundColor = Color.gray;
        
#if UNITY_EDITOR
        [ContextMenu("Apply Settings to Current Scene")]
        public void ApplySettingsToCurrentScene()
        {
            // Fog 설정
            RenderSettings.fog = enableFog;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
            
            // Ambient Light 설정
            RenderSettings.ambientMode = ambientMode;
            if (ambientMode == UnityEngine.Rendering.AmbientMode.Trilight)
            {
                RenderSettings.ambientSkyColor = ambientSkyColor;
                RenderSettings.ambientEquatorColor = ambientEquatorColor;
                RenderSettings.ambientGroundColor = ambientGroundColor;
            }
            
            Debug.Log("✅ Environment settings applied to scene!");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
        
        [ContextMenu("Capture Current Scene Settings")]
        public void CaptureCurrentSceneSettings()
        {
            // 현재 Scene의 설정을 캡처
            enableFog = RenderSettings.fog;
            fogColor = RenderSettings.fogColor;
            fogDensity = RenderSettings.fogDensity;
            
            ambientMode = RenderSettings.ambientMode;
            ambientSkyColor = RenderSettings.ambientSkyColor;
            ambientEquatorColor = RenderSettings.ambientEquatorColor;
            ambientGroundColor = RenderSettings.ambientGroundColor;
            
            Debug.Log("✅ Current scene settings captured!");
            EditorUtility.SetDirty(this);
        }
#endif
    }
}
