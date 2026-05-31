using UnityEngine;
using System.Collections;

public class ChamberPipe : MonoBehaviour
{
    [Header("Settings")]
    public int nextChamber;
    public KeyCode enterKey = KeyCode.F;
    public float transitionDuration = 2.0f;

    [Header("Prompt")]
    [Tooltip("'F: 파이프로 들어가기' 같은 UI — 나중에 TutorialPrompt로 교체 예정")]
    public GameObject promptUI;

    // 플레이어가 파이프 앞에 있는지
    private bool playerNearby = false;
    private bool isTransitioning = false;

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

        // 프롬프트 숨기기
        if (promptUI != null)
            promptUI.SetActive(false);

        // PlayerParticleSystem 가져오기
        GameObject player = GameObject.FindWithTag("SoulCore");
        PlayerParticleSystem pps = player != null
            ? player.GetComponentInChildren<PlayerParticleSystem>()
            : null;

        // 1. 액체 상태 강제
        if (pps != null)
            pps.SetSoul(pps.particles.Count > 0 ? pps.particles.Count : 200);

        // 2. 입자 수축 연출 (cohesionStrength 일시적으로 강하게)
        float originalCohesion = 0f;
        if (pps != null)
        {
            originalCohesion = pps.cohesionStrength;
            pps.cohesionStrength = originalCohesion * 5f;
        }

        // 3. 수축되는 동안 대기
        yield return new WaitForSeconds(transitionDuration * 0.4f);

        // 4. 페이드 아웃
        // HUDController.Instance?.FadeOut(0.3f);  ← HUDController 만들면 주석 해제
        yield return new WaitForSeconds(transitionDuration * 0.3f);

        // 5. 챔버 교체
        GameManager.Instance.OnChamberCleared(nextChamber - 1);

        // 6. 잠깐 대기 (챔버 로드 시간)
        yield return new WaitForSeconds(transitionDuration * 0.2f);

        // 7. cohesionStrength 복구
        if (pps != null)
            pps.cohesionStrength = originalCohesion;

        // 8. 다음 챔버 시작 파이프에서 흘러나오는 연출
        //    → 다음 챔버의 SpawnPipe.cs가 담당 (아래 참고)

        // 9. 페이드 인
        // HUDController.Instance?.FadeIn(0.3f);  ← HUDController 만들면 주석 해제

        isTransitioning = false;
    }
}