using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class BorderPulse : MonoBehaviour
{
    public float speed = 2f;
    public float minAlpha = 0.2f;
    public float maxAlpha = 1f;
    Image img; Color baseColor;

    void Awake() { img = GetComponent<Image>(); baseColor = img.color; }
    void Update()
    {
        float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;   // 0..1
        float a = Mathf.Lerp(minAlpha, maxAlpha, t);
        img.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
    }
}


