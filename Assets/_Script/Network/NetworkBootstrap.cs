using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkBootstrap : MonoBehaviour
{
    
    async void Start()
    {
        // UnityService 초기화
        await UnityServices.InitializeAsync();

        Debug.Log("Unity Services Initialized");

        // 익명 로그인 (Lobby / Relay 필수 조건)
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        Debug.Log($"플레이어 ID: { AuthenticationService.Instance.PlayerId}");

        // 여기서 씬 로딩 호출
        SceneLoaderManager.Instance.LoadMainMenuWithLogo();
    }
}
