using NUnit.Framework.Constraints;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoaderManager : MonoBehaviour
{
    public static SceneLoaderManager Instance;

    [Header("UI 연결")]
    public CanvasGroup fadePanel;       // 검은 화면 패널
    public CanvasGroup logoImage;       // 로고 이미지

    [Header("설정")]
    public float fadeDuration = 0.8f;   // 페이드 시간
    public float logoDuration = 2.0f;   // 로고 떠 있는 시간

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // 씬 로드될 때마다 호출되는 이벤트 연결
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 로고 이미지는 꺼야함
        logoImage.alpha = 0f;
        logoImage.gameObject.SetActive(false);

        if (SceneManager.GetActiveScene().name != "00_BootScene")
        {
            // 검은 화면이 덮여있으면 걷어내기
            StartCoroutine(FadeOutBlackBackground());
        }
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneSequence(sceneName));
    }

    private IEnumerator LoadSceneSequence(string sceneName)
    {
        yield return StartCoroutine(Fade(fadePanel, 0f, 1f));

        SceneManager.LoadScene(sceneName);
    }

    // 로고 페이드 인 아웃
    public void LoadMainMenuWithLogo()
    {
        StopAllCoroutines();

        StartCoroutine(LogoFadeCoroutine());
    }

    private IEnumerator LogoFadeCoroutine()
    {
        //fadePanel.gameObject.SetActive(true);
        //fadePanel.alpha = 1f;
        logoImage.gameObject.SetActive(true);
        logoImage.alpha = 0f;

        // 로고 페이드 인
        yield return StartCoroutine(Fade(logoImage, 0f, 1f));

        // 로고 유지
        yield return new WaitForSeconds(logoDuration);

        // 로고 페이드 아웃
        yield return StartCoroutine(Fade(logoImage, 1f, 0f));

        logoImage.gameObject.SetActive(false);

        SceneManager.LoadScene("01_MainMenuScene");
    }

    // 씬 로드 직전에 호출
    public IEnumerator FadeInBlackBackground()
    {
        yield return StartCoroutine(Fade(fadePanel, 0f, 1f));
    }

    public IEnumerator FadeOutBlackBackground()
    {
        yield return StartCoroutine(Fade(fadePanel, 1f, 0f));
    }

    private IEnumerator Fade(CanvasGroup cg, float start, float end)
    {
        cg.gameObject.SetActive(true);

        float timer = 0f;
        cg.alpha = start;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, end, timer / fadeDuration);
            yield return null;
        }
        cg.alpha = end;
        
        if(end == 0f)
        {
            cg.gameObject.SetActive(false);
        }
    }
}
