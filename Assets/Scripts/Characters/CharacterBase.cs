using UnityEngine;

public enum CharacterType { Vaca = 0, Boi = 1 }

[RequireComponent(typeof(Rigidbody))]
public abstract class CharacterBase : MonoBehaviour
{
    [Header("Gas (mesmo reservatorio para peido e leite)")]
    [SerializeField] protected float gasCapacity = 100f;
    [SerializeField] protected float gasCost = 25f;
    [SerializeField] protected float gasRegenRate = 15f;
    [Tooltip("Tempo sem gastar gas antes de comecar a regenerar")]
    [SerializeField] protected float gasRegenDelay = 0.4f;
    [SerializeField] protected float actionCooldown = 0.3f;

    [Header("Giro com inercia (gelo)")]
    [Tooltip("Giro (graus/s) ganho ao bater em algo")]
    [SerializeField] protected float bumpSpin = 200f;
    [SerializeField] protected float maxYawSpeed = 520f;

    [SerializeField] private bool isPlayer;

    // Esquema de teclas (permite 2 jogadores locais no mesmo teclado: P1=WASD/Espaco,
    // P2=Setas/Enter). Ver SetInputScheme(). Time (0/1) usado so no modo Multi p/ placar.
    protected KeyCode keyUp = KeyCode.W, keyDown = KeyCode.S, keyLeft = KeyCode.A, keyRight = KeyCode.D;
    protected KeyCode keyAction = KeyCode.Space;
    private int team;

    protected Rigidbody rb;
    protected Transform cam;

    private float currentGas;
    private float lastActionTime = -10f;
    private float lastGasUseTime = -10f;
    private float noGasMessageTime = -10f;
    private Transform eyeLeft, eyeRight;
    private Vector3 eyeLeftBase, eyeRightBase;
    private float eyeFactor = 1f;
    private float yaw;
    private float yawVel;

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

    protected float CurrentGas => currentGas;

    /// <summary>Velocidade ao longo da frente do personagem (negativa = de re).</summary>
    public float ForwardSpeed
    {
        get
        {
            Vector3 v = Vel;
            return Vector3.Dot(new Vector3(v.x, 0f, v.z), transform.forward);
        }
    }

    public void RefillGas(float amount)
    {
        currentGas = Mathf.Min(gasCapacity, currentGas + amount);
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

    /// <summary>Time no modo Multi (0 ou 1); so afeta o placar (CollectibleManager.ScoreA/ScoreB).</summary>
    public int Team => team;
    public void SetTeam(int value) => team = value;

    /// <summary>
    /// Troca as teclas de controle deste personagem (p/ 2 jogadores locais no mesmo PC,
    /// usado como placeholder nos modos Dupla/Multi ate o multiplayer em rede existir).
    /// </summary>
    public void SetInputScheme(KeyCode up, KeyCode down, KeyCode left, KeyCode right, KeyCode action)
    {
        keyUp = up; keyDown = down; keyLeft = left; keyRight = right; keyAction = action;
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

    protected virtual void OnEnable()
    {
        yaw = transform.eulerAngles.y;
        yawVel = 0f;
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        // bateu forte: o personagem rodopia (descontrole)
        if (collision.relativeVelocity.magnitude > 3f)
            yawVel += Random.Range(-1f, 1f) * bumpSpin;
    }

    /// <summary>
    /// Gira em direcao a "desired" como uma mola com pouco amortecimento:
    /// passa do ponto, balanca, e continua girando um pouco (efeito gelo).
    /// </summary>
    protected void Steer(Vector3 desired, float spring, float damping, float dt, float extraTorque = 0f)
    {
        float acc = -damping * yawVel + extraTorque;
        if (desired.sqrMagnitude > 0.01f)
        {
            float target = Mathf.Atan2(desired.x, desired.z) * Mathf.Rad2Deg;
            acc += spring * Mathf.DeltaAngle(yaw, target);
        }
        yawVel = Mathf.Clamp(yawVel + acc * dt, -maxYawSpeed, maxYawSpeed);
        yaw += yawVel * dt;
        rb.MoveRotation(Quaternion.Euler(0f, yaw, 0f));
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
        if (Time.time - lastGasUseTime >= gasRegenDelay)
            currentGas = Mathf.Min(gasCapacity, currentGas + gasRegenRate * Time.fixedDeltaTime);
    }

    protected abstract void PlayerInput();
    protected abstract void AIStep();

    /// <summary>Gasta um bloco de gas de uma vez (acao de clique unico, ex.: leite).</summary>
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
        lastGasUseTime = Time.time;
        return true;
    }

    /// <summary>Gasta gas continuamente (acao de segurar, ex.: peido). Retorna false quando acabou.</summary>
    protected bool DrainGas(float amount)
    {
        currentGas = Mathf.Max(0f, currentGas - amount);
        lastGasUseTime = Time.time;
        lastActionTime = Time.time;
        return currentGas > 0f;
    }

    protected void NotifyNoGas()
    {
        noGasMessageTime = Time.time;
    }

    protected Vector3 FlatForward()
    {
        Vector3 f = transform.forward;
        f.y = 0f;
        return f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.forward;
    }

    protected bool ReadWASD(out Vector3 dir)
    {
        float x = (Input.GetKey(keyRight) ? 1f : 0f) - (Input.GetKey(keyLeft) ? 1f : 0f);
        float z = (Input.GetKey(keyUp) ? 1f : 0f) - (Input.GetKey(keyDown) ? 1f : 0f);
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
