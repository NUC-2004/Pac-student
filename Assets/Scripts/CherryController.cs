using UnityEngine;

public class CherryController : MonoBehaviour
{
    [Header("Score")]
    public int points = 100;

    [Header("Motion")]
    public float speed = 3f;        // 仅当未通过 Init 传入时作为默认速度
    public float offscreenMargin = 1.0f; // 超出屏幕多少距离后强制销毁（单位：世界坐标）

    // 运行期
    Vector3 startPos;
    Vector3 endPos;
    bool inited = false;

    /// <summary>
    /// 由 Spawner 调用，设定起点/终点与速度（线性移动）
    /// </summary>
    public void Init(Vector3 start, Vector3 end, float moveSpeed)
    {
        startPos = start;
        endPos = end;
        speed = moveSpeed;
        transform.position = startPos;
        inited = true;
    }

    void Update()
    {
        if (!inited) return; // 等待 Spawner 调用 Init

        // 线性位移到终点
        transform.position = Vector3.MoveTowards(transform.position, endPos, speed * Time.deltaTime);

        // 到达终点：额外再判定一下是否已经越过屏幕边界 -> 销毁
        if (Vector3.SqrMagnitude(transform.position - endPos) < 0.0001f)
        {
            // 终点一般在屏幕外，直接销毁；或再加一道保险：超出相机范围则销毁
            if (IsOutsideCameraBounds(offscreenMargin))
                Destroy(gameObject);
        }
        else
        {
            // 途中如果已经飞出相机视口很远，也销毁（双保险，防止卡住）
            if (IsOutsideCameraBounds(offscreenMargin))
                Destroy(gameObject);
        }
    }

    // 被玩家吃到：加分并销毁
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        ScoreKeeper.I?.AddScore(points);
        Destroy(gameObject);
    }

    // 判定是否在主相机视口外（含 margin）
    bool IsOutsideCameraBounds(float margin)
    {
        Camera cam = Camera.main;
        if (!cam) return false;

        // 计算世界边界
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        Vector3 c = cam.transform.position;
        float minX = c.x - halfW - margin;
        float maxX = c.x + halfW + margin;
        float minY = c.y - halfH - margin;
        float maxY = c.y + halfH + margin;

        Vector3 p = transform.position;
        return (p.x < minX || p.x > maxX || p.y < minY || p.y > maxY);
    }
}

