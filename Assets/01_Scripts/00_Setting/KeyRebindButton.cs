using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class KeyRebindButton : MonoBehaviour
{
    [Header("Input Action 설정")]
    [SerializeField] private InputActionReference actionReference; // 재바인딩할 액션 (예: Move, Jump 등)
    [SerializeField] private int bindingIndex = 0; // 복합 입력(Composite)일 경우 몇 번째 인덱스인지 (기본은 0)

    [Header("UI 연결")]
    [SerializeField] private TMP_Text displayActionText; // "Jump" 같은 액션 이름 텍스트
    [SerializeField] private TMP_Text displayKeyText;    // "Space" 같은 현재 키 이름 텍스트
    [SerializeField] private Button rebindButton;       // 키 세팅 버튼
    [SerializeField] private GameObject overlayPanel;   // "아무 키나 누르세요..." 팝업 UI 패널

    private InputActionRebindingExtensions.RebindingOperation rebindingOperation;

    private void Start()
    {
        if (actionReference == null) return;

        // UI 초기화
        displayActionText.text = actionReference.action.name;
        UpdateBindingDisplay();

        // 버튼 클릭 이벤트 연결
        rebindButton.onClick.AddListener(StartRebinding);
    }

    // 1. 재바인딩 시작
    public void StartRebinding()
    {
        // 입력 액션이 켜져 있으면 재바인딩이 안 되므로 잠시 끕니다.
        actionReference.action.Disable();

        // "아무 키나 누르세요..." 팝업 활성화
        if (overlayPanel != null) overlayPanel.SetActive(true);

        // 이전 오퍼레이션이 남아있다면 메모리 해제
        rebindingOperation?.Cancel();

        // 2. 입력 대기 시동
        rebindingOperation = actionReference.action.PerformInteractiveRebinding(bindingIndex)
            // ESC 키를 누르면 취소되도록 설정
            .WithCancelingThrough("<Keyboard>/escape")
            // 마우스 움직임(Delta)이나 포인터 위치는 바인딩에서 제외 (실수로 마우스 흔들었다가 바인딩되는 것 방지)
            .WithControlsExcluding("<Mouse>/delta")
            .WithControlsExcluding("<Mouse>/position")
            // 키 입력이 완료되거나 취소되었을 때 실행할 콜백 함수 지정
            .OnComplete(operation => FinishRebinding())
            .OnCancel(operation => FinishRebinding());

        rebindingOperation.Start();
    }

    // 3. 재바인딩 완료 처리
    private void FinishRebinding()
    {
        // 대기 팝업 닫기
        if (overlayPanel != null) overlayPanel.SetActive(false);

        // 오퍼레이션 메모리 해제 및 액션 다시 켜기
        rebindingOperation.Dispose();
        rebindingOperation = null;
        actionReference.action.Enable();

        // UI 글자 업데이트
        UpdateBindingDisplay();

        // [중요] 변경된 키 세팅 저장 (유니티 인풋 시스템의 세팅을 문자열로 저장)
        string rebinds = actionReference.action.actionMap.asset.SaveBindingOverridesAsJson();
        //PlayerPrefs.SetString("CharacterControls", rebinds);
        //PlayerPrefs.Save();
        SettingManager.Instance.UpdateKeyBindings(rebinds);
    }

    // 현재 바인딩된 키 이름으로 텍스트 업데이트
    private void UpdateBindingDisplay()
    {
        displayKeyText.text = InputControlPath.ToHumanReadableString(
            actionReference.action.bindings[bindingIndex].effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice
        );
    }

    private void OnDestroy()
    {
        rebindingOperation?.Dispose();
    }
}