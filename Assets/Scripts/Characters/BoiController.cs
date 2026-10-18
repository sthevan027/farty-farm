using UnityEngine;

public enum CharacterType { Vaca = 0, Boi = 1 }

[RequireComponent(typeof(Rigidbody))]
public class BoiController : MonoBehaviour
{
    [Header("Direcao (WASD so gira o personagem)")]
    [SerializeField] private float turnSpeed = 10f;

    [Header("Movimento (so o peido move)")]
    [Tooltip("Atrito horizontal por segundo (independente de FPS)")]
    [SerializeField] private float friction = 0.8f;
    [SerializeField] private float maxSpeed = 16f;

    [Header("Peido")]
    [SerializeField] private float fartImpulse = 10f;
    [SerializeField] private float gasCapacity = 100f;
    [SerializeField] private float gasPerFart = 15f;
    [SerializeField] private float gasRegenRate = 15f;
    [SerializeField] private float fartCooldown = 0.3f;

    [Header("Referencias")]
    [SerializeField] private ParticleSystem fartParticles;

    private Rigidbody rb;
    private Transform cam;
    private Vector3 desiredFacing;
    private float currentGas;
    private float lastFartTime = -10f;
    private float noGasMessageTime = -10f;
    private Transform eyeLeft, eyeRight;
    private Vector3 eyeLeftBase, eyeRightBase;
    private float eyeFactor = 1f;

    public CharacterType Current { get; private set; } = CharacterType.Vaca;
    public float GasPercent => currentGas / gasCapacity;
    public float Speed => rb != null ? new Vector3(Vel.x, 0, Vel.z).magnitude : 0f;
    public bool OutOfGas => currentGas < gasPerFart;
    public float NoGasMessageTime => noGasMessageTime;

    private Vector3 Vel
    {
#if UNITY_6000_0_OR_NEWER
        get => rb.linearVelocity;
        set => rb.linearVelocity = value;
#else
        get => rb.velocity;
        set => rb.velocity = value;
#endif
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        currentGas = gasCapacity;
        desiredFacing = transform.forward;
        if (fartParticles == null) fartParticles = GetComponentInChildren<ParticleSystem>();
        SetCharacter(Current);
    }

    private void Start()
    {
        if (Camera.main != null) cam = Camera.main.transform;
    }

    public void SetCharacter(CharacterType type)
    {
        Current = type;
        Transform vaca = transform.Find("Visual_Vaca");
        Transform boi = transform.Find("Visual_Boi");
        if (vaca != null) vaca.gameObject.SetActive(type == CharacterType.Vaca);
        if (boi != null) boi.gameObject.SetActive(type == CharacterType.Boi);

        Transform active = type == CharacterType.Vaca ? vaca : boi;
        eyeLeft = active != null ? active.Find("Head/EyeLeft") : null;
        eyeRight = active != null ? active.Find("Head/EyeRight") : null;
        if (eyeLeft != null) eyeLeftBase = eyeLeft.localScale;
        if (eyeRight != null) eyeRightBase = eyeRight.localScale;
        eyeFactor = 1f;
    }

    private void Update()
    {
        ReadInput();
        UpdateEyes();
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        if (desiredFacing.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(desiredFacing, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, target, turnSpeed * dt));
        }

        Vector3 v = Vel;
        Vector3 horizontal = new Vector3(v.x, 0f, v.z);
        horizontal *= Mathf.Exp(-friction * dt);
        if (horizontal.magnitude > maxSpeed) horizontal = horizontal.normalized * maxSpeed;
        Vel = new Vector3(horizontal.x, v.y, horizontal.z);

        currentGas = Mathf.Min(gasCapacity, currentGas + gasRegenRate * dt);
    }

    private void ReadInput()
    {
        float x = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
        float z = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);

        if (x != 0f || z != 0f)
        {
            Vector3 forward = cam != null ? cam.forward : Vector3.forward;
            Vector3 right = cam != null ? cam.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            desiredFacing = (forward * z + right * x).normalized;
        }

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

        // Gas sai por tras; o impulso empurra para a frente (para onde o personagem olha).
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
        if (Time.time - lastFartTime < 0.25f) target = 1.6f;
        else if (OutOfGas) target = 0.8f;

        eyeFactor = Mathf.Lerp(eyeFactor, target, 15f * Time.deltaTime);
        if (eyeLeft != null) eyeLeft.localScale = eyeLeftBase * eyeFactor;
        if (eyeRight != null) eyeRight.localScale = eyeRightBase * eyeFactor;
    }
}
