using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerState : NetworkBehaviour
{
    // 모든 플레이어를 담아둘 전역 리스트
    public static List<NetworkPlayerState> allPlayers = new List<NetworkPlayerState>();

    // 준비 상태 변수 (동기화 되는 변수)
    public NetworkVariable<bool> isReady = new NetworkVariable<bool>(false);

    // string은 참조 타입이라 GC에 부담, FixedString32Bytes는 구조체 형태의 값 타입이라 성능이 매우 빠름
    // 네트워크 전송 효율과 메모리 최적화
    public NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>();

    public override void OnNetworkSpawn()
    {
        // 리스트에 나 자신을 추가
        allPlayers.Add(this);

        if (IsOwner)
        {
            SetNicknameServerRpc(NicknameInfo.myNickname);
        }
    }

    [ServerRpc]
    private void SetNicknameServerRpc(string nickname)
    {
        playerName.Value = nickname;
    }

    // 준비 버튼 누르면 호출
    public void ToggleReady()
    {
        if (IsOwner)
        {
            ToggleReadyServerRpc();
        }
    }

    [ServerRpc]
    private void ToggleReadyServerRpc()
    {
        isReady.Value = !isReady.Value;
    }

    // 사라질 때 (접속 끊김 등)
    public override void OnNetworkDespawn()
    {
        // 리스트에서 나 자신을 제거
        allPlayers.Remove(this);
    }
}
