using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingUIBridge : MonoBehaviour
{
    [Header("오디오 UI")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider bgmSlider;

    [Header("비디오 UI")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown screenModeDropdown;
    [SerializeField] private TMP_Dropdown fpsDropdown;

    [Header("언어 UI")]
    [SerializeField] private TMP_Dropdown languageDropdown;

    private void Start()
    {
        // 1. 기존에 저장되어 있던 값으로 UI 초기 상태 세팅
        SettingData data = SettingManager.Instance.CurrentData;

        masterSlider.value = data.masterVolume;
        sfxSlider.value = data.sfxVolume;
        bgmSlider.value = data.bgmVolume;
        screenModeDropdown.value = SettingManager.Instance.CurrentData.screenModeIndex;

        // 2. UI 조작 시 매니저의 데이터 변경 함수 호출하도록 연결 (리스너 등록)
        masterSlider.onValueChanged.AddListener(value => OnAudioSliderChanged());
        sfxSlider.onValueChanged.AddListener(value => OnAudioSliderChanged());
        bgmSlider.onValueChanged.AddListener(value => OnAudioSliderChanged());

        screenModeDropdown.onValueChanged.AddListener(index => OnVideoUIChanged());
        resolutionDropdown.onValueChanged.AddListener(index => OnVideoUIChanged());
        fpsDropdown.onValueChanged.AddListener(index => OnVideoUIChanged());

        languageDropdown.onValueChanged.AddListener(index => OnLanguageDropdownChanged());
    }

    private void OnAudioSliderChanged()
    {
        SettingManager.Instance.UpdateAudioSettings(masterSlider.value, sfxSlider.value, bgmSlider.value);
    }

    private void OnVideoUIChanged()
    {
        // 해상도 드롭다운 인덱스에 따라 분기 처리 예시
        int width = 1920; int height = 1080;
        if (resolutionDropdown.value == 1) { width = 1280; height = 720; }

        // FPS 드롭다운 인덱스 분기 처리 예시
        int fps = 60;
        if (fpsDropdown.value == 1) fps = 144;
        else if (fpsDropdown.value == 2) fps = -1; // 무제한

        SettingManager.Instance.UpdateVideoSettings(width, height, screenModeDropdown.value, fps);
    }

    private void OnLanguageDropdownChanged()
    {
        string code = "KO";
        if (languageDropdown.value == 1) code = "EN";
        else if (languageDropdown.value == 2) code = "JA";

        SettingManager.Instance.UpdateLanguage(code);
    }

    // 설정 창을 닫을 때나 '저장' 버튼을 누를 때 최종 JSON 파일 저장
    public void OnClickSaveAndClose()
    {
        SettingManager.Instance.SaveSettings();
        gameObject.SetActive(false); // 설정창 끄기
    }
}