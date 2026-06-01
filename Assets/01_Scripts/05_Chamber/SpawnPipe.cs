using UnityEngine;
using System.Collections;

public class SpawnPipe : MonoBehaviour
{
    [Header("Settings")]
    public float spawnDelay = 0.3f;         // 페이드 인 후 약간 대기
    public float cohesionRampDuration = 1f; // 응집력 서서히 정상화되는 시간

    void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(spawnDelay);

        GameObject player = GameObject.FindWithTag("SoulCore");
        PlayerParticleSystem pps = player != null
            ? player.GetComponentInChildren<PlayerParticleSystem>()
            : null;

        if (pps == null) yield break;

        // 1. 플레이어를 파이프 출구 위치로 이동
        player.transform.position = transform.position;

        // 2. 입자를 파이프 출구에서 소환
        //    (SetSoul이 내부적으로 warmup 처리하므로 자연스럽게 흘러나옴)
        pps.SetSoul(pps.particles.Count > 0 ? pps.particles.Count : 200);

        // 3. 초기엔 아래 방향으로 속도 부여 (흘러나오는 느낌)
        yield return new WaitForSeconds(0.1f); // SetSoul 반영 대기
        foreach (var p in pps.particles)
        {
            p.velocity += Vector3.down * 2f;
        }

        Debug.Log("SpawnPipe: 액체 흘러나오기 완료");
    }
}