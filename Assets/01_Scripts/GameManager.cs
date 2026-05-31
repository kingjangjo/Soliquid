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

    // 챔버 시작/종료 시 다른 스크립트가 구독할 수 있는 이벤트
    public static event Action<int> OnChamberStart;
    public static event Action<int> OnChamberEnd;

    void Awake()
    {
        // 싱글턴: 씬이 바뀌어도 유지
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
        StartChamber(currentChamber);
    }

    // ── 챔버 흐름 ────────────────────────────────────────────────

    public void StartChamber(int n)
    {
        currentChamber = n;
        state = GameState.Playing;
        OnChamberStart?.Invoke(n);
    }

    // ChamberDoor가 플레이어 통과를 감지하면 이걸 호출
    public void OnChamberCleared(int n)
    {
        if (state == GameState.Transitioning) return; // 중복 방지

        state = GameState.Transitioning;
        OnChamberEnd?.Invoke(n);
        SaveProgress();

        // ChamberManager에게 다음 챔버 로드 요청
        if (ChamberManager.Instance != null)
            ChamberManager.Instance.LoadChamber(n + 1);
    }

    // ── 입력 잠금 ────────────────────────────────────────────────

    public void LockInput(float seconds)
    {
        inputLocked = true;
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

    // 디버그/테스트용: Inspector에서 저장 초기화
    [ContextMenu("Reset Save")]
    public void ResetSave()
    {
        PlayerPrefs.DeleteKey("currentChamber");
        currentChamber = 1;
        Debug.Log("Save reset.");
    }
}