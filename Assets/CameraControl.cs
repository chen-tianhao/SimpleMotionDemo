using UnityEngine;
using UnityEngine.InputSystem;

public class CameraControl : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float lookSpeed = 2f;

    float yaw;
    float pitch;

    void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null || mouse == null)
            return;

        if (mouse.rightButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue();
            yaw += delta.x * lookSpeed;
            pitch -= delta.y * lookSpeed;
            pitch = Mathf.Clamp(pitch, -80f, 80f);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        float h = 0f;
        if (kb.aKey.isPressed) h -= 1f;
        if (kb.dKey.isPressed) h += 1f;

        float v = 0f;
        if (kb.sKey.isPressed) v -= 1f;
        if (kb.wKey.isPressed) v += 1f;

        float up = 0f;
        if (kb.eKey.isPressed) up += 1f;
        if (kb.qKey.isPressed) up -= 1f;

        Vector3 dir = new Vector3(h, up, v).normalized;
        transform.Translate(dir * moveSpeed * Time.deltaTime, Space.Self);
    }
}
