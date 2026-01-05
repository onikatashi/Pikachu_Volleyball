using System.Collections;
using Unity.Netcode;
using UnityEngine;

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

    private BallController ballController;      // 현재 소환된 공을 기억해둠
    private Transform nextBallPos;              // 다음 게임 시작 됐을 때, 공 위치

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
        // 플레이어 생성 권한은 호스트(Server)에게만 있음.
        if (IsServer)
        {
            SpawnPlayers();
            SpawnBall();
        }
    }

    // 플레이어 피카츄 소환
    private void SpawnPlayers()
    {
        // 현재 접속해 있는 모든 유저들의 ID 목록을 가져옴
        var clients = NetworkManager.Singleton.ConnectedClientsIds;

        int index = 0;
        foreach (var client in clients)
        {
            // 사람이 스폰 포인트보다 많으면 안됨
            if (index >= playerSpawnPoints.Length) break;

            // 위치 선정
            Transform spawnTransform = playerSpawnPoints[index];

            // 피카츄 생성 (서버에만 존재)
            GameObject playerInstance = Instantiate(
                playerPrefab,
                spawnTransform.position,
                Quaternion.identity
                );

            if(index == 1)
            {
                playerInstance.transform.localScale = new Vector2(-1f, 1f);
            }

            // 네트워크 스폰 + 소유권 부여
            // => 이 오브젝트를 네트워크 모든 사람들에게 보여주고, 조종권한은 client에게 줘라
            playerInstance.GetComponent<NetworkObject>().SpawnWithOwnership(client);

            index++;
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

        StartCoroutine(ProcessScoreSequence(objName));
    }

    private IEnumerator ProcessScoreSequence(string objName)
    {
        if (objName == "LeftGround")
        {
            nextBallPos = ballSpwnPoints[1];
            Debug.Log("오른쪽 득점");
        }
        else
        {
            nextBallPos = ballSpwnPoints[0];
            Debug.Log("왼쪽 득점");
        }

        // 슬로우 모션 시작 (ClientRpc로 다같이 느려져야 함)
        SetTimeScaleClientRpc(0.4f);

        // 슬로우 모션 상태로 잠시 대기 (리얼타임 기준 1초)
        yield return new WaitForSecondsRealtime(1.2f);

        // timescale 원상복구
        SetTimeScaleClientRpc(1.0f);

        // 다음 게임을 위한 초기화
        if (ballController != null)
        {
            // 화면 까매지는 거

            // 공 잠시 안보이게 시킴
            ballController.SetActiveState(false);

            // 공 위치 리셋
            ballController.ResetBall(nextBallPos.position);

            // 공이 텔레포트 할 시간 
            yield return new WaitForSecondsRealtime(0.2f);

            ballController.SetActiveState(true);
        }
    }

    [ClientRpc]
    private void SetTimeScaleClientRpc(float scale)
    {
        Time.timeScale = scale;

        // FixedDeltaTime도 같이 조절해야 부드러움
        Time.fixedDeltaTime = 0.02f * scale;
    }
}
