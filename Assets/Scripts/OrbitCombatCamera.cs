using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class OrbitCombatCamera : MonoBehaviour
{
    public Transform target;
    [Range(2f, 12f)] public float distance = 5.5f;
    public float focusHeight = 0.8f;
    public float mouseSensitivity = 0.12f;
    public float smoothing = 12f;
    public float pitch = 20f;
    public float yaw = 0f;
    private float cinematicOffset;
    private Coroutine slamSequence;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (target == null) return;
        if (Cursor.lockState == CursorLockMode.Locked && Mouse.current != null)
        {
            Vector2 movement = Mouse.current.delta.ReadValue();
            yaw += movement.x * mouseSensitivity;
            pitch = Mathf.Clamp(pitch - movement.y * mouseSensitivity, -15f, 65f);
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 focus = target.position + Vector3.up * focusHeight;
        Vector3 destination = focus - rotation * Vector3.forward * distance +
                              Vector3.up * cinematicOffset;
        float lerp = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
        transform.position = Vector3.Lerp(transform.position, destination, lerp);
        transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
    }

    public void PlaySlamBeat()
    {
        if (slamSequence != null) StopCoroutine(slamSequence);
        slamSequence = StartCoroutine(SlamBeat());
    }

    private IEnumerator SlamBeat()
    {
        float duration = 0.65f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Rise, then dive below the usual camera height, then reset.
            cinematicOffset = t < 0.35f
                ? Mathf.Lerp(0f, 1.8f, t / 0.35f)
                : t < 0.75f
                    ? Mathf.Lerp(1.8f, -0.45f, (t - 0.35f) / 0.40f)
                    : Mathf.Lerp(-0.45f, 0f, (t - 0.75f) / 0.25f);
            yield return null;
        }
        cinematicOffset = 0f;
        slamSequence = null;
    }
}
