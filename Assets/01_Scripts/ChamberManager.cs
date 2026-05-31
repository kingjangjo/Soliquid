using UnityEngine;
using System.Collections;

public class ChamberManager : MonoBehaviour
{
    public static ChamberManager Instance { get; private set; }

    [Header("Chamber Prefabs")]
    [Tooltip("인덱스 0 = 챔버 1, 인덱스 1 = 챔버 2 ...")]
    public GameObject[] chamberPrefabs;

    [Header("References")]
    public GameObject player; // SoulCore 오브젝트 연결

    // 현재 활성화된 챔버 인스턴스
    private GameObject currentChamberInstance;
    private Transform currentSpawnPoint;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ── 챔버 로드 ────────────────────────────────────────────────

    public void LoadChamber(int n)
    {
        StartCoroutine(LoadChamberRoutine(n));
    }

    IEnumerator LoadChamberRoutine(int n)
    {
        // 범위 체크
        int index = n - 1; // 챔버 번호는 1-based, 배열은 0-based
        if (index < 0 || index >= chamberPrefabs.Length)
        {
            Debug.LogWarning($"Chamber {n} 프리팹이 없습니다. (배열 크기: {chamberPrefabs.Length})");
            yield break;
        }

        // 1. 입력 잠금 (전환 연출 동안)
        GameManager.Instance.LockInput(0.5f);

        // 2. 기존 챔버 제거
        if (currentChamberInstance != null)
            Destroy(currentChamberInstance);

        yield return null; // 한 프레임 대기 (Destroy 반영)

        // 3. 새 챔버 생성
        currentChamberInstance = Instantiate(chamberPrefabs[index]);

        // 4. 스폰 위치 찾기 (챔버 프리팹 안에 "SpawnPoint" 태그 오브젝트 필요)
        GameObject spawnObj = GameObject.FindWithTag("SpawnPoint");
        if (spawnObj != null)
            currentSpawnPoint = spawnObj.transform;
        else
            Debug.LogWarning($"Chamber {n} 에 SpawnPoint 태그 오브젝트가 없습니다.");

        // 5. 플레이어 이동
        RespawnPlayer();

        // 6. GameManager에 챔버 시작 알림
        GameManager.Instance.StartChamber(n);
    }

    // ── 플레이어 리스폰 ──────────────────────────────────────────

    public void RespawnPlayer()
    {
        if (player == null || currentSpawnPoint == null) return;

        player.transform.position = currentSpawnPoint.position;

        // PlayerParticleSystem이 있으면 입자도 리셋
        var pps = player.GetComponentInChildren<PlayerParticleSystem>();
        if (pps != null)
            pps.SetSoul(pps.particles.Count > 0 ? pps.particles.Count : 200);
    }
}