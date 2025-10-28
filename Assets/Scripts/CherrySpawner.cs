using System.Collections;
using UnityEngine;

public class CherrySpawner : MonoBehaviour
{
    public CherryController cherryPrefab; // 拖 Project 里的 prefab（蓝色小方块）
    public Transform levelCenter;         // 可空；为空就用(0,0)

    [Header("Timing")]
    public float firstDelay = 5f;
    public float respawnDelay = 5f;

    [Header("Motion")]
    public float cherrySpeed = 3f;
    public float margin = 1.5f;           // 出生/终点超出屏幕的余量

    void Start()
    {
        StartCoroutine(Loop());
    }

    IEnumerator Loop()
    {
        yield return new WaitForSeconds(firstDelay);

        while (true)
        {
            SpawnOneCherry();

            // 等这一颗被销毁（飞出屏幕或被吃）
            yield return new WaitUntil(() => FindObjectOfType<CherryController>() == null);
            yield return new WaitForSeconds(respawnDelay);
        }
    }

    void SpawnOneCherry()
    {
        Camera cam = Camera.main;
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        Vector3 c = levelCenter ? levelCenter.position : Vector3.zero; // 关卡中心

        // 先在四条边之一随机一个“起点”
        int side = Random.Range(0, 4);
        Vector3 start = c;

        switch (side)
        {
            case 0: // 左边
                start = new Vector3(c.x - halfW - margin, Random.Range(c.y - halfH, c.y + halfH), 0);
                break;
            case 1: // 右边
                start = new Vector3(c.x + halfW + margin, Random.Range(c.y - halfH, c.y + halfH), 0);
                break;
            case 2: // 下边
                start = new Vector3(Random.Range(c.x - halfW, c.x + halfW), c.y - halfH - margin, 0);
                break;
            default: // 上边
                start = new Vector3(Random.Range(c.x - halfW, c.x + halfW), c.y + halfH + margin, 0);
                break;
        }

        // 终点 = 关于中心 c 的镜像点 => 一定经过中心
        Vector3 end = 2f * c - start;

        var inst = Instantiate(cherryPrefab, start, Quaternion.identity);
        inst.Init(start, end, cherrySpeed);

    }
}
