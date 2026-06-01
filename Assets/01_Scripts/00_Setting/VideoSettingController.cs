using UnityEngine;

public class VideoSettingController : MonoBehaviour
{
    private void Start()
    {
        // 매니저에 이벤트 등록 및 최초 적용
        SettingManager.Instance.OnVideoSettingsChanged += ApplyVideoSettings;
        ApplyVideoSettings();
    }

    // VideoSettingController.cs 내부의 함수
    private void ApplyVideoSettings()
    {
        SettingData data = SettingManager.Instance.CurrentData;

        // 드롭다운 인덱스에 따라 화면 모드 결정
        FullScreenMode mode = FullScreenMode.FullScreenWindow; // 기본: 전체화면
        if (data.screenModeIndex == 1) mode = FullScreenMode.Windowed; // 창모드
        else if (data.screenModeIndex == 2) mode = FullScreenMode.MaximizedWindow; // 테두리 없는 창모드

        Screen.SetResolution(data.resolutionWidth, data.resolutionHeight, mode);
        Application.targetFrameRate = data.fpsLimit;
    }

    private void OnDestroy()
    {
        if (SettingManager.Instance != null)
            SettingManager.Instance.OnVideoSettingsChanged -= ApplyVideoSettings;
    }
}