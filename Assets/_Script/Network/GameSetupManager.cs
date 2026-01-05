using Unity.Netcode;
using UnityEngine;

public class GameSetupManager : NetworkBehaviour 
{
    [Header("프리팹 설정")]
    public GameObject playerPrefab;     // 실제로 움직일 피카츄 프리팹

    [Header("스폰 위치")]
    public Transform[] spawnPoints;     // 0은 왼쪽, 1은 오른쪽

    public override void OnNetworkSpawn()
    {
        // 플레이어 생성 권한은 호스트(Server)에게만 있음.
        if (IsServer)
        {
            SpawnPlayers();
        }
    }

    private void SpawnPlayers()
    {
        // 현재 접속해 있는 모든 유저들의 ID 목록을 가져옴
        var clients = NetworkManager.Singleton.ConnectedClientsIds;

        int index = 0;
        foreach (var client in clients)
        {
            // 사람이 스폰 포인트보다 많으면 안됨
            if (index >= spawnPoints.Length) break;

            // 위치 선정
            Transform spawnTransform = spawnPoints[index];

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
}
