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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mainUI.SetActive(true);
        multiplayUI.SetActive(false);

        mulitiPlayButton.onClick.AddListener(ShowMultiplayUI);
        backButton.onClick.AddListener(ShowMainUI);
    }

    // Update is called once per frame
    void Update()
    {
        
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
}
