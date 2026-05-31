using UnityEngine;
using System.Collections.Generic;
using System;

public abstract class ChamberBase : MonoBehaviour
{
    [Header("Chamber Setup")]
    public int chamberNumber;
    //public ChamberDoor exitDoor;

    [Header("Clear Conditions")]
    [Tooltip("이 리스트의 조건이 전부 충족되면 출구가 열림")]
    //public List<PuzzleCondition> clearConditions = new List<PuzzleCondition>();

    // 중복 클리어 방지
    private bool isCleared = false;

    public event Action OnCleared;

    // ── 초기화 ───────────────────────────────────────────────────

    void Start()
    {
        // 자식 클래스별 초기화 먼저
        OnChamberInit();

        // 조건이 없으면 즉시 문 열림 (빈 챔버 테스트용)
        //if (clearConditions.Count == 0)
        //{
        //    Debug.LogWarning($"Chamber {chamberNumber}: clearConditions가 비어있습니다. 출구가 즉시 열립니다.");
        //    TriggerClear();
        //}
    }

    // 자식 챔버 스크립트에서 반드시 구현
    protected abstract void OnChamberInit();

    // ── 클리어 조건 체크 ─────────────────────────────────────────

    void Update()
    {
        if (!isCleared)
            CheckClearConditions();
    }

    void CheckClearConditions()
    {
        //foreach (var condition in clearConditions)
        //{
        //    if (!condition.IsSatisfied)
        //        return; // 하나라도 미충족이면 즉시 리턴
        //}

        // 전부 충족
        TriggerClear();
    }

    void TriggerClear()
    {
        if (isCleared) return;
        isCleared = true;

        Debug.Log($"Chamber {chamberNumber} cleared!");

        // 출구 문 열기
        //if (exitDoor != null)
        //    exitDoor.Unlock();
        //else
        //    Debug.LogWarning($"Chamber {chamberNumber}: exitDoor가 연결되지 않았습니다.");

        OnCleared?.Invoke();
    }
}