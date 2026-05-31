using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using System.Collections;

[Serializable]
public class LanguageFontMapping
{
    public string languageCode; // "KO", "EN", "JA" 등
    public TMP_FontAsset fontAsset; // 해당 언어용 TextMeshPro 폰트 에셋
}

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    [Header("구글 시트 웹 게시 CSV URL")]
    [SerializeField] private string googleSheetCSVUrl;

    [Header("언어별 폰트 설정")]
    [SerializeField] private List<LanguageFontMapping> fontMappings;

    // 파싱된 데이터를 담을 딕셔너리 <ID, <언어코드, 실제대사>>
    private Dictionary<string, Dictionary<string, string>> localizedTable = new Dictionary<string, Dictionary<string, string>>();

    // 현재 활성화된 폰트
    public TMP_FontAsset CurrentFont { get; private set; }

    // 대사 변경 시 UI 텍스트 컴포넌트들이 구독할 이벤트
    public event Action OnLanguageUpdated;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SettingManager.Instance.OnLocalizationChanged += HandleLanguageChange;
        StartCoroutine(DownloadLocalizationData());
    }

    // 1. 외부 구글 시트 다운로드 및 파싱
    private IEnumerator DownloadLocalizationData()
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(googleSheetCSVUrl))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                ParseCSV(webRequest.downloadHandler.text);
                HandleLanguageChange(); // 다운로드 완료 후 현재 언어 적용
            }
            else
            {
                Debug.LogError($"구글 시트 로드 실패: {webRequest.error}");
            }
        }
    }

    private void ParseCSV(string csvText)
    {
        string[] rows = csvText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        if (rows.Length == 0) return;

        // 첫 번째 줄(헤더) 파싱하여 언어 코드 위치 파악 (예: ID, KO, EN, JA)
        string[] headers = rows[0].Split(',');

        for (int i = 1; i < rows.Length; i++)
        {
            string[] cells = rows[i].Split(',');
            if (cells.Length < headers.Length) continue;

            string id = cells[0].Trim();
            localizedTable[id] = new Dictionary<string, string>();

            for (int j = 1; j < headers.Length; j++)
            {
                string langKey = headers[j].Trim();
                string textValue = cells[j].Trim().Replace("\\n", "\n"); // 줄바꿈 문자 처리
                localizedTable[id][langKey] = textValue;
            }
        }
    }

    // 2. 언어 변경 처리 (폰트 교체 및 이벤트 전파)
    private void HandleLanguageChange()
    {
        string currentLang = SettingManager.Instance.CurrentData.languageCode;

        // 폰트 매핑 찾기
        LanguageFontMapping mapping = fontMappings.Find(m => m.languageCode == currentLang);
        if (mapping != null && mapping.fontAsset != null)
        {
            CurrentFont = mapping.fontAsset;
        }

        // 전역 UI에 변경 통보
        OnLanguageUpdated?.Invoke();
    }

    // UI에서 ID를 던지면 현재 언어에 맞는 글자를 반환하는 헬퍼 함수
    public string GetLocalizedString(string id)
    {
        string currentLang = SettingManager.Instance.CurrentData.languageCode;
        if (localizedTable.ContainsKey(id) && localizedTable[id].ContainsKey(currentLang))
        {
            return localizedTable[id][currentLang];
        }
        return $"[{id}]"; // 데이터가 없을 경우 ID 자체를 출력하여 에러 확인
    }
}