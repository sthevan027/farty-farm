using UnityEngine;

public enum CharacterType { Vaca = 0, Boi = 1 }

[RequireComponent(typeof(Rigidbody))]
public abstract class CharacterBase : MonoBehaviour
{
    [Header("Gas (mesmo reservatorio para peido e leite)")]
    [SerializeField] protected float gasCapacity = 100f;
    [SerializeField] protected float gasCost = 25f;
    [SerializeField] protected float gasRegenRate = 15f;
    [SerializeField] protected float actionCooldown = 0.3f;

    [SerializeField] private bool isPlayer;

    protected Rigidbody rb;
    protected Transform cam;

    private float currentGas;
    private float lastActionTime = -10f;
    private float noGasMessageTime = -10f;
    private Transform eyeLeft, eyeRight;
    private Vector3 eyeLeftBase, eyeRightBase;
    private float eyeFactor = 1f;

    public bool IsPlayer => isPlayer;
    public float GasPercent => currentGas / gasCapacity;
    public bool OutOfGas => currentGas < gasCost;
    public float NoGasMessageTime => noGasMessageTime;
    public float Speed
    {
        get
        {
            Vector3 v = Vel;
            return new Vector3(v.x, 0f, v.z).magnitude;
        }
    }

    protected Vector3 Vel
    {
#if UNITY_6000_0_OR_NEWER
        get => rb.linearVelocity;
        set => rb.linearVelocity = value;
#else
        get => rb.velocity;
        set => rb.velocity = value;
#endif
    }

    public void SetPlayer(bool value)
    {
        isPlayer = value;
    }

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        currentGas = gasCapacity;

        eyeLeft = transform.Find("Visual/Head/EyeLeft");
        eyeRight = transform.Find("Visual/Head/EyeRight");
        if (eyeLeft != null) eyeLeftBase = eyeLeft.localScale;
        if (eyeRight != null) eyeRightBase = eyeRight.localScale;
    }

    protected virtual void Start()
    {
        if (Camera.main != null) cam = Camera.main.transform;
    }

    protected virtual void Update()
    {
        if (isPlayer) PlayerInput();
        else AIStep();
        UpdateEyes();
    }

    protected virtual void FixedUpdate()
    {
        currentGas = Mathf.Min(gasCapacity, currentGas + gasRegenRate * Time.fixedDeltaTime);
    }

    protected abstract void PlayerInput();
    protected abstract void AIStep();

    protected bool TrySpendGas()
    {
        if (Time.time - lastActionTime < actionCooldown) return false;
        if (currentGas < gasCost)
        {
            noGasMessageTime = Time.time;
            return false;
        }
        currentGas -= gasCost;
        lastActionTime = Time.time;
        return true;
    }

    protected Vector3 FlatForward()
    {
        Vector3 f = transform.forward;
        f.y = 0f;
        return f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.forward;
    }

    protected bool ReadWASD(out Vector3 dir)
    {
        float x = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
        float z = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
        dir = Vector3.zero;
        if (x == 0f && z == 0f) return false;

        Vector3 forward = cam != null ? cam.forward : Vector3.forward;
        Vector3 right = cam != null ? cam.right : Vector3.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
        dir = (forward * z + right * x).normalized;
        return true;
    }

    private void UpdateEyes()
    {
        float target = 1f;
        if (Time.time - lastActionTime < 0.25f) target = 1.6f;
        else if (OutOfGas) target = 0.8f;

        eyeFactor = Mathf.Lerp(eyeFactor, target, 15f * Time.deltaTime);
        if (eyeLeft != null) eyeLeft.localScale = eyeLeftBase * eyeFactor;
        if (eyeRight != null) eyeRight.localScale = eyeRightBase * eyeFactor;
    }
}
