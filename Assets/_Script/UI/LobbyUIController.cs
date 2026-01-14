using System.Collections;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUIController : MonoBehaviour
{
    [Header("방 정보")]
    public TextMeshProUGUI roomCode;                // 방 코드
    // 추후에 방 제목 생각

    [Header("UI 버튼")]
    public Button readyButton;                      // 준비 버튼
    public Button startButton;                      // 시작 버튼
    public Button leaveButton;                      // 나가기 버튼

    [Header("플레이어 정보 표시")]
    public TextMeshProUGUI hostNickname;            // 호스트 이름
    public TextMeshProUGUI hostReadyState;          // 호스트 준비 상태

    public TextMeshProUGUI clientNickname;          // 클라이언트 이름
    public TextMeshProUGUI clientReadyState;        // 클라이언트 준비 상태

    private bool isLeaving = false;

    private void Start()
    {
        if (roomCode != null)
        {
            roomCode.text = GameInfo.currentLobbyCode;
        }

        // 버튼 연결
        readyButton.onClick.AddListener(OnReadyClicked);
        startButton.onClick.AddListener(OnStartGameClicked);
        leaveButton.onClick.AddListener(OnLeaveClicked);

        // 초기 상태 설정
        startButton.interactable = false;       // 게임 시작 가능할 때 활성화

        if (NetworkManager.Singleton.IsServer)
        {
            // 호스트: 시작버튼, 나가기 버튼
            startButton.gameObject.SetActive(true);
            readyButton.gameObject.SetActive(false);
            leaveButton.gameObject.SetActive(true);

            // 호스트 자동 레디
            StartCoroutine(HostAutoReadyCoroutine());
        }
        else
        {
            // 클라이언트: 준비버튼, 나가기버튼
            startButton.gameObject.SetActive(false);
            readyButton.gameObject.SetActive(true);
            leaveButton.gameObject.SetActive(true);
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        }
    }

    private void Update()
    {
        UpdatePlayerInfo();
    }

    private IEnumerator HostAutoReadyCoroutine()
    {
        // 내 플레이어 오브젝트가 생성될 때까지 대기 (null 이 아닐때까지)
        yield return new WaitUntil(() => NetworkManager.Singleton.LocalClient != null);

        OnReadyClicked();
    }

    // 나가기 버튼 클릭
    async void OnLeaveClicked()
    {
        if (isLeaving) return;

        isLeaving = true;

        SoundManager.Instance.PlaySFX("Menu");

        // GameData에 적힌 ID를 보고 로비 서비스 정리
        await CleanUpLobby();

        // 네트워크 종료
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // 메인 씬 이동
        SceneLoaderManager.Instance.LoadScene("01_MainMenuScene");
    }

    // 로비 청소
    private async Task CleanUpLobby()
    {
        string lobbyId = GameInfo.CurrentLobbyId;
        if (string.IsNullOrEmpty(lobbyId)) return;

        try
        {
            string playerId = AuthenticationService.Instance.PlayerId;

            // 내가 호스트 서버 => 방 삭제
            if (NetworkManager.Singleton.IsServer)
            {
                await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
                Debug.Log("방 삭제");
            }

            // 클라이언트 => 이름만 삭제
            else
            {
                await LobbyService.Instance.RemovePlayerAsync(lobbyId, playerId);
            }
        }
        catch (System.Exception e)
        {
            Debug.Log($"로비 정리 실패: {e.Message}");
        }
        finally
        {
            GameInfo.CurrentLobbyId = "";
            GameInfo.currentLobbyCode = "";
        }
    }

    // 준비 버튼 클릭
    void OnReadyClicked()
    {
        // 내 플레이어 오브젝트(NetworkPlayerState)를 찾아서 상태 변경 요청
        var myPlayer = NetworkManager.Singleton.LocalClient.PlayerObject
            .GetComponent<NetworkPlayerState>();

        if (myPlayer != null)
        {
            SoundManager.Instance.PlaySFX("Menu");
            myPlayer.ToggleReady();
        }
    }

    // 게임 시작 버튼 클릭
    async void OnStartGameClicked()
    {
        SoundManager.Instance.PlaySFX("Select");
        if (NetworkManager.Singleton.IsServer)
        {
            FadeEffectClientRpc();

            int waitTime = (int)(SceneLoaderManager.Instance.fadeDuration * 1000);
            await Task.Delay(waitTime);

            if (!string.IsNullOrEmpty(SoundManager.Instance.GetCurrentBGMTitle()))
            {
                SoundManager.Instance.StopBGM();
            }

            NetworkManager.Singleton.SceneManager.LoadScene
                ("03_GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    // 플레이어 정보를 갱신해서 보여주는 함수
    void UpdatePlayerInfo()
    {
        // 현재 접속된 모든 플레이어 상태 가져오기
        var allPlayers = NetworkPlayerState.allPlayers;

        bool isAllReady = true;
        int playerCount = 0;

        // 텍스트 초기화 (아무도 없을 때를 대비)
        hostNickname.text = "Waiting...";
        hostReadyState.text = "";
        clientNickname.text = "Waiting...";
        clientReadyState.text = "";

        foreach (var p in allPlayers)
        {
            playerCount++;
            if (!p.isReady.Value)
            {
                isAllReady = false;
            }

            // 호스트인지 클라이언트인지 구별 (OwnerClientId == 0 이면 보통 호스트)
            if (p.OwnerClientId == NetworkManager.ServerClientId)
            {
                // 호스트 정보 표시
                hostNickname.text = p.playerName.Value.ToString(); // FixedString -> string 변환
                hostReadyState.text = p.isReady.Value ? "<color=green>READY</color>" : "<color=red>WAIT</color>"; 
            }
            else
            {
                // 클라이언트 정보 표시
                clientNickname.text = p.playerName.Value.ToString();
                clientReadyState.text = p.isReady.Value ? "<color=green>READY</color>" : "<color=red>WAIT</color>";
            }
        }

        // 호스트용 게임 시작 버튼 활성화
        if (NetworkManager.Singleton.IsServer)
        {
            // 플레이어가 2명이고 모두 준비됐으면 활성화
            startButton.interactable = (playerCount == 2 && isAllReady);
        }

    }

    private void OnClientDisconnect(ulong clientId)
    {
        if (!isLeaving)
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                SceneLoaderManager.Instance.LoadScene("01_MainMenuScene");
            }
        }
    }

    [ClientRpc]
    private void FadeEffectClientRpc()
    {
        // 각 클라이언트가 자신의 SceneTransitionManager를 통해 효과 재생
        StartCoroutine(SceneLoaderManager.Instance.FadeInBlackBackground());
    }
}
