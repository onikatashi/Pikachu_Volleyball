using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUIController : MonoBehaviour
{
    [Header("메인 메뉴 UI")]
    public GameObject mainUI;               // 맨 처음 보이는 UI 집합체
    public Button singlePlayButton;         // 싱글 플레이 버튼
    public Button mulitiPlayButton;         // 멀티 플레이 버튼
    public Button settingButton;            // 설정 버튼
    public Button quitButton;               // 뒤로 버튼

    [Header("멀티 플레이 UI")]
    public GameObject multiplayUI;          // 멀티 플레이 UI 집합체
    public Button backButton;               // 뒤로 버튼

    [Header("설정 패널 UI")]
    public GameObject settingPanel;         // 설정창 패널

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameInfo.isSinglePlay = false;

        mainUI.SetActive(true);
        multiplayUI.SetActive(false);

        singlePlayButton.onClick.AddListener(SinglePlay);
        mulitiPlayButton.onClick.AddListener(ShowMultiplayUI);
        settingButton.onClick.AddListener(ShowSettingUI);
        backButton.onClick.AddListener(ShowMainUI);
        quitButton.onClick.AddListener(GameQuit);

        settingPanel.SetActive(false);
    }

    private void SinglePlay()
    {
        SoundManager.Instance.PlaySFX("Select");

        // 싱글 모드 설정
        GameInfo.isSinglePlay = true;
        GameInfo.myNickname = "single";

        // 브금 종료
        if (!string.IsNullOrEmpty(SoundManager.Instance.GetCurrentBGMTitle()))
        {
            SoundManager.Instance.StopBGM();
        }

        // 바로 게임 씬으로 이동
        SceneLoaderManager.Instance.LoadScene(SceneNames.GAME);
    }

    private void ShowMultiplayUI()
    {
        mainUI.SetActive(false);
        multiplayUI.SetActive(true);
    }

    private void ShowMainUI()
    {
        mainUI.SetActive(true);
        multiplayUI.SetActive(false);
    }

    private void ShowSettingUI()
    {
        settingPanel.SetActive(true);
    }

    private void GameQuit()
    {
        // 에디터에서 실행 중일 때, 플레이 중지
#if UNITY_EDITOR

        EditorApplication.isPlaying = false;

        // 빌드된 게임일 때, 실제 종료
#else
        Application.Quit();
#endif
    }
}
