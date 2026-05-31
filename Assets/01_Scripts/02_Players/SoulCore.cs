using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class SoulCore : MonoBehaviour
{
    public Rigidbody rb;
    
    [Header("Movement Settings")]
    public float maxSpeed = 10f;
    public float acceleration = 10f; // 얼마나 빨리 최고 속도에 도달할지
    public float deceleration = 15f;
    public float jumpForce = 8f;

    [Header("Ground Check")]
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.3f;

    private bool isGrounded;
    public Animator playerAnim;
    public PlayerForm currentForm;
    private bool jumpRequest;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            jumpRequest = true;
        }
    }
    void FixedUpdate()
    {
        //지면 체크
        isGrounded = Physics.CheckSphere(
            transform.position - Vector3.up * 0.15f,
            groundCheckRadius,
            groundLayer
        );
        //isGrounded = Physics.CheckSphere(
        //    transform.position,
        //    groundCheckRadius,
        //    groundLayer
        //);

        // 이동
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
        if (currentForm == PlayerForm.Humanoid)
        {
            // 1. 월드 좌표 기준의 속도를 캐릭터의 로컬 좌표계로 변환합니다.
            // 이렇게 하면 캐릭터가 어느 방향을 보든 '캐릭터 기준 앞/옆' 속도를 얻을 수 있습니다.
            Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);

            // 2. 최대 속도로 나누어 -1 ~ 1 사이의 값으로 정규화합니다.
            float moveX = localVelocity.x / maxSpeed;
            float moveY = localVelocity.z / maxSpeed;

            // 3. 파라미터 전달 (Mathf.Clamp로 범위를 안전하게 제한)
            playerAnim.SetFloat("MoveX", Mathf.Clamp(moveX, -1f, 1f));
            playerAnim.SetFloat("MoveY", Mathf.Clamp(moveY, -1f, 1f));

            // 전체 속도감 (애니메이션 재생 속도 조절용)
            Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            playerAnim.SetFloat("Speed", horizontalVel.magnitude / maxSpeed);
        }
        // 점프
        if (jumpRequest && isGrounded && rb.linearVelocity.y <= 0.01f)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
        jumpRequest = false;
    }

    Vector3 GetMovementInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;
        camForward.y = 0;
        camRight.y = 0;

        return (camForward.normalized * v + camRight.normalized * h).normalized;
    }
}