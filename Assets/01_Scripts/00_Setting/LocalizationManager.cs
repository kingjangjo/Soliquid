using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

[Serializable]
public class LanguageFontMapping
{
    public string languageCode;
    public TMP_FontAsset fontAsset;
}

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    [Header("구글 시트 CSV URL (output=csv 형식)")]
    [SerializeField] private string googleSheetCSVUrl;
    // ✅ 수정 이유: pubhtml URL은 HTML 페이지를 반환하므로 CSV 파싱 불가.
    // Inspector에 아래 URL을 입력하세요:
    // https://docs.google.com/spreadsheets/d/e/2PACX-1vRqrVqIg9hCP0l4-Iu1JjKIxDfgO2tjtOrddiYxdWhg7jqTJracX6JlN7OOMGscDgRVpFL75q79ou3i/pub?gid=1946011291&single=true&output=csv

    [Header("언어별 폰트 설정")]
    [SerializeField] private List<LanguageFontMapping> fontMappings;

    private Dictionary<string, Dictionary<string, string>> localizedTable
        = new Dictionary<string, Dictionary<string, string>>();

    public TMP_FontAsset CurrentFont { get; private set; }

    public event Action OnLanguageUpdated;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // ✅ 수정 이유: Start()에서 구독해도 되지만,
        // SettingManager보다 늦게 초기화되면 초기 언어 적용이 누락될 수 있음.
        // SettingManager가 Awake()에서 싱글톤 세팅을 마치므로 Start()에서 구독은 안전함.
        // 단, SettingManager의 Script Execution Order가 LocalizationManager보다 먼저여야 함.
        SettingManager.Instance.OnLocalizationChanged += HandleLanguageChange;
        StartCoroutine(DownloadLocalizationData());
    }

    private IEnumerator DownloadLocalizationData()
    {
        if (string.IsNullOrEmpty(googleSheetCSVUrl))
        {
            Debug.LogError("[LocalizationManager] CSV URL이 비어 있습니다. Inspector에서 설정해주세요.");
            yield break;
        }

        using (UnityWebRequest webRequest = UnityWebRequest.Get(googleSheetCSVUrl))
        {
            // ✅ 추가: 구글 시트는 리다이렉트를 사용하므로 허용 필요
            webRequest.redirectLimit = 10;
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string csvText = webRequest.downloadHandler.text;
                Debug.Log($"[LocalizationManager] CSV 수신 성공. 길이: {csvText.Length}자");
                ParseCSV(csvText);
                HandleLanguageChange();
            }
            else
            {
                Debug.LogError($"[LocalizationManager] 구글 시트 로드 실패: {webRequest.error}\nURL: {googleSheetCSVUrl}");
            }
        }
    }

    private void ParseCSV(string csvText)
    {
        localizedTable.Clear();

        // ✅ 수정 이유: \r\n, \r, \n 모두 처리
        string[] rows = csvText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (rows.Length < 2)
        {
            Debug.LogWarning("[LocalizationManager] CSV 행이 2줄 미만입니다. 헤더만 있거나 비어 있습니다.");
            return;
        }

        // 헤더 파싱 (예: KEY, KO, EN, JA)
        string[] headers = ParseCSVRow(rows[0]);
        Debug.Log($"[LocalizationManager] 헤더: {string.Join(" | ", headers)}");

        for (int i = 1; i < rows.Length; i++)
        {
            string[] cells = ParseCSVRow(rows[i]);
            if (cells.Length == 0 || string.IsNullOrWhiteSpace(cells[0])) continue;

            string id = cells[0].Trim();
            localizedTable[id] = new Dictionary<string, string>();

            for (int j = 1; j < headers.Length && j < cells.Length; j++)
            {
                string langKey = headers[j].Trim();
                // ✅ 수정 이유: \n 이스케이프 처리 + 앞뒤 공백 제거
                string textValue = cells[j].Trim().Replace("\\n", "\n");
                localizedTable[id][langKey] = textValue;
            }
        }

        Debug.Log($"[LocalizationManager] 파싱 완료. 총 {localizedTable.Count}개 항목 로드됨.");
    }

    // ✅ 추가된 함수: 따옴표로 감싸진 CSV 필드를 올바르게 파싱
    // 이유: 텍스트에 쉼표가 포함된 경우 "hello, world" 처럼 감싸짐.
    // 단순 Split(',')은 이를 2개 셀로 잘못 분리함.
    private string[] ParseCSVRow(string row)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();

        for (int i = 0; i < row.Length; i++)
        {
            char c = row[i];

            if (c == '"')
            {
                // 연속된 "" → 실제 따옴표 문자
                if (inQuotes && i + 1 < row.Length && row[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString());
        return result.ToArray();
    }

    private void HandleLanguageChange()
    {
        string currentLang = SettingManager.Instance.CurrentData.languageCode;

        LanguageFontMapping mapping = fontMappings.Find(m => m.languageCode == currentLang);
        if (mapping != null && mapping.fontAsset != null)
        {
            CurrentFont = mapping.fontAsset;
        }

        OnLanguageUpdated?.Invoke();
    }

    public string GetLocalizedString(string id)
    {
        if (string.IsNullOrEmpty(id)) return "";

        string currentLang = SettingManager.Instance.CurrentData.languageCode;

        if (localizedTable.TryGetValue(id, out var langMap))
        {
            if (langMap.TryGetValue(currentLang, out string text))
                return text;

            // ✅ 추가: 현재 언어 없으면 KO 폴백, 그래도 없으면 ID 반환
            if (langMap.TryGetValue("KO", out string fallback))
            {
                Debug.LogWarning($"[LocalizationManager] '{id}'에 '{currentLang}' 없음. KO로 대체.");
                return fallback;
            }
        }

        Debug.LogWarning($"[LocalizationManager] ID '{id}'를 찾을 수 없습니다.");
        return $"[{id}]";
    }

    private void OnDestroy()
    {
        if (SettingManager.Instance != null)
            SettingManager.Instance.OnLocalizationChanged -= HandleLanguageChange;
    }
}