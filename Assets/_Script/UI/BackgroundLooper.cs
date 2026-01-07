using UnityEngine;
using UnityEngine.UI;

public class BackgroundLooper : MonoBehaviour
{
    [Header("이동 속도")]
    public Vector2 scrollSpeed = new Vector2(100f, 100f);

    private RectTransform rectTransform;
    private float width;
    private float height;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        Image img = GetComponent<Image>();
        if (img.sprite != null)
        {
            width = img.sprite.rect.width;
            height = img.sprite.rect.height;
        }
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 newPos = rectTransform.anchoredPosition + (scrollSpeed * Time.deltaTime);

        newPos.x = Mathf.Repeat(newPos.x, width);
        newPos.y = Mathf.Repeat(newPos.y, height);

        rectTransform.anchoredPosition += scrollSpeed * Time.deltaTime;

        if (scrollSpeed.x < 0 && rectTransform.anchoredPosition.x >= width)
        {
            rectTransform.anchoredPosition += new Vector2(width, 0);
        }
        if (scrollSpeed.y > 0 && rectTransform.anchoredPosition.y <= -height)
        {
            rectTransform.anchoredPosition -= new Vector2(0, height);
        }
    }
}
