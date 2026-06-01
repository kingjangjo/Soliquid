using UnityEngine;
using System.Collections;

public class ChamberPipe : MonoBehaviour
{
    [Header("Settings")]
    // [수정 1] nextChamber → currentChamber로 이름 변경
    // 기존: nextChamber라고 이름 붙여놓고 OnChamberCleared(nextChamber - 1) 호출
    //       → Inspector에서 "2" 입력 시 OnChamberCleared(1) 전달
    //       → GameManager는 1+1=2번 챔버 로드 → 맞긴 하지만 헷갈리는 구조
    // 수정: "이 파이프가 속한 챔버 번호"를 입력하도록 변경
    //       ex) 챔버 1의 파이프면 currentChamber = 1
    //       OnChamberCleared(1) → GameManager가 2번 챔버 로드
    [Tooltip("이 파이프가 속한 챔버 번호. 챔버 1이면 1, 챔버 2면 2를 입력.")]
    public int currentChamber;
    public KeyCode enterKey = KeyCode.F;
    public float transitionDuration = 2.0f;

    [Header("Prompt")]
    public GameObject promptUI;

    private bool playerNearby = false;
    private bool isTransitioning = false;

    // [수정 2] PlayerParticleSystem 캐싱
    // 기존: PipeTransition() 코루틴 안에서 매번 FindWithTag → GetComponentInChildren
    //       → PlayerParticle이 Player 하위가 아니어서 pps가 항상 null
    // 수정: Start()에서 FindObjectOfType으로 한 번만 찾아 캐싱
    private PlayerParticleSystem cachedPPS;

    void Start()
    {
        cachedPPS = FindObjectOfType<PlayerParticleSystem>();
        if (cachedPPS == null)
            Debug.LogWarning("ChamberPipe: PlayerParticleSystem을 찾지 못했습니다.");
    }

    // ── 파이프 앞 진입/이탈 감지 ─────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("SoulCore")) return;

        playerNearby = true;

        if (promptUI != null)
            promptUI.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("SoulCore")) return;

        playerNearby = false;

        if (promptUI != null)
            promptUI.SetActive(false);
    }

    // ── 키 입력 감지 ─────────────────────────────────────────────

    void Update()
    {
        if (isTransitioning) return;
        if (!playerNearby) return;
        if (GameManager.Instance.inputLocked) return;

        if (Input.GetKeyDown(enterKey))
            StartCoroutine(PipeTransition());
    }

    // ── 파이프 전환 연출 ─────────────────────────────────────────

    IEnumerator PipeTransition()
    {
        isTransitioning = true;
        GameManager.Instance.LockInput(transitionDuration);

        if (promptUI != null)
            promptUI.SetActive(false);

        // [수정 2 연계] 캐싱된 pps 사용
        PlayerParticleSystem pps = cachedPPS;

        // 1. 액체 상태 강제
        if (pps != null)
            pps.SetSoul(pps.particles.Count > 0 ? pps.particles.Count : 200);

        // 2. 입자 수축 연출
        float originalCohesion = 0f;
        if (pps != null)
        {
            originalCohesion = pps.cohesionStrength;
            pps.cohesionStrength = originalCohesion * 5f;
        }

        // 3. 수축 대기
        yield return new WaitForSeconds(transitionDuration * 0.4f);

        // 4. 페이드 아웃 (HUDController 완성 후 주석 해제)
        // HUDController.Instance?.FadeOut(0.3f);
        yield return new WaitForSeconds(transitionDuration * 0.3f);

        // 5. 챔버 교체
        // [수정 1 연계] currentChamber를 그대로 전달
        // GameManager.OnChamberCleared(1) → LoadChamber(2)
        GameManager.Instance.OnChamberCleared(currentChamber);

        // 6. 챔버 로드 대기
        yield return new WaitForSeconds(transitionDuration * 0.2f);

        // 7. cohesionStrength 복구
        // [수정 3] 복구 시점에 pps가 여전히 유효한지 확인 후 복구
        // 챔버 교체 후 pps 오브젝트가 Destroy될 수도 있기 때문
        if (pps != null)
            pps.cohesionStrength = originalCohesion;

        // 8. 페이드 인 (HUDController 완성 후 주석 해제)
        // HUDController.Instance?.FadeIn(0.3f);

        isTransitioning = false;
    }
}