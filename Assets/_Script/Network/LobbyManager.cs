using System.Collections.Generic;
using System.Threading.Tasks;
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
    [Header("닉네임 입력창")]
    public TMP_InputField nickname;

    [Header("방 코드 InputField")]
    public TMP_InputField joinCode;         // 방 코드 입력 필드

    [Header("에러 팝업 UI")]
    public GameObject errorPopupPanel;      // 에러 떴을 때 켤 패널
    public TextMeshProUGUI errorText;       // 에러 텍스트
    public Button errorConfirmButton;       // 에러 패널 종료(확인) 버튼

    private const int MAX_PLAYER = 2;   // 최대 2인
    private Lobby currentLobby;

    private void Start()
    {
        // 3번째 플레이어 접속을 막기 위한 승인 로직 등록
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
        }
        errorConfirmButton.onClick.AddListener(CloseErrorPopup);
        errorPopupPanel.SetActive(false);
    }

    // 접속 승인 검사 함수 (호스트에서 실행)
    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        if (NetworkManager.Singleton.ConnectedClientsIds.Count >= MAX_PLAYER)
        {
            response.Approved = false;
            response.Reason = "Room is full";
            Debug.Log("인원 초과로 접속 거부");
        }
        else
        {
            response.Approved = true;
        }
        response.Pending = false;
    }

    // 방 만들기 (Host)
    public async void CreateLobby()
    {
        try
        {
            // 닉네임 저장
            SaveNickname();

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
            GameInfo.currentLobbyCode = currentLobby.LobbyCode;
            GameInfo.currentLobbyId = currentLobby.Id;

            // Host로 게임 시작 (Relay 연결 설정)
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
                );

            // 씬 로드 전에 화면 까맣게 만들기
            StartCoroutine(SceneLoaderManager.Instance.FadeInBlackBackground());

            // 페이드 시간만큼 대기
            // SceneLoaderManager의 fadeDuration(초)을 밀리초(1000 곱하기)로 변환
            int waitTime = (int)(SceneLoaderManager.Instance.fadeDuration * 1000);
            await Task.Delay(waitTime);

            // 호스트 시작
            NetworkManager.Singleton.StartHost();

            // 로비 씬으로 이동 (Netcode의 씬 전환 기능 사용)
            NetworkManager.Singleton.SceneManager.LoadScene("02_LobbyScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"방 만들기 실패: {e}");
            ShowError("로비 생성 실패!");
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"릴레이 생성 실패: {e}");
            ShowError("릴레이 생성 실패!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"알수 없는 에러: {e}");
            ShowError("알 수 없는 오류가 발생했습니다.");
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
            // 닉네임 저장
            SaveNickname();

            // 코드로 로비 찾기
            currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
            Debug.Log($"로비 입장 성공. 로비 ID {currentLobby.Id}");
            GameInfo.currentLobbyCode = currentLobby.LobbyCode;
            GameInfo.currentLobbyId = currentLobby.Id;

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

            // 씬 로드 전에 화면 까맣게 만들기
            StartCoroutine(SceneLoaderManager.Instance.FadeInBlackBackground());

            // 페이드 시간만큼 대기
            int waitTime = (int)(SceneLoaderManager.Instance.fadeDuration * 1000);
            await Task.Delay(waitTime);

            // Client로 게임 시작
            NetworkManager.Singleton.StartClient();
        }

        catch (LobbyServiceException e)
        {
            Debug.LogError($"방 입장 실패: {e}");
            if (e.Reason == LobbyExceptionReason.LobbyFull)
            {
                ShowError("방이 꽉 찼습니다!");
            }
            else if (e.Reason == LobbyExceptionReason.LobbyNotFound)
            {
                ShowError("존재하지 않는 방 코드입니다.");
            }
            else
            {
                ShowError("로비 입장 실패");
            }
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
            ShowError("서버 연결 실패");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"방 입장 실패: {e}");
            ShowError("알 수 없는 오류로 입장할 수 없습니다.");
        }
    }

    // 플레이어 이름 저장
    public void SaveNickname()
    {
        if (!string.IsNullOrEmpty(nickname.text))
        {
            GameInfo.myNickname = nickname.text;
        }
        else
        {
            GameInfo.myNickname = "Player";
        }
    }

    // 에러 메세지를 띄우는 함수
    private void ShowError(string message)
    {
        if (errorPopupPanel != null)
        {
            errorPopupPanel.SetActive(true);
            if (errorText != null)
            {
                errorText.text = message;
            }
        }
        else
        {
            Debug.LogWarning("에러 팝업 UI가 연결되지 않음");
        }
    }

    // 팝업 닫기 버튼
    public void CloseErrorPopup()
    {
        if (errorPopupPanel != null)
        {
            errorPopupPanel.SetActive(false);
        }
    }
}
