using Unity.Netcode.Components;
using UnityEngine;

public class ClientNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        // 서버가 권한을 갖는가? -> 아니요: false, 주인이 갖습니다
        return false;
    }
}
