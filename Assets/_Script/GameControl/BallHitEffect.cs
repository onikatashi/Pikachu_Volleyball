using System.Collections;
using UnityEngine;

public class BallHitEffect : MonoBehaviour
{
    public static BallHitEffect Instance;

    private SpriteRenderer sr;

    // 실행중인 코루틴
    private Coroutine currentFadeCoroutine;

    [Header("설정")]
    public float duration = 0.4f;                               // 이펙트가 사라지는 시간
    public Vector3 startScale = new Vector3(1.5f, 1.5f, 1f);    // 시작 크기


    private void Awake()
    {
        Instance = this;
        sr = GetComponent<SpriteRenderer>();
        gameObject.SetActive(false);
    }

    public void ShowEffect(Vector2 pos)
    {
        // 위치 이동
        transform.position = pos;

        // 크기 초기화 및 활성화
        transform.localScale = startScale;
        gameObject.SetActive(true);

        // 기존 코루틴 끄고 새로 시작
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }

        currentFadeCoroutine = StartCoroutine(FadeOutCoroutine());
    }

    private IEnumerator FadeOutCoroutine()
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            transform.localScale = Vector2.Lerp(startScale, Vector2.zero, timer / duration);
            yield return null;
        }

        // 다 줄어들면 비활성화
        gameObject.SetActive(false);

        // 변수 비워주기
        currentFadeCoroutine = null;
    }
}
