using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class OrbitCombatCamera : MonoBehaviour
{
    public Transform target;
    [Header("Close third-person framing")]
    public float distance = 3.2f;
    public float shoulderOffset = 0.65f;
    public float focusHeight = 0.65f;
    public float mouseSensitivity = 0.12f;
    public float smoothing = 14f;
    public float pitch = 16f;
    public float yaw;
    [Header("Combat cinema")]
    [Range(0, 1)] public float cameraIntensity = 0.8f;
    public float baseFOV = 62f;
    public float attackFOV = 72f;
    public float impactShake = 0.20f;
    public float collisionRadius = 0.22f;
    public LayerMask collisionMask = ~0;

    Camera cam;
    public float slamFollowSmoothing = 25f;
    bool followingSlam;
    float fovKick;
    float shakeAmount;
    float shakeTime;
    Coroutine beat;

    private void Awake() { cam = GetComponent<Camera>(); }
    private void Start() { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }

    private void LateUpdate()
    {
        if (target == null) return;
        if (Cursor.lockState == CursorLockMode.Locked && Mouse.current != null)
        {
            Vector2 m = Mouse.current.delta.ReadValue();
            yaw += m.x * mouseSensitivity;
            pitch = Mathf.Clamp(pitch - m.y * mouseSensitivity, -20f, 65f);
        }
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
        Vector3 focus = target.position + Vector3.up * focusHeight;
        Vector3 side = rot * Vector3.right * shoulderOffset;
        Vector3 desired = focus + side - rot * Vector3.forward * distance;
        // Prevent walls from blocking the character; exclude player's layer in collisionMask.
        Vector3 from = focus + side;
        Vector3 ray = desired - from;
        if (Physics.SphereCast(from, collisionRadius, ray.normalized, out RaycastHit wall,
            ray.magnitude, collisionMask, QueryTriggerInteraction.Ignore))
            desired = from + ray.normalized * Mathf.Max(0.35f, wall.distance - 0.08f);
        float k = 1f - Mathf.Exp(-(followingSlam ? slamFollowSmoothing : smoothing) * Time.unscaledDeltaTime);
        transform.position = Vector3.Lerp(transform.position, desired, k);
        Vector3 look = focus + side * 0.28f - transform.position;
        if (look.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(look);
        fovKick = Mathf.MoveTowards(fovKick, 0, Time.unscaledDeltaTime * 45f);
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, baseFOV + fovKick * cameraIntensity, k);
        if (shakeTime > 0)
        {
            shakeTime -= Time.unscaledDeltaTime;
            transform.position += Random.insideUnitSphere * shakeAmount * cameraIntensity;
            shakeAmount = Mathf.MoveTowards(shakeAmount, 0f, Time.unscaledDeltaTime);
        }
    }

    public void AttackBeat(float magnitude = 0.5f)
    {
        fovKick = (attackFOV - baseFOV) * Mathf.Clamp01(magnitude);
        Shake(impactShake * magnitude, 0.13f);
    }
    public void Shake(float amount, float duration)
    {
        shakeAmount = Mathf.Max(shakeAmount, amount);
        shakeTime = Mathf.Max(shakeTime, duration);
    }
    // PlayerCombat controls the actual jump; camera simply tracks the ball.
    public void BeginSlamFollow() { followingSlam = true; }
    public void EndSlamFollow()
    {
        followingSlam = false;
        Shake(impactShake * 0.55f, 0.10f);
    }
    // Kept for compatibility with older scripts, but no camera rise/dive.
    public void PlaySlamBeat() { BeginSlamFollow(); }
}
