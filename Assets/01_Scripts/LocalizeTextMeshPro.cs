using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizeTextMeshPro : MonoBehaviour
{
    [SerializeField] private string localizationTextId; // 구글 시트의 ID 행 이름
    private TextMeshProUGUI textMeshPro;

    private void Awake()
    {
        textMeshPro = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        LocalizationManager.Instance.OnLanguageUpdated += RefreshUI;
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (LocalizationManager.Instance == null) return;

        // 1. 현재 언어에 맞는 폰트로 교체
        if (LocalizationManager.Instance.CurrentFont != null)
        {
            textMeshPro.font = LocalizationManager.Instance.CurrentFont;
        }

        // 2. 텍스트 내용 변경
        textMeshPro.text = LocalizationManager.Instance.GetLocalizedString(localizationTextId);
    }

    private void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageUpdated -= RefreshUI;
    }
}