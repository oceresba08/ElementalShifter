using UnityEngine;
[RequireComponent(typeof(Collider))]
public class WindCurrentZone : MonoBehaviour
{
    public Vector3 windDirection = Vector3.right;
    public float windAcceleration = 14f;
    public bool stormOnly = false;
    void Awake() { GetComponent<Collider>().isTrigger = true; }
    void OnTriggerStay(Collider other)
    {
        if (stormOnly && (WeatherManager.Instance == null || WeatherManager.Instance.Current != WeatherType.Storm)) return;
        Rigidbody body = other.attachedRigidbody;
        if (body != null && !body.isKinematic)
            body.AddForce(windDirection.normalized * windAcceleration, ForceMode.Acceleration);
        // NavMeshAgents are not Rigidbodies: they need a separate push integration later.
    }
}
