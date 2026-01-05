using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 5f;        // 움직이는 속도
    public float jumpForce = 10f;       // 점프 속도

    [Header("슬라이딩 설정")]
    public float slideForce = 8f;      // 슬라이딩 속도
    public float slideDuration = 0.5f;  // 슬라이딩 지속 시간
    private bool isSliding = false;     // 슬라이딩 했는지

    private Rigidbody2D rb;
    private bool isGrounded = false;    // 땅을 밟고 있는지
    private float minX, maxX;           // 이동 제한 변수

    // 인풋 액션을 담아둘 변수

    // 초기화
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        // 내 캐릭터 아니면 물리 연산 끄기 (위치 동기화만 받기 위해)
        // 그렇지 않으면 내 화면에서 상대방이 중력 때문에 뚝뚝 떨어지는 현상 발생 가능
        if (!IsOwner)
        {
            
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // 내 캐릭터일 때 InputManager 이벤트 연결
        else
        {
            InputManager.OnJump += HandleJump;
            InputManager.OnSpike += HandleSpike;
        }

        if (transform.position.x < 0)
        {
            minX = -8.5f;
            maxX = -1.05f;
        }
        else
        {
            minX = 0.55f;
            maxX = 8f;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            InputManager.OnJump -= HandleJump;
            InputManager.OnSpike -= HandleSpike;
        }
    }

    private void Update()
    {
        // 내 캐릭터 아니면 조종 X
        if (!IsOwner) return;

        // 슬라이딩 중이 아닐 때만 일반 이동 가능
        if (!isSliding)
        {
            Move();
        }

        // 위치 강제 고정
        float clampedX = Mathf.Clamp(transform.position.x, minX, maxX);
        transform.position = new Vector2(clampedX, transform.position.y);

    }

    // 이동 처리
    void Move()
    {
        float moveInput = InputManager.MoveInput;

        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    // 점프 처리
    private void HandleJump()
    {
        if (isGrounded && !isSliding)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isGrounded = false;
        }
    }

    // 스파이크 처리
    private void HandleSpike()
    {
        // 현재 누르고 있는 방향키 값 (-1, 0, 1)
        float currentMoveInput = InputManager.MoveInput;
        
        // 슬라이딩
        if (isGrounded && !isSliding)
        {
            if (currentMoveInput != 0)
            {
                StartCoroutine(Sliding(currentMoveInput));
            }
        }

        // 스파이크
        else
        {
            Spike();
        }
    }

    // 슬라이딩
    private IEnumerator Sliding(float direction)
    {
        isSliding = true;

        // 입력된 방향으로 힘을 가함
        float slideDir = Mathf.Sign(direction);

        // Y축은 0f로 만들어서 바닥에 붙게 함
        rb.linearVelocity = new Vector2(slideDir * slideForce, 0f);

        yield return new WaitForSeconds(slideDuration);

        isSliding = false;
        // 슬라이딩 끝나면 멈춤 (관성 제거)
        rb.linearVelocity = Vector2.zero;
    }

    // 스파이크
    private void Spike()
    {
        Debug.Log("스파이크");
        // 추후 기능 추가
    }

    // 바닥에 닿았을 때 체크
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            isGrounded = true;
            isSliding = false;
        }
    }
}
