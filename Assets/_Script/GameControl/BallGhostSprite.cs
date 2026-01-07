using System.Collections;
using UnityEngine;

public class BallGhostSprite : MonoBehaviour
{
    private SpriteRenderer sr;
    public float fadeSpeed = 2f;        // 사라지는 속도

    public void Init(Sprite sprite, Vector2 pos, Quaternion rot, Vector2 scale)
    {
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;

        // 공과 같은 레이어/오더 설정
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 3;

        // 반투명하게 시작
        sr.color = new Color(1, 1, 1, 0.6f);

        transform.position = pos;
        transform.rotation = rot;
        transform.localScale = scale;

        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        while (sr.color.a > 0)
        {
            float alpha = sr.color.a - (fadeSpeed * Time.deltaTime);
            sr.color = new Color (sr.color.r, sr.color.g, sr.color.b, alpha);
            yield return null;
        }
        Destroy(gameObject);
    }
}
