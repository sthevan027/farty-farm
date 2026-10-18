using UnityEngine;

public class TopDownCamera : MonoBehaviour
{
    [SerializeField] public Transform target;
    [SerializeField] private float distance = 15f;
    [SerializeField] private float height = 10f;
    [SerializeField] private float angle = 45f;
    [SerializeField] private float smoothDamp = 0.3f;

    private Vector3 velocity = Vector3.zero;
    private Camera cameraComponent;

    private void Start()
    {
        if (target == null)
            target = FindObjectOfType<BoiController>()?.transform;

        cameraComponent = GetComponent<Camera>();
        if (cameraComponent == null)
            cameraComponent = gameObject.AddComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        // Calculate desired position
        Vector3 angleRad = angle * Mathf.Deg2Rad * Vector3.up;
        Vector3 direction = new Vector3(
            Mathf.Sin(angle * Mathf.Deg2Rad),
            Mathf.Tan(45f * Mathf.Deg2Rad),
            Mathf.Cos(angle * Mathf.Deg2Rad)
        ).normalized;

        Vector3 desiredPosition = target.position - direction * distance + Vector3.up * height;

        // Smooth damp to desired position
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothDamp);

        // Look at target slightly above ground
        transform.LookAt(target.position + Vector3.up * 2f);
    }
}
