using UnityEngine;

public class AIController : MonoBehaviour
{
    [Header("AI 설정")]
    public float reactionDelay = 0.05f;      // 반응 속도

    [Header("점프 타이밍 설정")]
    public float jumpTriggerHeight = 1.5f;
    public float jumpHeightOffset = 0.3f;

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

    // 점프 물리 캐시
    private float gravity;
    private float jumpForce;
    private float jumpApexTime;
    private float jumpApexHeight;

    // 점프 쿨다운
    private float lastJumpTime = -999f;
    private const float JUMP_COOLDOWN = 0.3f;

    // ── 예측 위치 고정 ──
    // 공이 새로 날아올 때마다 한 번만 계산하고, 공이 내 진영에 있는 동안은 유지
    // 매 틱 재계산하면 오차(Random.Range)가 매번 더해져서 targetX가 흔들림
    private float lockedTargetX;
    private bool isTargetLocked = false;
    private bool wasBallOnMySide = false;    // 이전 프레임에 공이 내 쪽이었는지

    private float prevBallVelX = 0f;

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

        float playerGravityScale = (myPlayerController != null && myPlayerController.Rb != null)
            ? myPlayerController.Rb.gravityScale : 1f;
        gravity = Mathf.Abs(Physics2D.gravity.y) * playerGravityScale;

        jumpForce = myPlayerController != null ? myPlayerController.jumpForce : 10f;
        jumpApexTime = jumpForce / gravity;
        jumpApexHeight = jumpForce * jumpApexTime - 0.5f * gravity * jumpApexTime * jumpApexTime;
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

        if (GameSetupManager.Instance.isGameOver.Value) return;
        if (ballTransform == null || myPlayerController == null) return;

        // 반응 속도 딜레이 체크
        timer += Time.deltaTime;
        if (timer >= reactionDelay)
        {
            CalculateTargetPosition();

            // 슬라이딩 중이 아닐 때만 점프/스파이크/슬라이딩 판단
            if (!myPlayerController.IsSliding)
                DecideAction();

            timer = 0f;
        }

        // 실제 이동 수행
        MoveAI();
    }

    private void CalculateTargetPosition()
    {
        bool ballIsOnMySide = isSecondPlayer
            ? (ballTransform.position.x > 0)
            : (ballTransform.position.x < 0);

        bool ballComingToMe = isSecondPlayer
            ? (ballRb.linearVelocity.x > 0.5f)
            : (ballRb.linearVelocity.x < -0.5f);

        float curVelX = ballRb.linearVelocity.x;
        if (Mathf.Sign(curVelX) != Mathf.Sign(prevBallVelX) && Mathf.Abs(curVelX) > 0.5f)
        {
            isTargetLocked = false;
        }
        prevBallVelX = curVelX;

        if (ballIsOnMySide)
        {
            // 공이 내 진영에 막 들어왔을 때만 예측 위치 새로 계산
            if (!wasBallOnMySide || !isTargetLocked)
            {
                float predicted = CalculateLandingX();
                lockedTargetX = Mathf.Clamp(predicted, mapMinX, mapMaxX);
                isTargetLocked = true;
            }
            targetX = lockedTargetX;
        }
        else if (ballComingToMe && !isTargetLocked)
        {
            // 아직 내 진영은 아니지만 날아오는 중 → 미리 예측해서 이동 준비
            float predicted = CalculateLandingX();
            lockedTargetX = Mathf.Clamp(predicted, mapMinX, mapMaxX);
            isTargetLocked = true;
            targetX = lockedTargetX;
        }
        else if (!ballIsOnMySide && !ballComingToMe)
        {
            // 공이 상대방 쪽에 있고 내 쪽으로 오지도 않음 → 락 해제 + 수비 위치로
            isTargetLocked = false;
            targetX = mybaseX;
        }

        wasBallOnMySide = ballIsOnMySide;
    }

    // 공의 낙하/도달 위치를 딱 한 번 계산 (오차는 여기서만 더함)
    private float CalculateLandingX()
    {
        // 1순위: 점프 타격 높이에서의 X
        float strikeY = transform.position.y + jumpTriggerHeight;
        float strikeX = PredictBallXAtHeight(strikeY);
        if (strikeX != float.MinValue)
            return strikeX;

        // 2순위: 바닥 낙하 지점
        float landX = PredictLandingX(-2f);
        return PredictLandingX(-2f);
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
        myPlayerController.Move(Mathf.Sign(xDiff));
    }

    // 행동 결정
    private void DecideAction()
    {
        float distX = Mathf.Abs(ballTransform.position.x - transform.position.x);
        float distY = ballTransform.position.y - transform.position.y;
        bool isGrounded = myPlayerController.isGrounded.Value;

        if (!isGrounded)
        {
            TrySpike(distX, distY);
            return;
        }

        // 슬라이딩
        if (distX > 3f && ballTransform.position.y < 0.0f)
        {
            bool ballIsInMySide = isSecondPlayer
                ? (ballTransform.position.x > 0)
                : (ballTransform.position.x < 0);

            if (ballIsInMySide)
            {
                float slideDir = (ballTransform.position.x > transform.position.x) ? 1f : -1f;
                StartCoroutine(myPlayerController.Sliding(slideDir));
                return;
            }
        }

        TryJump(distX, distY);
    }

    private void TryJump(float distX, float distY)
    {
        if (Time.time < lastJumpTime + JUMP_COOLDOWN) return;
        if (distX > 3.0f) return;

        float strikeY = transform.position.y + jumpTriggerHeight;

        if (ballTransform.position.y < strikeY - 0.5f && ballRb.linearVelocity.y <= 0) return;

        float tBallToStrike = TimeForBallToReachHeight(strikeY);
        if (tBallToStrike < 0) return;

        float tJumpToStrike = TimeForJumpToReachHeight(jumpTriggerHeight);
        if (tJumpToStrike < 0) return;

        float timeDiff = tBallToStrike - tJumpToStrike;

        if (timeDiff >= 0f && timeDiff <= 0.35f)
        {
            lastJumpTime = Time.time;
            myPlayerController.HandleJump();
        }
    }

    private float TimeForBallToReachHeight(float targetY)
    {
        float v0y = ballRb.linearVelocity.y;
        float g = Mathf.Abs(Physics2D.gravity.y * ballRb.gravityScale);
        float dy = targetY - ballTransform.position.y;

        float a = 0.5f * g;
        float b = -v0y;
        float c = dy;

        float discriminant = b * b - 4 * a * c;
        if (discriminant < 0) return -1f;

        float sqrtD = Mathf.Sqrt(discriminant);
        float t1 = (-b - sqrtD) / (2 * a);
        float t2 = (-b + sqrtD) / (2 * a);

        if (t1 > 0 && t2 > 0) return Mathf.Min(t1, t2);
        if (t1 > 0) return t1;
        if (t2 > 0) return t2;
        return -1f;
    }

    private float TimeForJumpToReachHeight(float relativeHeight)
    {
        float a = 0.5f * gravity;
        float b = -jumpForce;
        float c = relativeHeight;

        float discriminant = b * b - 4 * a * c;
        if (discriminant < 0) return -1f;

        float sqrtD = Mathf.Sqrt(discriminant);
        float t1 = (-b - sqrtD) / (2 * a);
        float t2 = (-b + sqrtD) / (2 * a);

        if (t1 > 0) return t1;
        if (t2 > 0) return t2;
        return -1f;
    }

    private void TrySpike(float distX, float distY)
    {
        if (distX > 1.2f) return;
        if (distY < 0.3f) return;
        if (distY > 3.0f) return;

        bool ballComingToMe = isSecondPlayer
            ? (ballRb.linearVelocity.x >= 0)
            : (ballRb.linearVelocity.x <= 0);

        if (!ballComingToMe && distX > 0.8f) return;

        float attackDirX = isSecondPlayer ? -1f : 1f;
        float distToNet = Mathf.Abs(transform.position.x);
        float myHeight = transform.position.y;

        if (distToNet < 1.3f && myHeight > 2.3f)
            myPlayerController.Spike(0f, -1f);
        else if (distToNet < 2.5f && myHeight > 2f)
            myPlayerController.Spike(attackDirX, -1f);
        else if (distToNet < 5.0f)
            myPlayerController.Spike(attackDirX, 0f);
        else
            myPlayerController.Spike(attackDirX, 1f);
    }

    // 낙하 지점 예측
    private float PredictLandingX(float targetY)
    {
        float v0y = ballRb.linearVelocity.y;
        float v0x = ballRb.linearVelocity.x;
        float g = Mathf.Abs(Physics2D.gravity.y * ballRb.gravityScale);

        // 공의 현재 Y에서 targetY까지의 이동 거리
        // dy = v0y*t - 0.5*g*t^2  =>  0.5g*t^2 - v0y*t + (ballY - targetY) = 0
        float dy = ballTransform.position.y - targetY;  // ballY - targetY (>0이면 공이 위에 있음)

        float a = 0.5f * g;
        float b = -v0y;
        float c = -dy;  // ballY - targetY를 우변으로 이항

        float discriminant = b * b - 4 * a * c;
        if (discriminant < 0) return ballTransform.position.x;

        float sqrtD = Mathf.Sqrt(discriminant);
        float t1 = (-b - sqrtD) / (2 * a);
        float t2 = (-b + sqrtD) / (2 * a);

        // 양수 근 중 큰 값 = 올라갔다가 targetY까지 내려오는 총 시간
        float t = -1f;
        if (t1 > 0 && t2 > 0) t = Mathf.Max(t1, t2);
        else if (t1 > 0) t = t1;
        else if (t2 > 0) t = t2;

        if (t < 0) return ballTransform.position.x;

        float finalX = ballTransform.position.x + v0x * t;

        // 벽 튕김 계산
        if (finalX > 9f) finalX = 9f - (finalX - 9f);
        if (finalX < -9f) finalX = -9f - (finalX + 9f);

        return finalX;
    }

    // 공이 특정 높이(절대 좌표)에 있을 때의 X 위치 예측 (오차 미포함 — CalculateLandingX에서 더함)
    private float PredictBallXAtHeight(float targetY)
    {
        float tToHeight = TimeForBallToReachHeight(targetY);
        if (tToHeight < 0) return float.MinValue;

        float v0x = ballRb.linearVelocity.x;
        float finalX = ballTransform.position.x + v0x * tToHeight;

        if (finalX > 9f) finalX = 9f - (finalX - 9f);
        if (finalX < -9f) finalX = -9f - (finalX + 9f);

        return Mathf.Clamp(finalX, mapMinX, mapMaxX);
    }

    private void OnDrawGizmos()
    {
        if (ballTransform == null || ballRb == null) return;

        float predictedX = PredictLandingX(-2f);
        Vector3 predictedPos = new Vector3(predictedX, -2f, 0f);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(predictedPos, 0.3f);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, predictedPos);

        float strikeY = transform.position.y + jumpTriggerHeight;
        float strikeX = PredictBallXAtHeight(strikeY);
        if (strikeX != float.MinValue)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(new Vector3(strikeX, strikeY, 0f), 0.2f);
        }
    }
}
