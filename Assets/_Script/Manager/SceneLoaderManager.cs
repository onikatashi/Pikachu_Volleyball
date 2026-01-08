using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoaderManager : MonoBehaviour
{
    public SceneLoaderManager Instance;

    [Header("UI 연결")]
    public CanvasGroup fadeCanvasGroup;    // 검은 화면 패널의 CanvasGroup
    public float fadeDuration = 0.5f;       // 페이드 되는 시간

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
    }
    
    // 로컬에서 단순히 씬만 이동할 때
    public void LoadSceneLocal(string sceneName)
    {
        StartCoroutine(LoadSceneLoaclCoroutine(sceneName));
    }

    IEnumerator LoadSceneLoaclCoroutine(string sceneName)
    {
        // 화면 어두워짐
        yield return StartCoroutine(Fade(1f));

        // 씬 로딩
        SceneManager.LoadScene(sceneName);

        // 로딩이 끝날 때까지 대기
        yield return null;

        // 화면 밝아짐
        yield return StartCoroutine(Fade(0f));
    }

    // 페이드 효과 (Alpha 0 ~ 1)
    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = fadeCanvasGroup.alpha;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = targetAlpha;
    }

    // 서버에서 씬 전환을 할 때
    public void LoadNetworkScene(string sceneName)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            
        }
    }
}
