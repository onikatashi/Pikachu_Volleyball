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

        // 익명 로그인 (Lobby / Relay 필수 조건)
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        Debug.Log($"플레이어 ID: { AuthenticationService.Instance.PlayerId}");

        // 메인 메뉴 씬으로 이동
        SceneManager.LoadScene("MainMenuScene");
    }
}
