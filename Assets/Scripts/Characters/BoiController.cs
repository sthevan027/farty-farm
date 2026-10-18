using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoiController : MonoBehaviour
{
    [Header("Movimento")]
    [SerializeField] private float acceleration = 22f;
    [SerializeField] private float maxSpeed = 14f;
    [Tooltip("Taxa de atrito horizontal por segundo (independente de FPS)")]
    [SerializeField] private float friction = 1.2f;
    [SerializeField] private float turnSpeed = 8f;

    [Header("Peido")]
    [SerializeField] private float fartImpulse = 9f;
    [SerializeField] private float gasCapacity = 100f;
    [SerializeField] private float gasPerFart = 20f;
    [SerializeField] private float gasRegenRate = 12f;
    [SerializeField] private float fartCooldown = 0.3f;

    [Header("Referencias (preenchidas pelo SceneSetup)")]
    [SerializeField] private Transform eyeLeft;
    [SerializeField] private Transform eyeRight;
    [SerializeField] private ParticleSystem fartParticles;

    private Rigidbody rb;
    private Transform cam;
    private Vector3 moveInput;
    private float currentGas;
    private float lastFartTime = -10f;
    private float noGasMessageTime = -10f;
    private Vector3 eyeLeftBase;
    private Vector3 eyeRightBase;
    private float eyeFactor = 1f;

    public float GasPercent => currentGas / gasCapacity;
    public float Speed => rb != null ? new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude : 0f;
    public bool OutOfGas => currentGas < gasPerFart;
    public float NoGasMessageTime => noGasMessageTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        currentGas = gasCapacity;
    }

    private void Start()
    {
        if (Camera.main != null) cam = Camera.main.transform;
        if (eyeLeft == null) eyeLeft = transform.Find("Head/EyeLeft");
        if (eyeRight == null) eyeRight = transform.Find("Head/EyeRight");
        if (fartParticles == null) fartParticles = GetComponentInChildren<ParticleSystem>();
        if (eyeLeft != null) eyeLeftBase = eyeLeft.localScale;
        if (eyeRight != null) eyeRightBase = eyeRight.localScale;
    }

    private void Update()
    {
        ReadInput();
        UpdateEyes();
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        if (moveInput.sqrMagnitude > 0.01f)
        {
            rb.AddForce(moveInput * acceleration, ForceMode.Acceleration);
            Quaternion target = Quaternion.LookRotation(moveInput, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, target, turnSpeed * dt));
        }

        Vector3 v = rb.velocity;
        Vector3 horizontal = new Vector3(v.x, 0f, v.z);
        horizontal *= Mathf.Exp(-friction * dt);
        if (horizontal.magnitude > maxSpeed) horizontal = horizontal.normalized * maxSpeed;
        rb.velocity = new Vector3(horizontal.x, v.y, horizontal.z);

        currentGas = Mathf.Min(gasCapacity, currentGas + gasRegenRate * dt);
    }

    private void ReadInput()
    {
        float x = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
        float z = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);

        Vector3 forward = cam != null ? cam.forward : Vector3.forward;
        Vector3 right = cam != null ? cam.right : Vector3.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        moveInput = (forward * z + right * x);
        if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();

        if (Input.GetKeyDown(KeyCode.Space)) TryFart();
    }

    private void TryFart()
    {
        if (Time.time - lastFartTime < fartCooldown) return;

        if (currentGas < gasPerFart)
        {
            noGasMessageTime = Time.time;
            return;
        }

        // O gas sai por tras; o impulso empurra o boi para a frente (acao e reacao).
        Vector3 forward = transform.forward;
        forward.y = 0f;
        rb.AddForce(forward.normalized * fartImpulse, ForceMode.Impulse);

        currentGas -= gasPerFart;
        lastFartTime = Time.time;

        if (fartParticles != null) fartParticles.Play();
    }

    private void UpdateEyes()
    {
        float target = 1f;
        if (Time.time - lastFartTime < 0.25f) target = 1.8f;
        else if (OutOfGas) target = 0.8f;

        eyeFactor = Mathf.Lerp(eyeFactor, target, 15f * Time.deltaTime);
        if (eyeLeft != null) eyeLeft.localScale = eyeLeftBase * eyeFactor;
        if (eyeRight != null) eyeRight.localScale = eyeRightBase * eyeFactor;
    }
}
