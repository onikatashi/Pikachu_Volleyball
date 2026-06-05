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
        GameInfo.currentLobbyId = "";
        GameInfo.currentLobbyCode = "";

        // 네트워크 매니저 종료
        NetworkManager.Singleton.Shutdown();

        // 메인 메뉴로 이동
        SceneLoaderManager.Instance.LoadScene("01_MainMenuScene");
    }

    // [호스트]: 클라이언트가 사라졌을 때
    private void HandleClientQuit(ulong disconnectedClientId)
    {
        try
        {
            NetworkManager.Singleton.Shutdown();
            SceneLoaderManager.Instance.LoadScene("02_LobbyScene");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"로비 처리 중 에러: {e.Message}");
        }
    }
}
