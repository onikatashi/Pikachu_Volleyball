using TMPro;
using Unity.Netcode;
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

    [Header("플레이어 정보 표시")]
    public TextMeshProUGUI hostNickname;            // 호스트 이름
    public TextMeshProUGUI hostReadyState;          // 호스트 준비 상태

    public TextMeshProUGUI clientNickname;          // 클라이언트 이름
    public TextMeshProUGUI clientReadyState;        // 클라이언트 준비 상태

    private void Start()
    {
        if (roomCode != null)
        {
            roomCode.text = GameInfo.currentLobbyCode;
        }

        // 버튼 연결
        readyButton.onClick.AddListener(OnReadyClicked);
        startButton.onClick.AddListener(OnStartGameClicked);

        // 초기 상태 설정
        startButton.interactable = false;       // 게임 시작 가능할 때 활성화
        if (!NetworkManager.Singleton.IsServer)
        {
            // 호스트가 아니면 게임 시작 버튼 숨기기
            startButton.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        UpdatePlayerInfo();
    }

    // 준비 버튼 클릭
    void OnReadyClicked()
    {
        // 내 플레이어 오브젝트(NetworkPlayerState)를 찾아서 상태 변경 요청
        var myPlayer = NetworkManager.Singleton.LocalClient.PlayerObject
            .GetComponent<NetworkPlayerState>();

        if (myPlayer != null)
        {
            myPlayer.ToggleReady();
        }
    }

    // 게임 시작 버튼 클릭
    void OnStartGameClicked()
    {
        if (NetworkManager.Singleton.IsServer)
        {
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
}
