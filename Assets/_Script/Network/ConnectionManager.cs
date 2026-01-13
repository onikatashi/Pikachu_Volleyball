using Unity.Netcode;
using Unity.Services.Lobbies;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConnectionManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        // 연결 상태 변경 감지 이벤트 구독
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        }
    }

    // 연결이 끊겼을 때 호출되는 함수
    private void OnClientDisconnect(ulong clientId)
    {
        if (GameInfo.isSinglePlay) return;

        // 내가 클라이언트, 호스트 연결이 끊김
        if (!NetworkManager.Singleton.IsServer && NetworkManager.Singleton.DisconnectReason != string.Empty)
        {
            Debug.Log("호스트가 연결을 끊음");
            HandleHostQuit();
            return;
        }

        if (!NetworkManager.Singleton.IsServer && clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("서버와의 연결이 끊김");
            HandleHostQuit();
            return;
        }

        // 내가 호스트, 클라이언트가 나간 경우
        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log($"클라이언트 {clientId}가 나갔습니다");
            HandleClientQuit(clientId);
        }
    }

    // [클라이언트]: 호스트가 사라졌을 때
    private void HandleHostQuit()
    {
        // 데이터 초기화
        GameInfo.CurrentLobbyId = "";
        GameInfo.currentLobbyCode = "";

        // 네트워크 매니저 종료
        NetworkManager.Singleton.Shutdown();

        // 메인 메뉴로 이동
        SceneLoaderManager.Instance.LoadScene("01_MainMenuScene");
    }

    // [호스트]: 클라이언트가 사라졌을 때
    private async void HandleClientQuit(ulong disconnectedClientId)
    {
        // 로비 서비스에서도 사라진 클라이이언트를 지워줘야 함
        // disconnectedClientId: Netcode ID
        // 실제로는 Lobby PlayerId와 매핑해둔 정보를 써야함
        // 1:1 게임이기 때문에 '내가 아닌 다른 한 명'을 지우는 식으로 처리 가능

        string lobbyId = GameInfo.CurrentLobbyId;

        try
        {
            // 네트워크 세션 정리
            NetworkManager.Singleton.Shutdown();

            // 로비로 이동
            SceneLoaderManager.Instance.LoadScene("02_LobbyScene");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"로비 처리 중 에러: {e.Message}");
        }
    }
}
