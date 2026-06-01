using UnityEngine;
using System.Collections;

public class SpawnPipe : MonoBehaviour
{
    [Header("Settings")]
    public float spawnDelay = 0.3f;
    public float spawnOutSpeed = 2f; // 흘러나올 때 아래 방향 속도

    // [수정 1] PlayerParticleSystem 캐싱
    // 기존: SpawnRoutine 안에서 FindWithTag("SoulCore") → GetComponentInChildren
    //       → PlayerParticle이 SoulCore 하위가 아니라 별도 오브젝트라 pps = null
    //       → 아무것도 안 일어남
    // 수정: FindObjectOfType으로 직접 찾기
    private PlayerParticleSystem cachedPPS;
    private GameObject cachedPlayer;

    void Start()
    {
        // [수정 1 연계] 씬에서 직접 찾아 캐싱
        cachedPPS = FindObjectOfType<PlayerParticleSystem>();
        cachedPlayer = GameObject.FindWithTag("SoulCore");

        if (cachedPPS == null)
            Debug.LogWarning("SpawnPipe: PlayerParticleSystem을 찾지 못했습니다.");
        if (cachedPlayer == null)
            Debug.LogWarning("SpawnPipe: SoulCore 태그 오브젝트를 찾지 못했습니다.");

        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(spawnDelay);

        // [수정 2] null 체크 분리
        // 기존: player가 null이면 pps도 null인데 한 줄로 묶여있어 어느 게 null인지 불명확
        // 수정: 각각 따로 체크하고 어느 쪽이 없는지 로그로 구분
        if (cachedPlayer == null)
        {
            Debug.LogWarning("SpawnPipe: Player를 찾지 못해 스폰을 생략합니다.");
            yield break;
        }

        if (cachedPPS == null)
        {
            Debug.LogWarning("SpawnPipe: PPS를 찾지 못해 입자 스폰을 생략합니다.");
            yield break;
        }

        // 1. SoulCore를 파이프 출구 위치로 이동
        cachedPlayer.transform.position = transform.position;

        // 2. 입자 소환 (SetSoul 내부의 warmup이 자연스럽게 흘러나오는 효과를 만들어 줌)
        int count = cachedPPS.particles.Count > 0 ? cachedPPS.particles.Count : 200;
        cachedPPS.SetSoul(count);

        // 3. SetSoul 반영 대기 (particles 리스트가 채워지는 한 프레임 대기)
        yield return new WaitForSeconds(0.1f);

        // [수정 3] 속도 부여 방향 수정
        // 기존: Vector3.down * 2f → 파이프가 위에서 아래로 뚫려있을 때만 자연스러움
        // 수정: 파이프 오브젝트의 forward 방향으로 튀어나오게 변경
        //       파이프를 회전시켜 방향을 조절할 수 있음
        //       수직 파이프면 transform.forward가 아래를 향하도록 회전해두면 됨
        foreach (var p in cachedPPS.particles)
            p.velocity += transform.forward * spawnOutSpeed;

        Debug.Log($"SpawnPipe: {cachedPPS.particles.Count}개 입자 소환 완료");
    }
}