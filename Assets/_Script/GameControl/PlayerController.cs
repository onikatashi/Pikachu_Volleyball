using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using static UnityEditor.PlayerSettings;

public class PlayerController : NetworkBehaviour
{
    private Animator anim;
    private Rigidbody2D rb;
    private ClientNetworkTransform networkTransform;

    [Header("그림자 설정")]
    public Transform shadowTransform;   // 그림자 오브젝트
    public float groundY;               // 그림자가 있어야할 Y좌표

    [Header("이동 설정")]
    public float moveSpeed = 5f;        // 움직이는 속도
    public float jumpForce = 10f;       // 점프 속도

    [Header("슬라이딩 설정")]
    public float slideForce = 8f;       // 슬라이딩 속도
    public float slideDuration = 0.4f;  // 슬라이딩 지속 시간
    private bool isSliding = false;     // 슬라이딩 했는지

    [Header("스파이크 설정")]
    public float spikeDuration = 0.2f;  // 공격 판정 지속 시간
    public float spikeCooldown = 0.5f;  // 다시 쓰기까지 걸리는 시간
    private float lastSpikeTime = -999f;    // 마지막으로 쓴 시간

    // 이게 디폴트 값임
    public NetworkVariable<bool> isSpike = new NetworkVariable<bool>(false, 
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<Vector2> inputDirection = new NetworkVariable<Vector2>(Vector2.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private Vector2 startPos;           // 시작 위치

    // NetworkVariable 설정 (Owner 권한 필수)
    public NetworkVariable<bool> isGrounded = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private float minX, maxX;           // 이동 제한 변수

    // Animator Hash ID
    int hashIsGround;
    int hashIsSpike;
    int hashIsSliding;
    int hashSlidingEnd;
    int hashIsWin;
    int hashIsDefeat;

    // 초기화
    private void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        networkTransform = GetComponent<ClientNetworkTransform>();

        hashIsGround = Animator.StringToHash("IsGround");
        hashIsSpike = Animator.StringToHash("IsSpike");
        hashIsSliding = Animator.StringToHash("IsSliding");
        hashSlidingEnd = Animator.StringToHash("SlidingEnd");
        hashIsWin = Animator.StringToHash("IsWin");
        hashIsDefeat = Animator.StringToHash("IsDefeat");

        anim.SetBool(hashIsGround, isGrounded.Value);
    }

    public override void OnNetworkSpawn()
    {
        isGrounded.OnValueChanged += IsGroundedChange;

        anim.SetBool(hashIsGround, isGrounded.Value);

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
            maxX = -1.1f;
        }
        else
        {
            minX = 1.1f;
            maxX = 8.5f;
        }

        startPos = transform.position;
        groundY = shadowTransform.position.y;
    }

    public override void OnNetworkDespawn()
    {
        // 이벤트 연결 해제
        isGrounded.OnValueChanged -= IsGroundedChange;

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

        float x = InputManager.MoveInput;
        float y = 0f;
        if (Keyboard.current.upArrowKey.isPressed) y = 1f;
        else if (Keyboard.current.downArrowKey.isPressed) y = -1f;

        Vector2 currentDir = new Vector2(x, y);

        // 입력값이 바뀌었을 때만 서버에 전송 (최적화)
        if (currentDir != inputDirection.Value)
        {
            UpdateInputDirServerRpc(currentDir);
        }
    }

    private void LateUpdate()
    {
        if (shadowTransform == null) return;

        // 현재 그림자 월드 좌표
        Vector2 shadowPos = shadowTransform.position;

        // Y좌표 강제 고정
        shadowPos.y = groundY;

        // 변경된 위치 적용
        shadowTransform.position = shadowPos;
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
        if (isGrounded.Value && !isSliding)
        {
            PlayActionSoundServerRpc("Jump");

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isGrounded.Value = false;

            anim.SetBool(hashIsGround, isGrounded.Value);
        }
    }

    // 스파이크 처리
    private void HandleSpike()
    {
        // 현재 누르고 있는 방향키 값 (-1, 0, 1)
        float currentMoveInput = InputManager.MoveInput;
        
        // 슬라이딩
        if (isGrounded.Value && !isSliding)
        {
            if (currentMoveInput != 0)
            {
                PlayActionSoundServerRpc("Jump");
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

        //anim.SetTrigger(hashIsSliding);
        TriggerAnimationPlayServerRpc(hashIsSliding);

        // 입력된 방향으로 힘을 가함
        float slideDir = Mathf.Sign(direction);

        // 원래 보고 있던 방향 저장
        float originalFacing = Mathf.Sign(transform.localScale.x);

        // 슬라이딩 방향으로 전환
        SetFacingServerRpc(slideDir);

        // Y축은 0f로 만들어서 바닥에 붙게 함
        rb.linearVelocity = new Vector2(slideDir * slideForce, 0f);

        yield return new WaitForSeconds(slideDuration);

        //anim.SetTrigger(hashSlidingEnd);
        TriggerAnimationPlayServerRpc(hashSlidingEnd);

        yield return new WaitForSeconds(0.35f);

        // 원래 방향으로 복귀
        SetFacingServerRpc(originalFacing);

        // 슬라이딩 끝나면 멈춤 (관성 제거)
        rb.linearVelocity = Vector2.zero;

        isSliding = false;
    }

    // 스파이크
    private void Spike()
    {
        // 쿨타임 체크 (로컬에서 거름)
        if (Time.time < lastSpikeTime + spikeCooldown) return;

        PlayActionSoundServerRpc("Spike");

        float x = InputManager.MoveInput;
        float y = 0f;
        if (Keyboard.current.upArrowKey.isPressed) y = 1f;
        else if (Keyboard.current.downArrowKey.isPressed) y = -1f;

        Vector2 currentDir = new Vector2(x, y);

        StartCoroutine(SpikeCoroutine(currentDir));

        //anim.SetTrigger(hashIsSpike);
        TriggerAnimationPlayServerRpc(hashIsSpike);
        // 추후 기능 추가
    }

    private IEnumerator SpikeCoroutine(Vector2 dir)
    {
        lastSpikeTime = Time.time;  // 쿨타임 갱신

        // 스파이크를 서버에 알림
        SetSpikeStateServerRpc(true, dir);

        // 판정 지속 시간
        yield return new WaitForSeconds(spikeDuration);

        // 스파이크 끝을 서버에 알림
        SetSpikeStateServerRpc(false, Vector2.zero);
    }

    // 바닥에 닿았을 때 체크
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsOwner) return;

        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            isGrounded.Value = true;
            isSliding = false;

            anim.SetBool(hashIsGround, isGrounded.Value);
        }
    }

    private void IsGroundedChange(bool oldVal, bool newVal)
    {
        anim.SetBool(hashIsGround, newVal);
    }

    private void SetFacingDirection(float dirX)
    {
        if (dirX == 0) return;

        Vector3 currentScale = transform.localScale;

        currentScale.x = Mathf.Abs(currentScale.x) * (dirX > 0 ? 1 : -1);

        transform.localScale = currentScale;
    }

    [ServerRpc]
    private void SetSpikeStateServerRpc(bool state, Vector2 dir)
    {
        isSpike.Value = state;

        // 스파이크를 할 때, 방향 정보도 강제 초기화
        if (state)
        {
            inputDirection.Value = dir;
        }
    }

    [ServerRpc]
    private void UpdateInputDirServerRpc(Vector2 dir)
    {
        inputDirection.Value = dir;
    }

    [ServerRpc]
    private void SetFacingServerRpc(float dirX)
    {
        SetFacingClientRpc(dirX);
    }

    [ClientRpc]
    private void SetFacingClientRpc(float dirX)
    {
        SetFacingDirection(dirX);
    }

    [ServerRpc]
    private void PlayActionSoundServerRpc(string soundName)
    {
        PlayActionSoundClientRpc(soundName);
    }

    [ClientRpc]
    private void PlayActionSoundClientRpc(string soundName)
    {
        // 소리 재생은 각자 컴퓨터에서 실행됨
        SoundManager.Instance.PlaySFX(soundName);
    }

    [ClientRpc]
    public void ResetPlayerPositionClientRpc()
    {
        if (IsOwner)
        {
            //transform.position = startPos;
            networkTransform.Teleport(startPos, Quaternion.identity, transform.localScale);
        }
    }

    [ClientRpc]
    public void EndGameResultClientRpc(bool isWinner)
    {
        // 조작 막기
        this.enabled = false;

        // 미끄럼 방지
        rb.linearVelocity = Vector2.zero;
        //rb.bodyType = RigidbodyType2D.Kinematic;

        // 승패 애니메이션
        if (isWinner)
        {
            anim.SetTrigger(hashIsWin);
        }
        else
        {
            anim.SetTrigger(hashIsDefeat);
        }
    }

    [ServerRpc]
    private void TriggerAnimationPlayServerRpc(int hashcode)
    {
        TriggerAnimationPlayClientRpc(hashcode);
    }

    [ClientRpc]
    private void TriggerAnimationPlayClientRpc(int hashcode)
    {
        anim.SetTrigger(hashcode);
    }
}
