using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class SettingManager : MonoBehaviour
{
    public static SettingManager Instance { get; private set; }

    public SettingData CurrentData { get; private set; } = new SettingData();
    [Header("Input System")]
    public InputActionAsset inputActions;
    // 설정 변경을 하위 모듈에 알릴 이벤트들
    public event Action OnAudioSettingsChanged;
    public event Action OnVideoSettingsChanged;
    public event Action OnLocalizationChanged;

    private string saveFilePath;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            saveFilePath = Path.Combine(Application.persistentDataPath, "settings.json");
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // JSON 파일로 저장
    public void SaveSettings()
    {
        try
        {
            string json = JsonUtility.ToJson(CurrentData, true);
            File.WriteAllText(saveFilePath, json);
            Debug.Log($"설정 저장 완료: {saveFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"설정 저장 실패: {e.Message}");
        }
    }

    // JSON 파일 로드
    public void LoadSettings()
    {
        if (File.Exists(saveFilePath))
        {
            try
            {
                string json = File.ReadAllText(saveFilePath);
                CurrentData = JsonUtility.FromJson<SettingData>(json);
                Debug.Log("설정 로드 완료");
            }
            catch (Exception e)
            {
                Debug.LogError($"설정 로드 실패(기본값 사용): {e.Message}");
                CurrentData = new SettingData();
            }
            if (inputActions != null && !string.IsNullOrEmpty(CurrentData.keyBindingsJson))
            {
                inputActions.LoadBindingOverridesFromJson(CurrentData.keyBindingsJson);
            }
        }
        else
        {
            CurrentData = new SettingData(); // 파일 없으면 기본값
        }
    }

    // 데이터 갱신 후 하위 모듈에 전파하는 함수들
    public void UpdateAudioSettings(float master, float sfx, float bgm)
    {
        CurrentData.masterVolume = master;
        CurrentData.sfxVolume = sfx;
        CurrentData.bgmVolume = bgm;
        OnAudioSettingsChanged?.Invoke();
    }

    public void UpdateVideoSettings(int width, int height, int fullScreenIdx, int fps)
    {
        CurrentData.resolutionWidth = width;
        CurrentData.resolutionHeight = height;
        CurrentData.screenModeIndex = fullScreenIdx;
        CurrentData.fpsLimit = fps;
        OnVideoSettingsChanged?.Invoke();
    }

    public void UpdateLanguage(string langCode)
    {
        CurrentData.languageCode = langCode;
        OnLocalizationChanged?.Invoke();
    }
    public void UpdateKeyBindings(string overridesJson)
    {
        CurrentData.keyBindingsJson = overridesJson;
    }
}