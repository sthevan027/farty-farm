using UnityEngine;

public class BoiController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float acceleration = 50f;
    [SerializeField] private float maxSpeed = 30f;
    [SerializeField] private float friction = 0.95f;
    [SerializeField] private float angularDrag = 5f;

    [Header("Fart System")]
    [SerializeField] private float fartForce = 20f;
    [SerializeField] private float gasCapacity = 100f;
    [SerializeField] private float gasPerFart = 20f;
    [SerializeField] private float gasRegenRate = 15f;
    [SerializeField] private float fartCooldown = 0.3f;

    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform eyeLeftBone;
    [SerializeField] private Transform eyeRightBone;
    [SerializeField] private ParticleSystem fartParticles;

    private Vector3 moveDirection = Vector3.zero;
    private float currentGas;
    private float lastFartTime = -1f;
    private Vector3 currentVelocity = Vector3.zero;

    private void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        currentGas = gasCapacity;
    }

    private void Update()
    {
        HandleInput();
        UpdateExpressions();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
        RegenerateGas();
        LimitSpeed();
        ApplyFriction();
    }

    private void HandleInput()
    {
        moveDirection = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
            moveDirection += Vector3.forward;
        if (Input.GetKey(KeyCode.S))
            moveDirection += Vector3.back;
        if (Input.GetKey(KeyCode.A))
            moveDirection += Vector3.left;
        if (Input.GetKey(KeyCode.D))
            moveDirection += Vector3.right;

        moveDirection.Normalize();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryFart();
        }
    }

    private void ApplyMovement()
    {
        if (moveDirection.magnitude > 0)
        {
            Vector3 force = moveDirection * acceleration;
            rb.AddForce(force, ForceMode.Acceleration);

            // Rotate towards movement direction
            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, angularDrag * Time.fixedDeltaTime);
            }
        }
    }

    private void ApplyFriction()
    {
        rb.velocity *= friction;
    }

    private void LimitSpeed()
    {
        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }
    }

    private void RegenerateGas()
    {
        if (currentGas < gasCapacity)
        {
            currentGas += gasRegenRate * Time.fixedDeltaTime;
            currentGas = Mathf.Min(currentGas, gasCapacity);
        }
    }

    private void TryFart()
    {
        if (Time.time - lastFartTime < fartCooldown)
            return;

        if (currentGas < gasPerFart)
        {
            Debug.Log("Sem gás!");
            return;
        }

        // Get direction opposite to where boi is facing
        Vector3 fartDirection = -transform.forward;
        fartDirection.y = 0;
        fartDirection.Normalize();

        // Apply impulse
        rb.AddForce(fartDirection * fartForce, ForceMode.Impulse);

        // Consume gas
        currentGas -= gasPerFart;
        lastFartTime = Time.time;

        // Particles
        if (fartParticles != null)
        {
            fartParticles.Play();
        }

        Debug.Log($"PRRRRR! Gás restante: {currentGas:F1}/{gasCapacity}");
    }

    private void UpdateExpressions()
    {
        // Eyes scale up when preparing/using fart
        float eyeScale = 1f;

        if (Time.time - lastFartTime < 0.2f)
        {
            // Just farted - eyes big
            eyeScale = 1.5f;
        }

        if (eyeLeftBone != null)
            eyeLeftBone.localScale = Vector3.one * eyeScale;
        if (eyeRightBone != null)
            eyeRightBone.localScale = Vector3.one * eyeScale;
    }

    public float GetGasPercent()
    {
        return currentGas / gasCapacity;
    }

    public float GetCurrentSpeed()
    {
        return rb.velocity.magnitude;
    }
}
