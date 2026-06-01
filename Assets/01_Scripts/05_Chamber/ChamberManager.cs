using UnityEngine;
using System.Collections;

public class ChamberManager : MonoBehaviour
{
    public static ChamberManager Instance { get; private set; }

    [Header("Chamber Prefabs")]
    [Tooltip("인덱스 0 = 챔버 1, 인덱스 1 = 챔버 2 ...")]
    public GameObject[] chamberPrefabs;

    [Header("References")]
    public GameObject player; // Player 오브젝트 연결 (SoulCore의 부모)

    private GameObject currentChamberInstance;
    private Transform currentSpawnPoint;

    // [수정 1] PlayerParticleSystem을 미리 캐싱
    // 기존: RespawnPlayer() 호출마다 GetComponentInChildren으로 찾았음
    //       → PlayerParticle이 Player 하위가 아니라 별도 오브젝트라 항상 null 반환
    // 수정: Start()에서 씬 전체에서 한 번만 찾아 캐싱 (FindObjectOfType)
    private PlayerParticleSystem cachedPPS;
    private PlayerFormController cachedFormController;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // [수정 1] 씬 전체에서 PlayerParticleSystem, PlayerFormController 캐싱
        cachedPPS = FindObjectOfType<PlayerParticleSystem>();
        cachedFormController = FindObjectOfType<PlayerFormController>();

        if (cachedPPS == null)
            Debug.LogWarning("ChamberManager: PlayerParticleSystem을 찾지 못했습니다.");
        if (cachedFormController == null)
            Debug.LogWarning("ChamberManager: PlayerFormController를 찾지 못했습니다.");
    }

    // ── 챔버 로드 ────────────────────────────────────────────────

    public void LoadChamber(int n)
    {
        StartCoroutine(LoadChamberRoutine(n));
    }

    IEnumerator LoadChamberRoutine(int n)
    {
        int index = n - 1;
        if (index < 0 || index >= chamberPrefabs.Length)
        {
            Debug.LogWarning($"Chamber {n} 프리팹이 없습니다. (배열 크기: {chamberPrefabs.Length})");

            // [수정 2] 마지막 챔버 이후 처리 — 경고만 내고 끝내지 않고 로그 추가
            if (index >= chamberPrefabs.Length)
                Debug.Log("모든 챔버를 클리어했습니다!");
            yield break;
        }

        // 1. 입력 잠금
        GameManager.Instance.LockInput(0.5f);

        // 2. 기존 챔버 제거
        if (currentChamberInstance != null)
            Destroy(currentChamberInstance);

        yield return null; // Destroy 반영 대기

        // 3. 새 챔버 Instantiate
        currentChamberInstance = Instantiate(chamberPrefabs[index]);

        // 4. SpawnPoint 찾기
        // [수정 3] FindWithTag는 씬 전체에서 찾기 때문에 새 챔버의 SpawnPoint가
        //          이전 챔버 잔재나 다른 오브젝트와 혼동될 수 있음
        //          → currentChamberInstance 하위에서만 찾도록 변경
        Transform spawnObj = currentChamberInstance.transform.Find("SpawnPoint");
        if (spawnObj != null)
        {
            currentSpawnPoint = spawnObj;
        }
        else
        {
            // 직접 Find 실패 시 태그로 fallback
            GameObject tagObj = GameObject.FindWithTag("SpawnPoint");
            if (tagObj != null)
                currentSpawnPoint = tagObj.transform;
            else
                Debug.LogWarning($"Chamber {n}: SpawnPoint를 찾지 못했습니다.");
        }

        // 5. 플레이어 리스폰
        RespawnPlayer();

        // 6. GameManager에 챔버 시작 알림
        GameManager.Instance.StartChamber(n);
    }

    // ── 플레이어 리스폰 ──────────────────────────────────────────

    public void RespawnPlayer()
    {
        if (player == null)
        {
            Debug.LogWarning("ChamberManager: player가 연결되지 않았습니다.");
            return;
        }

        if (currentSpawnPoint == null)
        {
            Debug.LogWarning("ChamberManager: SpawnPoint가 없어 리스폰 위치를 모릅니다.");
            return;
        }

        // [수정 4] player 오브젝트를 SpawnPoint로 이동
        player.transform.position = currentSpawnPoint.position;

        // [수정 1 연계] 캐싱해둔 pps 사용 (GetComponentInChildren 제거)
        if (cachedPPS != null)
        {
            int count = cachedPPS.particles.Count > 0 ? cachedPPS.particles.Count : 200;
            cachedPPS.SetSoul(count);
        }

        // [수정 5] 폼 상태도 Soul로 초기화 (챔버 진입 시 항상 액체 상태로 시작)
        // 고체 상태로 이전 챔버를 클리어했을 때 다음 챔버에서도 고체로 시작하는 문제 방지
        if (cachedFormController != null)
        {
            // 현재 Humanoid 상태면 Soul로 전환
            if (cachedFormController.currentForm == PlayerForm.Humanoid)
            {
                // FormController의 FormChange를 직접 호출하면 토글이라 위험
                // 대신 Soul 상태일 때만 SetSoul이 의미있으므로 PPS만 리셋
                // 완전한 폼 전환은 StateController 완성 후 처리 예정
            }
        }
    }
}