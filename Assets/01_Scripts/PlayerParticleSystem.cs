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

    private Vector3 gravity = new Vector3(0, -9.8f, 0);
    private SpatialHash spatialHash;
    private List<int> neighborCache = new List<int>();

    // 충돌 체크용 캐시 (GC 방지)
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
            if(cohesionStrength > 0)
            {
                originCohesionStrength = cohesionStrength;
                cohesionStrength = 0f;
            }
            else
            {   
                cohesionStrength = originCohesionStrength;
            }
            
        }
        float dt = Mathf.Min(Time.deltaTime, 0.016f);
        int subSteps = 4;
        float subDt = dt / subSteps;

        //spatialHash.Rebuild(particles);

        for (int i = 0; i < subSteps; i++)
        {
            SimulationStep(subDt, i == 0);
        }
        //SimulationStep(dt); 
    }
    void OnEnable() => RenderPipelineManager.beginCameraRendering += OnBeginCamera;
    void OnDisable() => RenderPipelineManager.beginCameraRendering -= OnBeginCamera;

    void OnBeginCamera(ScriptableRenderContext context, Camera camera)
    {
        if (camera.name != "LiquidCamera") return;

        RenderParticles(camera);
    }

    //void SimulationStep(float dt, bool heavyCompute)
    //{
    //    // 1) 중력 + 응집력
    //    foreach (var p in particles)
    //    {
    //        p.velocity += gravity * dt;
    //        ApplyCohesion(p, dt);
    //    }

    //    // 3) 위치 예측
    //    foreach (var p in particles)
    //    {
    //        p.prevPosition = p.position;
    //        p.position += p.velocity * dt;
    //    }

    //    if (heavyCompute)
    //    {
    //        // 2) 점성
    //        ApplyViscosity(dt);
    //        // 4) DDR
    //        DoubleDensityRelaxation(dt);
    //    }



    //    spatialHash.Rebuild(particles);


    //    // 5) 환경 충돌 (입자별)
    //    SolveEnvironmentCollisions_Optimized();

    //    // 6) 속도 갱신
    //    foreach (var p in particles)
    //    {
    //        p.velocity = (p.position - p.prevPosition) / dt;
    //        p.velocity *= 0.98f; // 감쇠
    //    }
    //}
    void SimulationStep(float dt, bool heavyCompute)
    {
        // 1) 중력 + 응집력
        foreach (var p in particles)
        {
            p.velocity += gravity * dt;
            ApplyCohesion(p, dt, true);
        }

        // 2) 위치 예측
        foreach (var p in particles)
        {
            p.prevPosition = p.position;
            p.position += p.velocity * dt;
        }

        // 3) 무거운 연산 (점성, DDR) - heavyCompute일 때만 실행
        if (heavyCompute)
        {
            spatialHash.Rebuild(particles); // 리빌드를 이 안으로 이동
            ApplyViscosity(dt);
            DoubleDensityRelaxation(dt);
        }

        // 4) 환경 충돌 (터널링 방지를 위해 매번 실행)
        SolveEnvironmentCollisions_Optimized();

        // 5) 속도 갱신
        foreach (var p in particles)
        {
            p.velocity = (p.position - p.prevPosition) / dt;
            p.velocity *= 0.98f;
        }
    }

    // 핵심: 입자별 환경 충돌

    void SolveEnvironmentCollisions_Optimized()
    {
        // ─── 1단계: Core 주변 콜라이더만 수집 ───
        //int hitCount = Physics.OverlapSphereNonAlloc(
        //    core.transform.position,
        //    cohesionRadius + 1f,  // 입자가 퍼질 수 있는 범위
        //    colliderCache,
        //    environmentLayer
        //);

        //// 콜라이더가 없으면 바닥만 처리
        //if (hitCount == 0)
        //{
        //    foreach (var p in particles)
        //    {
        //        if (p.position.y < groundY + particleRadius)
        //        {
        //            p.position.y = groundY + particleRadius;
        //            if (p.velocity.y < 0) p.velocity.y *= -0.3f;
        //        }
        //    }
        //    return;
        //}

        //// ─── 2단계: 각 입자에 대해 근처 콜라이더만 체크 ───
        //foreach (var p in particles)
        //{
        //    // 바닥
        //    if (p.position.y < groundY + particleRadius)
        //    {
        //        p.position.y = groundY + particleRadius;
        //        if (p.velocity.y < 0) p.velocity.y *= -0.3f;
        //    }

        //    // 수집된 콜라이더만 검사 (Physics 호출 없음)
        //    for (int i = 0; i < hitCount; i++)
        //    {
        //        Collider col = colliderCache[i];

        //        // 빠른 거리 체크 (Bounds 기반)
        //        if (!col.bounds.Contains(p.position) &&
        //            (col.bounds.ClosestPoint(p.position) - p.position).sqrMagnitude > 0.25f)
        //            continue;

        //        if (col.CompareTag("LiquidPassable"))
        //            continue;

        //        if (IsLowObstacle(col, p.position))
        //            continue;

        //        ResolveCollision(p, col);
        //    }
        //}
        foreach (var p in particles)
        {
            // 바닥 처리
            if (p.position.y < groundY + particleRadius)
            {
                p.position.y = groundY + particleRadius;
                if (p.velocity.y < 0) p.velocity.y *= -0.3f;
            }

            // 주변 콜라이더 수집 (핵 기준이 아니라 입자 기준으로 소량만 체크)
            // 성능이 걱정된다면 5~10프레임마다 한 번씩만 갱신하는 캐시를 사용하세요.
            int hitCount = Physics.OverlapSphereNonAlloc(
                p.position,
                particleRadius * 2f, // 입자 주변 아주 좁은 범위
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
        // 장애물 높이가 0.5 이하면 액체가 넘어감
        float obstacleTop = col.bounds.max.y;
        float obstacleHeight = col.bounds.size.y;

        // 입자가 장애물 위쪽에 있고, 장애물이 낮으면 통과
        if (obstacleHeight < 0.5f && particlePos.y > obstacleTop - 0.1f)
            return true;

        return false;
    }

    void ResolveCollision(Particle p, Collider col)
    {
        Vector3 closestPoint = col.ClosestPoint(p.position);
        Vector3 diff = p.position - closestPoint;
        float dist = diff.magnitude;

        if (dist < particleRadius)
        {
            Vector3 normal;

            // ─── 1단계: 반발 방향(Normal) 결정 ───
            if (dist < 0.0001f)
            {
                // 완전히 겹침: 콜라이더 중심에서 입자 위치로 향하는 방향을 법선으로 사용
                // 만약 중심과도 겹쳤다면 최후의 보루로 위쪽(Vector3.up) 사용
                Vector3 centerToParticle = p.position - col.bounds.center;
                normal = centerToParticle.sqrMagnitude > 0.0001f ? centerToParticle.normalized : Vector3.up;

                // 위치 보정: 콜라이더 표면 밖으로 강제 이동
                p.position = closestPoint + normal * particleRadius;
            }
            else
            {
                // 표면 근처: 기존 방식대로 법선 계산
                normal = diff / dist;
                float penetration = particleRadius - dist;
                p.position += normal * penetration;
            }

            // ─── 2단계: 속도 반사 및 충격 가하기 ───
            float velAlongNormal = Vector3.Dot(p.velocity, normal);

            if (velAlongNormal < 0)
            {
                // 1. 기존 속도 반사 (입사각/반사각)
                // 1.3f는 반발 계수(Bounciness)입니다. 1.0보다 크면 더 강하게 튕깁니다.
                p.velocity -= normal * velAlongNormal * 1.5f;

                // 2. 추가적인 척력 (선택 사항)
                // 속도가 너무 느려 벽에 끼는 것을 방지하기 위해 최소 반발 속도를 부여합니다.
                p.velocity += normal * 2.0f;
            }

            // 3. 마찰력 적용 (벽을 타고 흐르는 느낌)
            // 법선 방향이 아닌 속도(접선 속도)를 줄여서 끈적하게 만듭니다.
            Vector3 tangentVelocity = p.velocity - (normal * Vector3.Dot(p.velocity, normal));
            p.velocity -= tangentVelocity * 0.2f; // 0.2f는 끈적임 정도
        }
    }

    //응집력: 핵을 향해 당김
    void ApplyCohesion(Particle p, float dt, bool canScan)
    {
        Vector3 toCore = core.transform.position - p.position;
        float dist = toCore.magnitude;

        if (dist < 0.01f) return;

        Vector3 dir = toCore / dist;

        if(Physics.Raycast(p.position, dir, dist, environmentLayer))
        {
            Vector3 newBypass = GetBypassDirection(p, dir, dist);
            if(newBypass != Vector3.zero)
            {
                p.velocity += newBypass * pullForce * 1.2f * dt;
                p.velocity += dir * pullForce * 0.1f * dt;
                return;
            }
        }
        //float pullFactor = Mathf.Pow(Mathf.Clamp01(1.0f - (dist / (cohesionRadius * 2.0f))), 2);
        //p.velocity += dir * pullForce * pullFactor * dt;
        if (dist > cohesionRadius)
        {
            // 반경 밖: 강하게 당김
            float overflow = dist - cohesionRadius;
            p.velocity += dir * overflow * cohesionStrength * dt;
        }
        else
        {
            // 반경 안: 약하게 당김
            float factor = dist / cohesionRadius;
            p.velocity += dir * factor * cohesionStrength * 0.3f * dt;
        }
    }
    Vector3 GetBypassDirection(Particle p, Vector3 toCoreDir, float distToCore)
    {
        float step = 0.5f; // 스캔 간격 (장애물 두께에 따라 조절)
        int maxSteps = 10; // 최대 5m까지 스캔

        for (int i = 1; i <= maxSteps; i++)
        {
            float offset = i * step;

            // 위쪽 검사: 해당 높이로 올라갔을 때 코어가 보이는가?
            Vector3 upPos = p.position + Vector3.up * offset;
            if (!Physics.CheckSphere(upPos, particleRadius, environmentLayer)) // 머리 위가 비었나?
            {
                if (!Physics.Raycast(upPos, toCoreDir, distToCore, environmentLayer)) // 거기서 코어가 보이나?
                    return Vector3.up;
            }

            // 아래쪽 검사
            Vector3 downPos = p.position + Vector3.down * offset;
            if (!Physics.CheckSphere(downPos, particleRadius, environmentLayer))
            {
                if (!Physics.Raycast(downPos, toCoreDir, distToCore, environmentLayer))
                    return Vector3.down;
            }
        }
        return Vector3.zero;
    }
    public int SetHumanoid()
    {
        int removeCount = 0;
        for(int i = particles.Count - 1; i >= 0; i--)
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
    public void SetSoul(int count)
    {
        //for(int i = 0; i < count; i++)
        //{
        //    Vector3 pos = core.transform.position + Random.insideUnitSphere * 1.5f;
        //    Particle p = new Particle(pos);

        //    // 2. 초기 속도를 0으로 확실히 고정 (생성 직후 튀는 현상 방지)
        //    p.velocity = Vector3.zero;
        //    p.prevPosition = pos;

        //    particles.Add(p);
        //}
        cohesionStrength = defaultCohesionStrength;
        for (int i = 0; i < count; i++)
        {
            // Random.onUnitSphere의 y값을 양수로 절댓값 처리하여 위쪽으로만 퍼지게 함
            Vector3 randomDir = Random.insideUnitSphere;
            if (randomDir.y < 0) randomDir.y *= -0.5f; // 바닥 쪽이면 위로 올림

            Vector3 pos = core.transform.position + randomDir * 1.5f;

            // 최소 생성 높이 보정 (예: 바닥 위 0.5f 지점)
            if (pos.y < core.transform.position.y) pos.y = core.transform.position.y + 0.1f;

            Particle p = new Particle(pos);
            p.velocity = Vector3.zero;
            p.prevPosition = pos;
            particles.Add(p);
        }
    }
    //void ApplyCohesion(Particle p, float dt)
    //{
    //    Vector3 toCore = core.transform.position - p.position;
    //    float dist = toCore.magnitude;

    //    // 1. 너무 멀면 낙오 (기존 유지)
    //    if (dist > cohesionRadius * 2.0f) return;

    //    // 2. [추가] 너무 가까우면 당기지 않음 (핵의 물리 이동 방해 방지)
    //    if (dist < 0.5f) return;

    //    Vector3 dir = toCore / dist;

    //    // 3. 거리 제곱 감쇠 (기존 유지)
    //    float pullFactor = Mathf.Clamp01(1.0f - (dist / (cohesionRadius * 2.0f)));
    //    pullFactor = Mathf.Pow(pullFactor, 2);

    //    p.velocity += dir * pullForce * pullFactor * dt;
    //}
    // 점성 (기존 코드)
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

    // DDR
    void DoubleDensityRelaxation(float dt)
    {
        float dt2 = dt * dt;

        // 밀도 계산
        for (int i = 0; i < particles.Count; i++)
        {
            Particle pi = particles[i];
            pi.density = 0;
            pi.nearDensity = 0;

            float Pi = stiffness * (pi.density - restDensity);
            float Pi_near = nearStiffness * pi.nearDensity;

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

        // 압력 변위
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
            // 1. 생성 범위를 조금 더 넓히거나, 특정 지점에서 떨어지게 설정
            Vector3 pos = core.transform.position + Random.insideUnitSphere * 1.5f;
            Particle p = new Particle(pos);

            // 2. 초기 속도를 0으로 확실히 고정 (생성 직후 튀는 현상 방지)
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
    }
    void RenderParticles(Camera camera)
    {
        int count = particles.Count;
        if (count == 0) return;

        // 1. 성능 최적화: 배열 재할당 방지
        if (instanceMatrices == null || instanceMatrices.Length < count)
        {
            // 여유 있게 10% 정도 더 크게 할당하면 파티클 개수가 변할 때 재할당 횟수가 줄어듭니다.
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

        // 2. RenderParams 설정 (크래시 방지 핵심)
        RenderParams rp = new RenderParams(particleMaterial);
        rp.layer = LayerMask.NameToLayer("Liquid"); // 10번 레이어 할당
        rp.camera = camera;

        // 중요: 카메라가 이 메쉬를 그릴지 판단할 영역을 설정합니다. 
        // 실제 파티클이 위치하는 전체 범위를 넣거나, 테스트를 위해 아주 크게 잡으세요.
        rp.worldBounds = new Bounds(Vector3.zero, Vector3.one * 1000f);

        // 3. 인스턴싱 그리기
        Graphics.RenderMeshInstanced(rp, particleMesh, 0, instanceMatrices, count);
    }
}   