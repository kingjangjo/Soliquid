using UnityEngine;

public class VideoSettingController : MonoBehaviour
{
    private void Start()
    {
        // 매니저에 이벤트 등록 및 최초 적용
        SettingManager.Instance.OnVideoSettingsChanged += ApplyVideoSettings;
        ApplyVideoSettings();
    }

    private void ApplyVideoSettings()
    {
        SettingData data = SettingManager.Instance.CurrentData;

        // 1. 해상도 및 창모드 적용
        Screen.SetResolution(data.resolutionWidth, data.resolutionHeight, data.isFullscreen);

        // 2. 프레임 제한 적용
        Application.targetFrameRate = data.fpsLimit;

        Debug.Log($"그래픽 적용: {data.resolutionWidth}x{data.resolutionHeight}, FullScreen={data.isFullscreen}, FPS={data.fpsLimit}");
    }

    private void OnDestroy()
    {
        if (SettingManager.Instance != null)
            SettingManager.Instance.OnVideoSettingsChanged -= ApplyVideoSettings;
    }
}