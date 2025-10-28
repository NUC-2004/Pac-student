using UnityEngine;

public class CherryController : MonoBehaviour
{
    Vector3 start, end;
    float speed;

    public void Init(Vector3 s, Vector3 e, float spd)
    {
        start = s; end = e; speed = spd;
        transform.position = s;
    }

    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, end, speed * Time.deltaTime);
        if ((transform.position - end).sqrMagnitude < 0.0001f)
            Destroy(gameObject);
    }
}
