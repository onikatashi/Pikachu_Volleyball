using UnityEngine;

public class AIController : MonoBehaviour
{
    public float speed = 5f;
    public float reactionDelay = 0.1f; // 반응 속도 (낮을수록 잘함)

    private Transform ball;
    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // 공을 찾지 못했으면 계속 찾음
        if (ball == null)
        {
            GameObject ballObj = GameObject.FindGameObjectWithTag("Ball");
            if (ballObj != null) ball = ballObj.transform;
            return;
        }

        // 공의 Y축 위치를 따라가도록 움직임
        float direction = 0f;

        // 공이 나보다 위에 있으면 위로, 아래에 있으면 아래로
        if (ball.position.y > transform.position.y + 0.2f)
        {
            direction = 1f;
        }
        else if (ball.position.y < transform.position.y - 0.2f)
        {
            direction = -1f;
        }

        // 움직임 적용 (Rigidbody 사용)
        rb.linearVelocity = new Vector2(0, direction * speed);
    }
}
