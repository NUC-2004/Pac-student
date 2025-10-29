using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 左上角心形生命显示：根据生命值开启/关闭若干个 Image。
/// 用法：把脚本挂在 LivesPanel（Life_1 / Life_2 / Life_3 的父物体）上即可。
/// 不手动指定 hearts 时，会自动收集子物体里的 Image，按名字排序（Life_1, Life_2…）。
/// </summary>
public class LivesHeartsUI : MonoBehaviour
{
    [Tooltip("可手动把心形 Image 拖进来（从左到右）。留空则自动收集子物体的 Image。")]
    public List<Image> hearts = new List<Image>();

    [Tooltip("自动收集时，用这些前缀过滤子物体名，避免把其他图也算进来。")]
    public string[] namePrefixes = new[] { "Life", "Heart", "HP" };

    void Awake()
    {
        if (hearts == null || hearts.Count == 0)
        {
            hearts = GetComponentsInChildren<Image>(true)
                .Where(img =>
                {
                    string n = img.gameObject.name;
                    for (int i = 0; i < namePrefixes.Length; i++)
                        if (n.StartsWith(namePrefixes[i])) return true;
                    return false;
                })
                .OrderBy(img => img.gameObject.name, System.StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    /// <summary>设置生命值（0..N），前 n 颗显示，其余隐藏。</summary>
    public void SetLives(int lives)
    {
        if (hearts == null || hearts.Count == 0) return;
        int clamped = Mathf.Clamp(lives, 0, hearts.Count);
        for (int i = 0; i < hearts.Count; i++)
        {
            if (hearts[i]) hearts[i].gameObject.SetActive(i < clamped);
        }
    }
}
