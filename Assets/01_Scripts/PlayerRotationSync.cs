using UnityEngine;

public class PlayerRotationSync : MonoBehaviour
{
    // Main Camera의 Transform을 연결하세요
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float rotationSpeed = 15f;

    void Start()
    {
        // 수동 연결 안 했을 경우 메인 카메라를 자동으로 찾음
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
    }

    // 시네마신 업데이트 이후에 실행되도록 LateUpdate 사용
    void LateUpdate()
    {
        // 1. 카메라의 현재 Y축 회전값(수평 회전)만 가져옵니다.
        float targetYRotation = cameraTransform.eulerAngles.y;

        // 2. 목표 회전값을 쿼터니언으로 만듭니다.
        Quaternion targetRotation = Quaternion.Euler(0, targetYRotation, 0);

        // 3. 부드럽게 회전시킵니다. (즉시 돌리고 싶다면 바로 적용 가능)
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * rotationSpeed
        );
    }
}
