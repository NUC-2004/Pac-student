using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class DottedBorderScroll : MonoBehaviour
{
    public float pixelsPerDot = 32f;   // 点之间的间距（像素）
    public float speed = 50f;          // 沿边移动速度（像素/秒）
    public bool horizontal = true;     // 横向条带 true；纵向 false

    RawImage img;
    RectTransform rt;

    void Awake() { img = GetComponent<RawImage>(); rt = GetComponent<RectTransform>(); SetupTiling(); }
    void OnEnable() => SetupTiling();
    void OnRectTransformDimensionsChange() => SetupTiling();

    void SetupTiling()
    {
        if (!img || rt.rect.width <= 0 || rt.rect.height <= 0) return;
        if (horizontal)
        {
            float tiles = Mathf.Max(1f, rt.rect.width / pixelsPerDot);
            img.uvRect = new Rect(img.uvRect.x, 0f, tiles, 1f);
        }
        else
        {
            float tiles = Mathf.Max(1f, rt.rect.height / pixelsPerDot);
            img.uvRect = new Rect(0f, img.uvRect.y, 1f, tiles);
        }
    }

    void Update()
    {
        var uv = img.uvRect;
        float delta = (speed * Time.unscaledDeltaTime) / pixelsPerDot;
        if (horizontal) uv.x += delta; else uv.y += delta;
        img.uvRect = uv;
    }
}
