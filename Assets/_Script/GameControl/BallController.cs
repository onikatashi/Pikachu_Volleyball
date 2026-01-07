using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class BallController : NetworkBehaviour
{
    private Rigidbody2D rb;
    private CircleCollider2D col;
    private SpriteRenderer sr;
    private NetworkTransform networkTransform;      // 텔레포트 위해 필요

    [Header("공 물리 설정")]
    public float hitUpForce = 8f;                   // 공이 튀어오르는 힘 (위쪽)
    public float hitForwardFroce = 5f;              // 공이 날아가는 힘 (앞쪽)
    public float rotationMultiplier = 50f;          // 회전 속도

    [Header("스파이크 설정")]
    public float spikeSpeed = 18f;

    // 잔상 설정
    [Header("잔상 효과")]
    public float ghostInterval = 0.05f;             // 잔상 생성 간격
    public float ghostDuration = 0.4f;              // 잔상이 생성되는 총 시간
    public float ghostFadeSpeed = 3f;               // 잔상이 사라지는 속도


    private bool isSpikeActive = false;             // 현재 스파이크 상태인지

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();
        sr = GetComponent<SpriteRenderer>();
        networkTransform = GetComponent<NetworkTransform>();
    }

    private void Update()
    {
        float rotateSpeed = -rb.linearVelocity.x * rotationMultiplier * Time.deltaTime;

        transform.Rotate(0, 0, rotateSpeed);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 점수 계산과 충돌 처리는 서버에서만 관리
        if (!IsServer) return;

        // 플레이어와 부딪혔을 때 (공 튀기기)
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            HandlePlayerCollision(collision);
        }

        // 바닥에 부딪혔을 때 (점수 처리)
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            PlayActionSoundServerRpc("BallGround");
            GameSetupManager.Instance.OnBallHitGround(collision.gameObject.name);
        }
    }

    private void HandlePlayerCollision(Collision2D collision)
    {
        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        if (player == null) return;

        float facingDir = collision.transform.position.x < transform.position.x ? 1 : -1;

        // 스파이크 상태일 때
        if (player.isSpike.Value)
        {
            PlayActionSoundServerRpc("BallSpike");

            Vector2 input = player.inputDirection.Value;
            Vector2 spikeDir;

            // 위쪽 키가 눌렸을 때
            if (input.y > 0)
            {
                // 앞 + 위 => 대각선 위 방향
                if (Mathf.Abs(input.x) > 0)
                {
                    spikeDir = new Vector2(facingDir * 0.7f, 0.8f);
                }
                // 위 방향키만
                else
                {
                    spikeDir = new Vector2(facingDir * 0.2f, 1f);
                }
            }

            // 아래쪽 키가 눌렸을 때
            else if (input.y < 0)
            {
                // 앞 + 아래 => 대각선 아래 방향
                if (Mathf.Abs(input.x) > 0)
                {
                    spikeDir = new Vector2(facingDir * 0.7f, -0.8f);
                }
                // 아래 방향키만
                else
                {
                    spikeDir = new Vector2(facingDir * 0.2f, -1f);
                }
            }

            // 위 아래 키 안눌림 (키입력 x 또는 좌우 키입력)
            else
            {
                // 앞 방향키 눌렸을 때
                if (Mathf.Abs(input.x) > 0)
                {
                    spikeDir = new Vector2(facingDir * 1.0f, -0.25f);
                }
                // 키 입력 없음: 기본 스파이크
                else
                {
                    spikeDir = new Vector2(facingDir * 0.6f, -0.6f);
                }
            }

            // 최종 정규화 및 속도 적용
            rb.linearVelocity = Vector2.zero;   // 기존 속도 제거
            rb.linearVelocity = spikeDir.normalized * spikeSpeed;     
        }
        // 일반 타격 (리시브)
        else
        {
            rb.linearVelocity = Vector2.zero;
            rb.linearVelocity = new Vector2(facingDir * hitForwardFroce, hitUpForce);
        }
    }

    // 공 위치 리셋
    public void ResetBall(Vector2 pos)
    {
        if (IsServer)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            // NetworkTransform이 보간이 적용되기 때문에 텔레포트 사용
            if (networkTransform != null)
            {
                networkTransform.Teleport(pos, Quaternion.identity, Vector2.one);
            }
            else
            {
                // 혹시라도 없으면 그냥 이동
                transform.position = pos;
            }
        }
    }

    // 공 끄기 / 켜기 (ClientRpc로 모두에게 적용)
    public void SetActiveState(bool isActive)
    {
        SetActiveClientRpc(isActive);
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
    private void SetActiveClientRpc(bool isActive)
    {
        rb.simulated = isActive;        // 물리를 끄면 충돌도 안 일어남
        if (sr != null) sr.enabled = isActive;
        if (col != null) col.enabled = isActive;
    }

    [ClientRpc]
    private void ActivateSpikeEffectClientRpc(float duration)
    {

    }

    [ClientRpc]
    private void StopSpikeEffectClientRpc()
    {

    }


}
