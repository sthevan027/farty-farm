using UnityEngine;

public class TopDownCamera : MonoBehaviour
{
    public Transform target;
    [SerializeField] private float distance = 16f;
    [SerializeField] private float pitch = 50f;
    [SerializeField] private float yaw = 0f;
    [SerializeField] private float smoothTime = 0.25f;
    [SerializeField] private float lookHeight = 0.5f;

    private Vector3 velocity;

    private void Start()
    {
        if (target == null)
        {
            BoiController boi = FindFirstObjectByType<BoiController>();
            if (boi != null) target = boi.transform;
        }
        if (target != null) transform.position = DesiredPosition();
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private Vector3 DesiredPosition()
    {
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        return target.position + Vector3.up * lookHeight + rot * Vector3.back * distance;
    }

    private void LateUpdate()
    {
        if (target == null) return;
        transform.position = Vector3.SmoothDamp(transform.position, DesiredPosition(), ref velocity, smoothTime);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}
