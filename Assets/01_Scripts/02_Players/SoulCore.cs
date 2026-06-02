using UnityEngine;
using UnityEngine.InputSystem; // 1. 뉴 인풋 시스템 네임스페이스 추가

public class SoulCore : MonoBehaviour
{
    public Rigidbody rb;

    [Header("Movement Settings")]
    public float maxSpeed = 10f;
    public float acceleration = 10f;
    public float deceleration = 15f;
    public float jumpForce = 8f;

    [Header("Ground Check")]
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.3f;

    [Header("New Input System 설정")]
    [SerializeField] private InputActionReference moveActionRef; // 이동 액션 에셋 연결용
    [SerializeField] private InputActionReference jumpActionRef; // 점프 액션 에셋 연결용

    private bool isGrounded;
    public Animator playerAnim;
    public PlayerForm currentForm;
    private bool jumpRequest;

    // 뉴 인풋 시스템에서 읽어온 이동 입력 값을 저장할 변수
    private Vector2 movementInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    private void OnEnable()
    {
        // 2. 스크립트가 켜질 때 인풋 액션들을 활성화하고, 점프 이벤트(Performed)를 구독합니다.
        if (moveActionRef != null) moveActionRef.action.Enable();

        if (jumpActionRef != null)
        {
            jumpActionRef.action.Enable();
            // 버튼이 눌린 '그 순간' 호출될 콜백 함수 등록
            jumpActionRef.action.performed += OnJumpInput;
        }
    }

    private void OnDisable()
    {
        // 3. 스크립트가 꺼질 때 이벤트 구독 해제 및 액션 비활성화 (메모리 누수 방지)
        if (moveActionRef != null) moveActionRef.action.Disable();

        if (jumpActionRef != null)
        {
            jumpActionRef.action.Disable();
            jumpActionRef.action.performed -= OnJumpInput;
        }
    }

    // 4. Update 대신 이벤트 기반으로 점프 입력을 받습니다.
    private void OnJumpInput(InputAction.CallbackContext context)
    {
        // FixedUpdate에서 안전하게 물리 처리를 할 수 있도록 플래그만 켭니다.
        jumpRequest = true;
    }

    private void Update()
    {
        // 5. 이동 입력은 실시간으로 프레임마다 Vector2(X, Y) 형태로 읽어옵니다.
        if (moveActionRef != null)
        {
            movementInput = moveActionRef.action.ReadValue<Vector2>();
        }
    }

    void FixedUpdate()
    {
        // 지면 체크
        isGrounded = Physics.CheckSphere(
            transform.position - Vector3.up * 0.15f,
            groundCheckRadius,
            groundLayer
        );

        // 이동 (기존 GetMovementInput 코드가 수정된 movementInput을 기반으로 돌도록 연동)
        Vector3 input = GetMovementInput();

        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 targetVelocity = input * maxSpeed;

        float lerpSpeed = (input.sqrMagnitude > 0) ? acceleration : deceleration;

        float newX = Mathf.Lerp(currentVelocity.x, targetVelocity.x, lerpSpeed * Time.fixedDeltaTime);
        float newZ = Mathf.Lerp(currentVelocity.z, targetVelocity.z, lerpSpeed * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector3(newX, currentVelocity.y, newZ);

        // 속도 제한
        Vector3 vel = rb.linearVelocity;
        Vector3 horizontal = new Vector3(vel.x, 0, vel.z);
        if (horizontal.magnitude > maxSpeed)
        {
            horizontal = horizontal.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(horizontal.x, vel.y, horizontal.z);
        }

        // 애니메이션 파라미터 제어
        if (currentForm == PlayerForm.Humanoid)
        {
            Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);

            float moveX = localVelocity.x / maxSpeed;
            float moveY = localVelocity.z / maxSpeed;

            playerAnim.SetFloat("MoveX", Mathf.Clamp(moveX, -1f, 1f));
            playerAnim.SetFloat("MoveY", Mathf.Clamp(moveY, -1f, 1f));

            Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            playerAnim.SetFloat("Speed", horizontalVel.magnitude / maxSpeed);
        }

        // 점프 물리 처리
        if (jumpRequest && isGrounded && rb.linearVelocity.y <= 0.01f)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        // 소비된 점프 입력은 초기화
        jumpRequest = false;
    }

    Vector3 GetMovementInput()
    {
        // 6. 구형 Input.GetAxisRaw 대신 수집된 movementInput 값을 사용해 카메라 기준 방향을 계산합니다.
        float h = movementInput.x; // A, D 또는 Left, Right Arrow 값 (-1 ~ 1)
        float v = movementInput.y; // W, S 또는 Up, Down Arrow 값 (-1 ~ 1)

        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;
        camForward.y = 0;
        camRight.y = 0;

        return (camForward.normalized * v + camRight.normalized * h).normalized;
    }
}