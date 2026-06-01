using UnityEngine;
using System.Collections.Generic;
using System;

public abstract class ChamberBase : MonoBehaviour
{
    [Header("Chamber Setup")]
    public int chamberNumber;

    // [수정 1] exitDoor 주석 해제
    // 기존: 주석처리되어 있어서 ChamberDoor.Unlock()이 절대 호출되지 않았음
    // 수정: 주석 해제. Inspector에서 챔버 프리팹의 Exit_Door 오브젝트를 여기에 연결하면 됨
    public ChamberDoor exitDoor;

    // [수정 2] clearConditions 리스트 활성화
    // 기존: 전부 주석처리 → CheckClearConditions()가 매 프레임 TriggerClear() 호출
    //       → 게임 시작하자마자 문이 열려버리는 버그
    // 수정: 리스트 활성화. PuzzleCondition이 아직 없으므로 일단 빈 리스트로 둠
    //       빈 리스트일 때는 즉시 클리어하지 않고, 자식 클래스가 직접 TriggerClear()를 호출
    [Header("Clear Conditions")]
    [Tooltip("이 리스트에 PuzzleCondition을 추가하면 전부 충족 시 출구가 열림. " +
             "비어있으면 자식 클래스에서 TriggerClear()를 직접 호출해야 함.")]
    //public List<PuzzleCondition> clearConditions = new List<PuzzleCondition>();

    private bool isCleared = false;

    public event Action OnCleared;

    // ── 초기화 ───────────────────────────────────────────────────

    void Start()
    {
        OnChamberInit();

        // [수정 3] 빈 리스트 즉시 클리어 제거
        // 기존: 리스트가 비어있으면 Start()에서 바로 TriggerClear() 호출
        //       → 테스트용이었지만 실제 게임에선 항상 발동
        // 수정: 빈 리스트면 아무것도 안 함. 자식 클래스가 명시적으로 TriggerClear() 호출해야 함
    }

    protected abstract void OnChamberInit();

    // ── 클리어 조건 체크 ─────────────────────────────────────────

    void Update()
    {
        if (!isCleared)
            CheckClearConditions();
    }

    void CheckClearConditions()
    {
        // [수정 4] 조건 체크 로직 복구
        // 기존: foreach가 주석처리 → 조건 없이 매 프레임 TriggerClear() 호출
        // 수정: 리스트가 비어있으면 체크 안 함 (자식 클래스가 직접 TriggerClear 호출)
        //       리스트에 조건이 있으면 전부 충족 시에만 TriggerClear 호출


        //if (clearConditions.Count == 0) return;

        //foreach (var condition in clearConditions)
        //{
        //    if (!condition.IsSatisfied)
        //        return;
        //}


        TriggerClear();
    }

    // [수정 5] TriggerClear를 public으로 변경
    // 기존: private → 자식 클래스에서 직접 호출 불가능
    // 수정: public → 자식 챔버 스크립트나 외부에서 퍼즐 완료 시 직접 호출 가능
    //       ex) Chamber_01.cs에서 버튼 눌렸을 때 TriggerClear() 호출
    public void TriggerClear()
    {
        if (isCleared) return;
        isCleared = true;

        Debug.Log($"Chamber {chamberNumber} cleared!");

        // [수정 1 연계] exitDoor 연결됐으면 잠금 해제
        if (exitDoor != null)
            exitDoor.Unlock();
        else
            Debug.LogWarning($"Chamber {chamberNumber}: exitDoor가 연결되지 않았습니다.");

        OnCleared?.Invoke();
    }

    // ── 디버그 ───────────────────────────────────────────────────

    // [추가] 테스트용: Inspector 우클릭으로 강제 클리어
    [ContextMenu("Force Clear (Debug)")]
    public void ForceClear()
    {
        isCleared = false; // 리셋 후 재호출
        TriggerClear();
    }
}