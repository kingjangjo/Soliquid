using UnityEngine;
using System;

public enum GameState { Playing, Transitioning, Paused }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("State")]
    public int currentChamber = 1;
    public GameState state = GameState.Playing;
    public bool inputLocked = false;

    public static event Action<int> OnChamberStart;
    public static event Action<int> OnChamberEnd;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        LoadProgress();

        // [수정 1] Start()에서 직접 LoadChamber를 호출하도록 변경
        // 기존: StartChamber(currentChamber) → 이벤트만 발사하고 챔버를 실제로 로드하지 않았음
        // 수정: ChamberManager.LoadChamber()를 호출해서 챔버 프리팹을 실제로 씬에 올림
        if (ChamberManager.Instance != null)
            ChamberManager.Instance.LoadChamber(currentChamber);
        else
            StartChamber(currentChamber); // ChamberManager 없을 때 fallback
    }

    // ── 챔버 흐름 ────────────────────────────────────────────────

    public void StartChamber(int n)
    {
        currentChamber = n;
        state = GameState.Playing;
        OnChamberStart?.Invoke(n);
    }

    // ChamberPipe에서 전환 완료 시 호출
    // [수정 2] 파라미터 이름을 n → clearedChamberNumber로 변경해서 의미를 명확히 함
    // "방금 클리어한 챔버 번호"를 받아서 그 다음(+1) 챔버를 로드함
    public void OnChamberCleared(int clearedChamberNumber)
    {
        if (state == GameState.Transitioning) return;

        state = GameState.Transitioning;
        OnChamberEnd?.Invoke(clearedChamberNumber);
        SaveProgress();

        if (ChamberManager.Instance != null)
            ChamberManager.Instance.LoadChamber(clearedChamberNumber + 1);
        else
            Debug.LogWarning("ChamberManager가 없습니다.");
    }

    // ── 입력 잠금 ────────────────────────────────────────────────

    public void LockInput(float seconds)
    {
        inputLocked = true;
        // [수정 3] 중복 Invoke 방지: 기존 예약된 UnlockInput 취소 후 재예약
        // 기존: LockInput을 여러 번 호출하면 UnlockInput이 여러 번 예약되어
        //       생각보다 일찍 잠금이 풀리는 버그 있었음
        CancelInvoke(nameof(UnlockInput));
        Invoke(nameof(UnlockInput), seconds);
    }

    void UnlockInput() => inputLocked = false;

    // ── 저장 / 불러오기 ──────────────────────────────────────────

    public void SaveProgress()
    {
        PlayerPrefs.SetInt("currentChamber", currentChamber);
        PlayerPrefs.Save();
    }

    public void LoadProgress()
    {
        currentChamber = PlayerPrefs.GetInt("currentChamber", 1);
    }

    [ContextMenu("Reset Save")]
    public void ResetSave()
    {
        PlayerPrefs.DeleteKey("currentChamber");
        currentChamber = 1;
        Debug.Log("Save reset.");
    }
}