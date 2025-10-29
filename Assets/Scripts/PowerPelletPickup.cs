using UnityEngine;

/// <summary>
/// 大力丸：玩家触发 → 统一交给 GameRule70.OnPowerPelletEaten()
/// 注意：OnPowerPelletEaten() 内已负责 +50 分、受惊状态与清关 -1
/// </summary>
public class PowerPelletPickup : MonoBehaviour
{
    [Tooltip("显示用途（实际加分在 GameRule70.OnPowerPelletEaten 内完成）")]
    public int points = 50;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // 统一入口：内部已处理加分、受惊、以及清关 -1
        GameRule70.I?.OnPowerPelletEaten();

        // 销毁大力丸本体
        Destroy(gameObject);
    }
}
