using UnityEngine;

public class Truck : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;

    void Start()
    {
        Debug.Log($"=====> Truck.Start fired at {Time.time:F2}");
    }

    void Update()
    {
        // 沿自身 forward 方向前进
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }
}
