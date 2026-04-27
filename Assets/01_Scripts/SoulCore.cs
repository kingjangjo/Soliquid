using UnityEngine;
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

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void FixedUpdate()
    {
        //지면 체크
        //isGrounded = Physics.CheckSphere(
        //    transform.position - Vector3.up * 0.5f,
        //    groundCheckRadius,
        //    groundLayer
        //);
        isGrounded = Physics.CheckSphere(
            transform.position,
            groundCheckRadius,
            groundLayer
        );

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
        if(currentForm == PlayerForm.Humanoid)
        {
            playerAnim.SetFloat("Speed", currentVelocity.magnitude / maxSpeed);
            playerAnim.SetFloat("MoveX", currentVelocity.x / maxSpeed);
            playerAnim.SetFloat("MoveY", currentVelocity.z / maxSpeed);
        }
        // 점프
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
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