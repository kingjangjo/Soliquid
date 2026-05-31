using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerParticleSystem : MonoBehaviour
{
    public SoulCore core;
    public List<Particle> particles = new List<Particle>();

    [Header("Particle")]
    public int particleCount = 200;

    [Header("Simulation")]
    public float radius = 1.0f;
    public float restDensity = 6f;
    public float stiffness = 60f;
    public float nearStiffness = 60f;
    public float viscosity = 0.5f;

    [Header("Cohesion - 핵을 향한 응집력")]
    public float cohesionRadius = 2.5f;
    public float cohesionStrength = 40f;
    private float defaultCohesionStrength;

    [Header("Collision")]
    public LayerMask environmentLayer;
    public float particleRadius = 0.05f;

    [Header("Boundary")]
    public float groundY = 0f;

    public float pullForce = 0f;

    [Header("Rendering")]
    public Mesh particleMesh;
    public Material particleMaterial;

    public Matrix4x4[] instanceMatrices;

    // ─── [수정 A] Warmup 설정 ─────────────────────────────────────────
    // SetSoul() 이후 몇 프레임 동안 DDR/충돌 보정을 부드럽게 감쇠시킨다.
    // 액체 시뮬레이션 본체는 전혀 변경하지 않는다.
    [Header("Spawn Warmup")]
    [Tooltip("SetSoul 직후 안정화 프레임 수. 이 기간 동안 힘이 서서히 켜진다.")]
    public int spawnWarmupFrames = 20;
    private int warmupFramesLeft = 0;

    // warmup 진행도 0~1. 0=방금 스폰, 1=완전 정상
    private float WarmupT => warmupFramesLeft <= 0
        ? 1f
        : 1f - (float)warmupFramesLeft / spawnWarmupFrames;
    // ──────────────────────────────────────────────────────────────────

    private Vector3 gravity = new Vector3(0, -9.8f, 0);
    private SpatialHash spatialHash;
    private List<int> neighborCache = new List<int>();

    private Collider[] colliderCache = new Collider[8];
    public float originCohesionStrength;


    void Start()
    {
        defaultCohesionStrength = cohesionStrength;
        spatialHash = new SpatialHash(radius);
        SpawnParticles();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (cohesionStrength > 0)
            {
                originCohesionStrength = cohesionStrength;
                cohesionStrength = 0f;
            }
            else
            {
                cohesionStrength = originCohesionStrength;
            }
        }

        // ─── [수정 A-1] warmup 카운트다운 ───
        if (warmupFramesLeft > 0)
            warmupFramesLeft--;
        // ────────────────────────────────────

        float dt = Mathf.Min(Time.deltaTime, 0.016f);
        int subSteps = 4;
        float subDt = dt / subSteps;

        for (int i = 0; i < subSteps; i++)
        {
            SimulationStep(subDt, i == 0);
        }
    }

    void OnEnable() => RenderPipelineManager.beginCameraRendering += OnBeginCamera;
    void OnDisable() => RenderPipelineManager.beginCameraRendering -= OnBeginCamera;

    void OnBeginCamera(ScriptableRenderContext context, Camera camera)
    {
        if (camera.name != "LiquidCamera") return;
        RenderParticles(camera);
    }

    void SimulationStep(float dt, bool heavyCompute)
    {
        // 1) 중력 + 응집력
        // ─── [수정 B] warmup 중에는 cohesion을 서서히 켠다 ───
        // WarmupT가 0이면 cohesion 0, 1이면 평상시 값
        // ApplyCohesion 자체는 수정하지 않고 호출 시 강도를 스케일링한다.
        float cohesionScale = warmupFramesLeft > 0
            ? Mathf.SmoothStep(0f, 1f, WarmupT)
            : 1f;

        float savedCohesion = cohesionStrength;
        cohesionStrength *= cohesionScale; // 임시 스케일

        foreach (var p in particles)
        {
            p.velocity += gravity * dt;
            ApplyCohesion(p, dt, true);
        }

        cohesionStrength = savedCohesion; // 원상복구
        // ──────────────────────────────────────────────────

        // 2) 위치 예측
        foreach (var p in particles)
        {
            p.prevPosition = p.position;
            p.position += p.velocity * dt;
        }

        // 3) 무거운 연산 (점성, DDR) - 변경 없음
        if (heavyCompute)
        {
            spatialHash.Rebuild(particles);
            ApplyViscosity(dt);
            DoubleDensityRelaxation(dt);
        }

        // 4) 환경 충돌
        SolveEnvironmentCollisions_Optimized();

        // 5) 속도 갱신
        foreach (var p in particles)
        {
            p.velocity = (p.position - p.prevPosition) / dt;
            p.velocity *= 0.98f;

            // ─── [수정 C] 전역 속도 상한선 ───────────────────────────
            // warmup 중일수록 더 엄격하게 제한한다.
            // 평상시에도 너무 빠른 입자가 벽을 뚫는 것을 방지한다.
            float maxSpeed = warmupFramesLeft > 0
                ? Mathf.Lerp(2f, 15f, WarmupT)  // 처음엔 2, 나중엔 15
                : 20f;                            // 평상시 상한
            if (p.velocity.sqrMagnitude > maxSpeed * maxSpeed)
                p.velocity = p.velocity.normalized * maxSpeed;
            // ──────────────────────────────────────────────────────────
        }
    }

    // ─── [수정 없음] 환경 충돌 수집 로직은 그대로 ───
    void SolveEnvironmentCollisions_Optimized()
    {
        foreach (var p in particles)
        {
            if (p.position.y < groundY + particleRadius)
            {
                p.position.y = groundY + particleRadius;
                if (p.velocity.y < 0) p.velocity.y *= -0.3f;
            }

            int hitCount = Physics.OverlapSphereNonAlloc(
                p.position,
                particleRadius * 2f,
                colliderCache,
                environmentLayer
            );

            for (int i = 0; i < hitCount; i++)
            {
                Collider col = colliderCache[i];
                if (col.CompareTag("LiquidPassable")) continue;
                if (IsLowObstacle(col, p.position)) continue;

                ResolveCollision(p, col);
            }
        }
    }

    bool IsLowObstacle(Collider col, Vector3 particlePos)
    {
        float obstacleTop = col.bounds.max.y;
        float obstacleHeight = col.bounds.size.y;
        if (obstacleHeight < 0.5f && particlePos.y > obstacleTop - 0.1f)
            return true;
        return false;
    }

    void ResolveCollision(Particle p, Collider col)
    {
        Vector3 closestPoint = col.ClosestPoint(p.position);
        Vector3 diff = p.position - closestPoint;
        float dist = diff.magnitude;

        if (dist >= particleRadius) return;

        // ─── 1단계: normal 결정 (변경 없음) ────────────────────────
        Vector3 normal;
        if (dist < 0.0001f)
        {
            Vector3 centerToParticle = p.position - col.bounds.center;
            normal = centerToParticle.sqrMagnitude > 0.0001f
                ? centerToParticle.normalized
                : Vector3.up;
            p.position = closestPoint + normal * particleRadius;
        }
        else
        {
            normal = diff / dist;
            float penetration = particleRadius - dist;

            // ─── [수정 D-1] penetration 보정량 제한 ─────────────────
            // 한 프레임에 반경의 80%까지만 밀어낸다.
            // warmup 중에는 더 조심스럽게 60%로 제한.
            float maxCorrection = particleRadius * (warmupFramesLeft > 0 ? 0.6f : 0.8f);
            float correction = Mathf.Min(penetration, maxCorrection);
            p.position += normal * correction;
            // ────────────────────────────────────────────────────────
        }

        // ─── 2단계: 속도 반사 ───────────────────────────────────────
        float velAlongNormal = Vector3.Dot(p.velocity, normal);
        if (velAlongNormal < 0)
        {
            // ─── [수정 D-2] restitution 과다 제거 ────────────────────
            // 기존: velAlongNormal * 1.5f + normal * 2.0f
            //   → 에너지가 150% 반환 + 최소 척력 추가로 폭발 유발
            // 수정: restitution 0.1 (에너지 10%만 반환), 추가 척력 제거
            float restitution = warmupFramesLeft > 0 ? 0.0f : 0.1f;
            p.velocity -= normal * (velAlongNormal * (1f + restitution));
            // ────────────────────────────────────────────────────────
        }

        // 3단계: 마찰 (변경 없음)
        Vector3 tangentVelocity = p.velocity - (normal * Vector3.Dot(p.velocity, normal));
        p.velocity -= tangentVelocity * 0.2f;
    }

    // ─── [수정 없음] 응집력 로직 자체는 그대로 ───
    // 강도 스케일링은 SimulationStep에서 cohesionStrength를 임시 변경하는 방식으로 처리
    void ApplyCohesion(Particle p, float dt, bool canScan)
    {
        Vector3 toCore = core.transform.position - p.position;
        float dist = toCore.magnitude;

        if (dist < 0.01f) return;

        Vector3 dir = toCore / dist;

        if (dist > cohesionRadius)
        {
            float overflow = dist - cohesionRadius;
            p.velocity += dir * overflow * cohesionStrength * dt;
        }
        else
        {
            float factor = dist / cohesionRadius;
            p.velocity += dir * factor * cohesionStrength * 0.3f * dt;
        }
    }

    // ─── [수정 E] SetHumanoid: warmup 리셋 불필요 (cohesion=0이므로) ───
    public int SetHumanoid()
    {
        int removeCount = 0;
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            var p = particles[i];
            Vector3 toCore = core.transform.position - p.position;
            float dist = toCore.magnitude;
            if (dist < 2.5f)
            {
                particles.Remove(p);
                removeCount++;
            }
        }
        cohesionStrength = 0f;
        return removeCount;
    }

    // ─── [수정 F] SetSoul: 핵심 수정 지점 ─────────────────────────────
    [Header("Soul Spawn")]
    public float maxSearchRadius = 3.0f; // Inspector에서 조절

    public void SetSoul(int count)
    {
        cohesionStrength = defaultCohesionStrength;
        warmupFramesLeft = spawnWarmupFrames;

        float minDist = particleRadius * 2.2f;

        // ★ 여기서 한 번 계산해서 모든 단계에서 공유
        float minHeightAboveGround = particleRadius * 3f;
        Vector3 spawnOrigin = core.transform.position;
        spawnOrigin.y = Mathf.Max(spawnOrigin.y, spawnOrigin.y + minHeightAboveGround);

        int spawned = 0;
        int maxAttempts = count * 20;

        for (int attempt = 0; attempt < maxAttempts && spawned < count; attempt++)
        {
            Vector3 dir = Random.insideUnitSphere;
            if (dir.sqrMagnitude < 0.001f) continue;
            dir.Normalize();

            float dist = Random.Range(minDist, Mathf.Min(1.0f, maxSearchRadius));
            Vector3 pos = spawnOrigin + dir * dist;  // ★

            if (pos.y < groundY + particleRadius) continue;
            if (Physics.CheckSphere(pos, particleRadius * 1.2f, environmentLayer)) continue;
            if (IsTooClose(pos, minDist)) continue;

            AddParticle(pos);
            spawned++;
        }

        if (spawned < count)
            spawned += SpawnByLayerScan(count - spawned, minDist, spawnOrigin);  // ★ 전달

        for (int i = spawned; i < count; i++)
        {
            Vector3 pos = spawnOrigin + Vector3.up * (minDist * (i - spawned + 1));  // ★
            pos.y = Mathf.Max(pos.y, groundY + particleRadius);
            AddParticle(pos);
        }
    }

    int SpawnByLayerScan(int needed, float minDist, Vector3 spawnOrigin)
    {
        int sampleCount = 720;
        Vector3[] dirs = FibonacciSphere(sampleCount);

        int[] indices = new int[sampleCount];
        for (int i = 0; i < sampleCount; i++) indices[i] = i;

        bool[] blocked = new bool[sampleCount];

        float layerStep = minDist * 0.8f;
        int layerCount = Mathf.CeilToInt(maxSearchRadius / layerStep);

        // ★ 핵심: 스폰 기준점을 바닥에서 최소 높이만큼 올림
        // Core가 바닥에 완전히 붙어있어도 기준점은 항상 공중에 위치
        //float minHeightAboveGround = particleRadius * 3f;
        //Vector3 spawnOrigin = core.transform.position;
        //spawnOrigin.y = Mathf.Max(spawnOrigin.y, groundY + minHeightAboveGround);

        int added = 0;

        for (int li = 0; li < layerCount && added < needed; li++)
        {
            float r = layerStep * (li + 1);
            ShuffleArray(indices);

            for (int di = 0; di < sampleCount && added < needed; di++)
            {
                int idx = indices[di];
                if (blocked[idx]) continue;

                // ★ core.transform.position 대신 spawnOrigin 사용
                Vector3 pos = spawnOrigin + dirs[idx] * r;

                if (pos.y < groundY + particleRadius) continue;

                float wallCheckRadius = particleRadius * 3f;

                if (Physics.CheckSphere(pos, wallCheckRadius, environmentLayer))
                {
                    blocked[idx] = true;
                    continue;
                }

                if (IsTooClose(pos, minDist)) continue;

                AddParticle(pos);
                added++;
            }
        }

        return added;
    }

    void ShuffleArray(int[] arr)
    {
        for (int i = arr.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
    }

    void ShuffleArray(Vector3[] arr)
    {
        for (int i = arr.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
    }

    // ── 피보나치 구면 분포 ─────────────────────────────────────────────
    // 균일하게 퍼진 방향 벡터를 생성 (랜덤보다 고르게 분포됨)
    Vector3[] FibonacciSphere(int n)
    {
        var dirs = new Vector3[n];
        float golden = Mathf.PI * (3f - Mathf.Sqrt(5f)); // 황금각

        for (int i = 0; i < n; i++)
        {
            float y = 1f - (i / (float)(n - 1)) * 2f; // +1 ~ -1
            float r = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
            float theta = golden * i;
            dirs[i] = new Vector3(r * Mathf.Cos(theta), y, r * Mathf.Sin(theta));
        }
        return dirs;
    }

    // ── 헬퍼 ──────────────────────────────────────────────────────────
    bool IsTooClose(Vector3 pos, float minDist)
    {
        float sqrMin = minDist * minDist;
        foreach (var p in particles)
            if ((p.position - pos).sqrMagnitude < sqrMin) return true;
        return false;
    }

    void AddParticle(Vector3 pos)
    {
        var p = new Particle(pos);
        p.velocity = Vector3.zero;
        p.prevPosition = pos;
        particles.Add(p);
    }
    // ──────────────────────────────────────────────────────────────────

    Vector3 GetBypassDirection(Particle p, Vector3 toCoreDir, float distToCore)
    {
        float step = 0.5f;
        int maxSteps = 10;

        for (int i = 1; i <= maxSteps; i++)
        {
            float offset = i * step;

            Vector3 upPos = p.position + Vector3.up * offset;
            if (!Physics.CheckSphere(upPos, particleRadius, environmentLayer))
            {
                if (!Physics.Raycast(upPos, toCoreDir, distToCore, environmentLayer))
                    return Vector3.up;
            }

            Vector3 downPos = p.position + Vector3.down * offset;
            if (!Physics.CheckSphere(downPos, particleRadius, environmentLayer))
            {
                if (!Physics.Raycast(downPos, toCoreDir, distToCore, environmentLayer))
                    return Vector3.down;
            }
        }
        return Vector3.zero;
    }

    // ─── [수정 없음] 아래부터는 원본과 동일 ───────────────────────────

    void ApplyViscosity(float dt)
    {
        for (int i = 0; i < particles.Count; i++)
        {
            Particle pi = particles[i];
            spatialHash.GetNeighborIndices(pi.position, neighborCache);

            foreach (int j in neighborCache)
            {
                if (j <= i) continue;

                Particle pj = particles[j];
                Vector3 rij = pj.position - pi.position;
                float dist = rij.magnitude;

                if (dist >= radius || dist < 0.0001f) continue;

                float q = 1f - dist / radius;
                Vector3 n = rij / dist;
                float u = Vector3.Dot(pi.velocity - pj.velocity, n);

                if (u > 0)
                {
                    Vector3 impulse = dt * q * viscosity * u * n;
                    pi.velocity -= impulse * 0.5f;
                    pj.velocity += impulse * 0.5f;
                }
            }
        }
    }

    void DoubleDensityRelaxation(float dt)
    {
        float dt2 = dt * dt;

        for (int i = 0; i < particles.Count; i++)
        {
            Particle pi = particles[i];
            pi.density = 0;
            pi.nearDensity = 0;

            spatialHash.GetNeighborIndices(pi.position, neighborCache);

            foreach (int j in neighborCache)
            {
                if (j == i) continue;

                Particle pj = particles[j];
                float dist = (pj.position - pi.position).magnitude;

                if (dist < radius)
                {
                    float q = 1f - dist / radius;
                    pi.density += q * q;
                    pi.nearDensity += q * q * q;
                }
            }
        }

        for (int i = 0; i < particles.Count; i++)
        {
            Particle pi = particles[i];
            float Pi = stiffness * (pi.density - restDensity);
            float Pi_near = nearStiffness * pi.nearDensity;

            spatialHash.GetNeighborIndices(pi.position, neighborCache);

            foreach (int j in neighborCache)
            {
                if (j <= i) continue;

                Particle pj = particles[j];
                Vector3 rij = pj.position - pi.position;
                float dist = rij.magnitude;

                if (dist < radius && dist > 0.0001f)
                {
                    float q = 1f - dist / radius;
                    Vector3 n = rij / dist;

                    float Pj = stiffness * (pj.density - restDensity);
                    float Pj_near = nearStiffness * pj.nearDensity;

                    float D = dt2 * (
                        (Pi + Pj) * 0.5f * q +
                        (Pi_near + Pj_near) * 0.5f * q * q
                    );

                    Vector3 disp = D * 0.5f * n;
                    pi.position -= disp;
                    pj.position += disp;
                }
            }
        }
    }

    void SpawnParticles()
    {
        for (int i = 0; i < particleCount; i++)
        {
            Vector3 pos = core.transform.position + Random.insideUnitSphere * 1.5f;
            Particle p = new Particle(pos);
            p.velocity = Vector3.zero;
            p.prevPosition = pos;
            particles.Add(p);
        }
    }

    void OnDrawGizmos()
    {
        if (particles == null) return;

        Gizmos.color = new Color(0, 0.5f, 1f, 0.7f);
        foreach (var p in particles)
            Gizmos.DrawSphere(p.position, particleRadius);

        if (core != null)
        {
            Gizmos.color = new Color(1, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(core.transform.position, cohesionRadius);
        }

        // warmup 진행도 시각화 (디버그용)
        if (warmupFramesLeft > 0)
        {
            Gizmos.color = new Color(1, 0.5f, 0, 0.4f);
            Gizmos.DrawWireSphere(core.transform.position, cohesionRadius * WarmupT);
        }
    }

    void RenderParticles(Camera camera)
    {
        int count = particles.Count;
        if (count == 0) return;

        if (instanceMatrices == null || instanceMatrices.Length < count)
        {
            instanceMatrices = new Matrix4x4[Mathf.CeilToInt(count * 1.1f)];
        }

        for (int i = 0; i < count; i++)
        {
            instanceMatrices[i] = Matrix4x4.TRS(
                particles[i].position,
                Quaternion.identity,
                Vector3.one * (particleRadius * 2f)
            );
        }

        RenderParams rp = new RenderParams(particleMaterial);
        rp.layer = LayerMask.NameToLayer("Liquid");
        rp.camera = camera;
        rp.worldBounds = new Bounds(Vector3.zero, Vector3.one * 1000f);

        Graphics.RenderMeshInstanced(rp, particleMesh, 0, instanceMatrices, count);
    }
}