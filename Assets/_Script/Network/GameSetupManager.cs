using NUnit.Framework;
using NUnit.Framework.Constraints;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSetupManager : NetworkBehaviour 
{
    // 싱글톤 패턴 (BallController에서 접근하기 위해)
    public static GameSetupManager Instance { get; private set; }

    [Header("프리팹 설정")]
    public GameObject playerPrefab;             // 실제로 움직일 피카츄 프리팹
    public GameObject ballPrefab;               // 공 프리팹

    [Header("스폰 위치")]
    public Transform[] playerSpawnPoints;       // 0은 왼쪽, 1은 오른쪽
    public Transform[] ballSpwnPoints;          // 0은 왼쪽, 1은 오른쪽

    [Header("점수 스프라이트(0~9)")]
    public Sprite[] numberSprites;              // 0~9 숫자 스프라이트

    [Header("점수 UI")]
    // 1P 점수판
    public Image leftTensImage;
    public Image leftOnesImage;

    // 2P 점수판
    public Image rightTensImage;
    public Image rightOnesImage;

    [Header("게임 시작 관리")]
    public CanvasGroup readyImage;              // Ready? 이미지
    public NetworkVariable<bool> isGameActive = new NetworkVariable<bool>(false);

    // 점수 변수 (NetworkVariable)
    private NetworkVariable<int> p1Score = new NetworkVariable<int>(0);
    private NetworkVariable<int> p2Score = new NetworkVariable<int>(0);
    private const int WIN_SCORE = 3;

    private BallController ballController;      // 현재 소환된 공을 기억해둠
    private Transform nextBallPos;              // 다음 게임 시작 됐을 때, 공 위치

    private PlayerController[] pikachus = new PlayerController[2];

    private bool isScored = false;              // 중복 점수 획득 및 게임 종료후 점수 획득 방지용

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        // 점수 바뀌면 이미지 업데이트 하도록 연결
        p1Score.OnValueChanged += (oldVal, newVal) => UpdateScoreUI();
        p2Score.OnValueChanged += (oldVal, newVal) => UpdateScoreUI();

        UpdateScoreUI();

        // 플레이어 생성 권한은 호스트(Server)에게만 있음.
        if (IsServer)
        {
            SpawnPlayers();
            SpawnBall();
        }

        StartCoroutine(StartNewRoundCoroutine());
    }

    private void Start()
    {
        SoundManager.Instance.PlayBGM("GameBgm");
    }

    private void OnDisable()
    {
        SoundManager.Instance.StopBGM();
    }

    // 라운드 시작
    private IEnumerator StartNewRoundCoroutine()
    {
        if(!IsServer) yield break;

        isScored = false;

        // Ready 애니메이션 재생
        PlayReadyAnimationClientRpc();

        // 애니메이션 시간만큼 대기
        yield return new WaitForSeconds(1.8f);

        isGameActive.Value = true;
    }

    // Ready 애니메이션
    private IEnumerator BlinkReadySpriteCoroutine()
    {
        if (readyImage == null) yield break;

        readyImage.gameObject.SetActive(true);

        for (int i = 0; i < 3; i++)
        {
            // 켜짐
            readyImage.alpha = 1f;
            yield return new WaitForSeconds(0.3f);

            // 꺼짐
            readyImage.alpha = 0f;
            yield return new WaitForSeconds(0.3f);
        }

        readyImage.gameObject.SetActive(false);
    }

    private void UpdateScoreUI()
    {
        int score1 = p1Score.Value;
        int tens1 = score1 / 10;
        int ones1 = score1 % 10;

        leftTensImage.sprite = numberSprites[tens1];
        leftOnesImage.sprite = numberSprites[ones1];

        int score2 = p2Score.Value;
        int tens2 = score2 / 10;
        int ones2 = score2 % 10;

        rightTensImage.sprite = numberSprites[tens2];
        rightOnesImage.sprite = numberSprites[ones2];
    }

    // 플레이어 피카츄 소환
    private void SpawnPlayers()
    {
        // 현재 접속해 있는 모든 유저들의 ID 목록을 가져옴
        var clients = NetworkManager.Singleton.ConnectedClientsIds;

        foreach (var client in clients)
        {
            // 클라이언트 ID가 ServerClinetId(보통 0, 호스트)와 같으면 0번 플레이어
            // 아니면 1번 플레이어
            int index = (client == NetworkManager.ServerClientId) ? 0 : 1;

            // 이미 자리에 있다면 스킵
            if (pikachus[index] != null) continue;

            // 위치 선정
            Transform spawnTransform = playerSpawnPoints[index];

            // 피카츄 생성 (서버에만 존재)
            GameObject playerInstance = Instantiate(
                playerPrefab,
                spawnTransform.position,
                Quaternion.identity
                );

            // 이미지 좌우 반전
            if(index == 1)
            {
                playerInstance.transform.localScale = new Vector2(-1f, 1f);
            }

            // 네트워크 스폰 + 소유권 부여
            // => 이 오브젝트를 네트워크 모든 사람들에게 보여주고, 조종권한은 client에게 줘라
            playerInstance.GetComponent<NetworkObject>().SpawnWithOwnership(client);

            PlayerController pc = playerInstance.GetComponent<PlayerController>();
            if (pc != null)
            {
                pikachus[index] = pc;
            }
        }
    }
    
    // 공 소환
    private void SpawnBall()
    {
        GameObject ballInstance = Instantiate(ballPrefab, ballSpwnPoints[0].position, Quaternion.identity);
        NetworkObject ballNetObj = ballInstance.GetComponent<NetworkObject>();
        ballNetObj.Spawn();

        // 생성된 공의 스크립트를 저장해둠
        ballController = ballInstance.GetComponent<BallController>();
    }

    public void OnBallHitGround(string objName)
    {
        // 로직은 서버에서 진행
        if (!IsServer) return;

        if (p1Score.Value >= WIN_SCORE ||  p2Score.Value >= WIN_SCORE) return;

        if (isScored) return;

        isScored = true;

        StartCoroutine(ProcessScoreSequence(objName));
    }

    private IEnumerator ProcessScoreSequence(string objName)
    {
        if (objName == "LeftGround")
        {
            nextBallPos = ballSpwnPoints[1];
            p2Score.Value++;
        }
        else
        {
            nextBallPos = ballSpwnPoints[0];
            p1Score.Value++;
        }

        // 게임 종료 체크
        if (p1Score.Value >= WIN_SCORE ||  p2Score.Value >= WIN_SCORE)
        {
            SoundManager.Instance.PlaySFX("GameEnd");

            // 누가 이겼는지 확인
            int winnerIndex = (p1Score.Value >= WIN_SCORE ? 0 : 1);

            for (int i = 0; i < pikachus.Length; i++)
            {
                if (pikachus[i] == null) continue;

                bool isWinner = (i == winnerIndex);

                pikachus[i].EndGameResultClientRpc(isWinner);
            }

            Debug.Log("게임 종료!");
            yield return new WaitForSeconds(3.0f);

            StartCoroutine(ReturnToLobbySequence());

            yield break;
        }

        // 슬로우 모션 시작 (ClientRpc로 다같이 느려져야 함)
        SetTimeScaleClientRpc(0.3f);

        // 슬로우 모션 상태로 잠시 대기 (리얼타임 기준 1초)
        yield return new WaitForSecondsRealtime(1.2f);

        // timescale 원상복구
        SetTimeScaleClientRpc(1.0f);

        // 다음 게임을 위한 공 위치 초기화
        if (ballController != null)
        {
            // 화면 까매지는 거
            GameSetupFadeInClientRpc();

            // 공 위치 리셋
            ballController.ResetBall(nextBallPos.position);

            // 공이 텔레포트 할 시간 
            yield return new WaitForSecondsRealtime(0.2f);

            ballController.SetActiveState(true);
        }

        // 플레이어 위치 초기화
        foreach(PlayerController pikachu in pikachus)
        {
            if (pikachu != null)
            {
                pikachu.ResetPlayerPositionClientRpc();
            }
        }

        // 게임 동작 정지 (플레이어 / 공 멈춤)
        isGameActive.Value = false;
        
        GameSetupFadeOutClientRpc();

        yield return new WaitForSeconds(SceneLoaderManager.Instance.fadeDuration);

        StartCoroutine(StartNewRoundCoroutine());
    }

    private IEnumerator ReturnToLobbySequence()
    {
        // 화면 검은색으로 전환
        GameSetupFadeInClientRpc();
        float fadeDuration = SceneLoaderManager.Instance.fadeDuration;
        yield return new WaitForSeconds(fadeDuration);

        // 플레이어 ready 상태 초기화
        ResetAllPlayersReadyState();

        // 네트워크 씬 전환
        NetworkManager.Singleton.SceneManager.LoadScene("02_LobbyScene", LoadSceneMode.Single);
    }

    // NetworkPlayerState를 사용해서 ready 초기화
    private void ResetAllPlayersReadyState()
    {
        foreach (var playerState in NetworkPlayerState.allPlayers)
        {
            if (playerState != null)
            {
                playerState.isReady.Value = false;
            }
        }
    }

    [ClientRpc]
    private void SetTimeScaleClientRpc(float scale)
    {
        Time.timeScale = scale;

        // FixedDeltaTime도 같이 조절해야 부드러움
        Time.fixedDeltaTime = 0.02f * scale;
    }

    [ClientRpc]
    private void GameSetupFadeInClientRpc()
    {
        // 각 클라이언트가 자신의 SceneTransitionManager를 통해 효과 재생
        StartCoroutine(SceneLoaderManager.Instance.FadeInBlackBackground());
    }

    [ClientRpc]
    private void GameSetupFadeOutClientRpc()
    {
        // 각 클라이언트가 자신의 SceneTransitionManager를 통해 효과 재생
        StartCoroutine(SceneLoaderManager.Instance.FadeOutBlackBackground());
    }

    [ClientRpc]
    private void PlayReadyAnimationClientRpc()
    {
        StartCoroutine(BlinkReadySpriteCoroutine());
    }
}
