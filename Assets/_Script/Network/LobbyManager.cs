using System.Collections.Generic;
using TMPro;
using TMPro.Examples;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    [Header("방 코드 InputField")]
    public TMP_InputField joinCode;         // 방 코드 입력 필드

    private const int MAX_PLAYER = 2;   // 최대 2인
    private Lobby currentLobby;

    // 방 만들기 (Host)
    public async void CreateLobby()
    {
        try
        {
            // Relay 방 생성 (Allocation)
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MAX_PLAYER);
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log($"Relay 방 생성 완료, Join Code: {relayJoinCode}");

            // 로비 생성
            CreateLobbyOptions options = new CreateLobbyOptions();
            options.Data = new Dictionary<string, DataObject>
            {
                // 클라이언트가 접속할 때 필요한 Relay 코드를 로비 데이터에 숨겨둠
                { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
            };

            // "PikachuLobby"라는 이름으로 로비 생성
            currentLobby = await LobbyService.Instance.CreateLobbyAsync("PikachuLobby", MAX_PLAYER, options);

            Debug.Log($"로비 생성 완료. Lobby Code: {currentLobby.LobbyCode}");

            // Host로 게임 시작 (Relay 연결 설정)
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
                );

            // 호스트 시작
            NetworkManager.Singleton.StartHost();

            // 로비 씬으로 이동 (Netcode의 씬 전환 기능 사용)
            NetworkManager.Singleton.SceneManager.LoadScene("02_LobbyScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"방 만들기 실패: {e}");
        }
    }

    // 방 입장하기 (Client)
    public async void JoinLobby()
    {
        // 입력한 코드 가져오기
        string lobbyCode = joinCode.text;
        if (string.IsNullOrEmpty(lobbyCode))
        {
            return;
        }

        try
        {
            // 코드로 로비 찾기
            currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
            Debug.Log($"로비 입장 성공. 로비 ID {currentLobby.Id}");

            // 로비 데이터에서 Relay 코드 꺼내기
            string relayJoinCode = currentLobby.Data["RelayJoinCode"].Value;

            // Relay 연결 준비
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
                );

            // Client로 게임 시작
            NetworkManager.Singleton.StartClient();
        }

        catch (System.Exception e)
        {
            Debug.LogError($"방 입장 실패: {e}");
        }
    }
}
