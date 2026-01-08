using System.Collections;
using UnityEngine;

public class CloudFlow : MonoBehaviour
{
    public float minSpeed, maxSpeed;
    public float resetX;
    public float startX;
    public float minY, maxY;

    private float flowSpeed;

    private float scaleMulitplier;
    private float duration;

    private Vector2 originalScale;
    private Coroutine currentScaleCoroutine;

    void Start()
    {
        scaleMulitplier = Random.Range(1.3f, 1.8f);
        duration = Random.Range(0.2f, 0.5f);
        originalScale = transform.localScale;
        flowSpeed = Random.Range(minSpeed, maxSpeed);
        transform.position = new Vector2(Random.Range(startX, resetX), Random.Range(minY, maxY));
    }

    void Update()
    {
        // 오른쪽으로 이동
        transform.Translate(Vector2.right * flowSpeed * Time.deltaTime);

        // 맵 끝에 가면 다시 왼쪽으로 이동하여 재활용
        if (transform.position.x > resetX)
        {
            ResetCloudPosition();
        }

        if (currentScaleCoroutine == null)
        {
            currentScaleCoroutine = StartCoroutine(ScaleCoroutine());
        }
    }

    private void ResetCloudPosition()
    {
        // 위치 리셋
        Vector2 pos = transform.position;
        pos.x = startX;
        pos.y = Random.Range(minY, maxY);
        transform.position = pos;
        flowSpeed = Random.Range(minSpeed, maxSpeed);
    }

    private IEnumerator ScaleCoroutine()
    {
        Vector2 targetScale = originalScale * scaleMulitplier;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector2.Lerp(originalScale, targetScale, elapsed / duration);
            yield return null;
        }

        // 다시 돌아오는 단계
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector2.Lerp(targetScale, originalScale, elapsed / duration);
            yield return null;
        }

        transform.localScale = originalScale;
        currentScaleCoroutine = null;
    }
}
