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
    // 공이 튀어오르는 힘 (위쪽)
    public float hitUpForce = 8f;
    // 공이 날아가는 힘 (앞쪽)
    public float hitForwardFroce = 5f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();
        sr = GetComponent<SpriteRenderer>();
        networkTransform = GetComponent<NetworkTransform>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 점수 계산과 충돌 처리는 서버에서만 관리
        if (!IsServer) return;

        // 플레이어와 부딪혔을 때 (공 튀기기)
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Vector2 dir = Vector2.up;

            // 공이 플레이어보다 왼쪽에 있으면 외쪽으로, 아니면 오른쪽으로 튐
            if (collision.transform.position.x < transform.position.x) dir.x = 1;
            else dir.x = -1;

            rb.linearVelocity = Vector2.zero;
            rb.linearVelocity = new Vector2(dir.x * hitForwardFroce, hitUpForce);
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            GameSetupManager.Instance.OnBallHitGround(collision.gameObject.name);
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

    [ClientRpc]
    private void SetActiveClientRpc(bool isActive)
    {
        rb.simulated = isActive;        // 물리를 끄면 충돌도 안 일어남
        if (sr != null) sr.enabled = isActive;
        if (col != null) col.enabled = isActive;
    }
}
