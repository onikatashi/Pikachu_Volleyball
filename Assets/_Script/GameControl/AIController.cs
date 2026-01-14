using UnityEngine;

public class AIController : MonoBehaviour
{
    [Header("AI 설정")]
    public float reactionDelay = 0.1f;      // 반응 속도
    public float errorMargin = 0.2f;        // 오차 범위

    [Header("맵 정보")]
    private float netXPosition = 0f;
    private float mybaseX;
    private float mapMinX, mapMaxX;

    // 참조 컴포넌트
    private PlayerController myPlayerController;
    private Transform ballTransform;
    private Rigidbody2D ballRb;

    // 내부 변수
    private float targetX;
    private float timer = 0f;
    private bool isSecondPlayer = false;    // 내가 2P(오른쪽)인가?

    private void Awake()
    {
        myPlayerController = GetComponent<PlayerController>();

        if (myPlayerController != null)
        {
            myPlayerController.SetAIState(true);
        }
    }

    private void Start()
    {
        if (transform.position.x > netXPosition)
        {
            isSecondPlayer = true;
            mybaseX = 4f;
            mapMinX = 1.1f;
            mapMaxX = 8.5f;
        }
        else
        {
            isSecondPlayer = false;
            mybaseX = -4f;
            mapMinX = -8.5f;
            mapMaxX = -1.1f;
        }
    }

    // GameSetupManager에서 호출하여 공 정보를 얻어옴
    public void SetBallTarget(Transform ball)
    {
        ballTransform = ball;
        ballRb = ball.GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // 게임이 시작되지 않았거나 공이 없으면 아무것도 안 함
        if ((GameSetupManager.Instance == null || !GameSetupManager.Instance.isGameActive.Value))
        {
            // 움직임 멈춤
            if (myPlayerController != null) myPlayerController.Move(0f);
            return;
        }

        if (GameSetupManager.Instance.isGameOver)
        {
            return;
        }

        if (ballTransform == null || myPlayerController == null) return;

        // 슬라이딩 중이면 AI 판단 중지
        if (myPlayerController.IsSliding) return;

        // 반응 속도 딜레이 체크
        timer += Time.deltaTime;
        if (timer >= reactionDelay)
        {
            CalculateTargetPosition();
            DecideAction();
            timer = 0f;
        }

        // 실제 이동 수행
        MoveAI();
    }

    private void CalculateTargetPosition()
    {
        // 공이 내 진영 쪽에 있는가
        bool ballIsOnMySide = isSecondPlayer ? (ballTransform.position.x > 0) : (ballTransform.position.x < 0);

        // 속도 체크 (내 쪽으로 날아오는 중인가?)
        bool ballComingToMe = isSecondPlayer ? (ballRb.linearVelocity.x > 0.5f) : (ballRb.linearVelocity.x < -0.5f);

        if (ballIsOnMySide || ballComingToMe)
        {
            // 낙하 지점 예측
            float predictedX = PredictLandingX(-2f);

            // 맵 밖으로 나가지 않게 타겟 보정
            targetX = Mathf.Clamp(predictedX, mapMinX, mapMaxX);
        }
        else
        {
            // 공이 상대방 쪽에 있으면 수비 위치로 복귀
            targetX = mybaseX;
        }
    }

    // 이동
    private void MoveAI()
    {
        float xDiff = targetX - transform.position.x;

        // 목표 지점과 거의 비슷하면 멈춤
        if (Mathf.Abs(xDiff) < 0.1f)
        {
            myPlayerController.Move(0);
            return;
        }

        // 방향 결정
        float dir = Mathf.Sign(xDiff);
        myPlayerController.Move(dir);
    }

    // 행동 결정
    private void DecideAction()
    {
        float distX = Mathf.Abs(ballTransform.position.x - transform.position.x);
        float distY = ballTransform.position.y - transform.position.y;

        bool isGrounded = myPlayerController.isGrounded.Value;

        // 스파이크: 점프 중이고 공이 머리 위에 있음
        if (!isGrounded)
        {
            // 공이 내 타격 범위 안에 들어왔는지 체크
            if (distX < 1.2f && distY > 0.5f && distY < 2.5f)
            {
                // 방향 계산
                float attackDirX = isSecondPlayer ? -1f : 1f;

                // 상황 판단 변수 계산
                // 네트와의 거리
                float distToNet = Mathf.Abs(transform.position.x);
                // 나의 점프 높이
                float myHeight = transform.position.y;

                // [아래쪽] 네트에 아주 가까울 때
                if (distToNet < 1.3f && myHeight > 2.3f)
                {
                    myPlayerController.Spike(0f, -1f);
                    Debug.Log("AI: 아래 스파이크!");
                }

                // [아래쪽 앞 대각선] 
                else if (distToNet < 2.5f && myHeight > 2f)
                {
                    myPlayerController.Spike(attackDirX, -1f);
                    Debug.Log("AI: 아래앞 스파이크!");
                }

                // [앞쪽]
                else if (distToNet < 5.0f)
                {
                    myPlayerController.Spike(attackDirX, 0f);
                    Debug.Log("AI: 앞 스파이크!");
                }

                // [위쪽 앞 대각선]
                else if (distToNet >= 5.0f)
                {
                    myPlayerController.Spike(attackDirX, 1f);
                    Debug.Log("AI: 위앞 스파이크!");
                }

                // [위쪽]
                else
                {
                    myPlayerController.Spike(0f, 1f);
                    Debug.Log("AI: 위 스파이크!");
                }

            }
            return;
        }

        // 슬라이딩: 땅에 있고, 공이 멀고 낮게 옴
        if (isGrounded && distX > 3f && ballTransform.position.y < -1.1f)
        {
            bool ballIsInMySide = isSecondPlayer ? (ballTransform.position.x > 0) : (ballTransform.position.x < 0);
            if (ballIsInMySide)
            {
                // 공 방향으로 슬라이딩
                float slideDir = (ballTransform.position.x > transform.position.x) ? 1f : -1f;

                StartCoroutine(myPlayerController.Sliding(slideDir));
            }
            return;
        }

        // 점프: 공이 내 근처 + 높음
        if (isGrounded)
        {
            if (distX <1.5f && distY >2.0f && ballRb.linearVelocity.y < 0)
            {
                myPlayerController.HandleJump();
            }
        }
    }

    // 낙하 지점 예측
    private float PredictLandingX(float targetY)
    {
        float v0y = ballRb.linearVelocity.y;
        float v0x = ballRb.linearVelocity.x;
        float g = Mathf.Abs(Physics2D.gravity.y * ballRb.gravityScale);

        float distY = ballTransform.position.y - targetY;

        // 이미 목표보다 아래에 있으면 현재 X 리턴
        if (distY < 0) return ballTransform.position.x;

        // 근의 공식으로 낙하 시간 계산
        // h = v0*t + 0.5*g*t^2 => 0.5gt^2 + v0y*t - disty = 0
        float val = v0y * v0y + 2 * g * distY;
        if (val < 0) return ballTransform.position.x;       // 허수 방지

        float t = (v0y + Mathf.Sqrt(val)) / g;

        // 도달 위치 X = x0 + vx * t
        float finalX =ballTransform.position.x + (v0x * t);

        // 벽 튕김 계산 (간단하게 구현
        if (finalX > 9f)
        {
            finalX = 9f - (finalX - 9f);
        }
        if (finalX < -9f)
        {
            finalX = -9f - (finalX + 9f);
        }

        // 오차 추가
        return finalX + Random.Range(-errorMargin, errorMargin);
    }
}
