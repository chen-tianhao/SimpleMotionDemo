using UnityEngine;

public class MoveAtConstantSpeed : MonoBehaviour
{
    public Vector3 velocity = new Vector3(1f, 0f, 0f);

    void Update()
    {
        transform.position += velocity * Time.deltaTime;
    }
}
