using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class BallMotor : MonoBehaviour
{
    [Min(0f)] public float acceleration = 23f;
    [Min(1f)] public float topSpeed = 8f;
    [Min(0f)] public float rollingTorque = 7f;
    public PlayerElements elements;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (elements == null) elements = GetComponent<PlayerElements>();
    }

    private void FixedUpdate()
    {
        if (Keyboard.current == null) return;
        Vector2 input = Vector2.zero;
        if (Keyboard.current.wKey.isPressed) input.y += 1;
        if (Keyboard.current.sKey.isPressed) input.y -= 1;
        if (Keyboard.current.dKey.isPressed) input.x += 1;
        if (Keyboard.current.aKey.isPressed) input.x -= 1;
        input = Vector2.ClampMagnitude(input, 1f);

        Transform cam = Camera.main != null ? Camera.main.transform : null;
        Vector3 forward = cam != null ? cam.forward : Vector3.forward;
        Vector3 right = cam != null ? cam.right : Vector3.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();
        Vector3 direction = (forward * input.y + right * input.x).normalized;

        float multiplier = elements != null && elements.Current != null
            ? elements.Current.speedMultiplier : 1f;
        rb.AddForce(direction * acceleration * multiplier, ForceMode.Acceleration);
        if (direction.sqrMagnitude > 0.01f)
            rb.AddTorque(Vector3.Cross(Vector3.up, direction) * rollingTorque, ForceMode.Acceleration);

        Vector3 horizontal = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        float limit = topSpeed * multiplier;
        if (horizontal.magnitude > limit)
        {
            horizontal = horizontal.normalized * limit;
            rb.linearVelocity = new Vector3(horizontal.x, rb.linearVelocity.y, horizontal.z);
        }
    }
}
