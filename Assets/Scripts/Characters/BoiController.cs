using UnityEngine;

public class BoiController : CharacterBase
{
    [Header("Direcao (WASD so gira)")]
    [SerializeField] private float turnSpeed = 10f;

    [Header("Movimento (so o peido move)")]
    [Tooltip("Atrito horizontal por segundo (independente de FPS)")]
    [SerializeField] private float friction = 0.8f;
    [Tooltip("Trava de velocidade maxima")]
    [SerializeField] private float maxSpeed = 2.5f;
    [Tooltip("Velocidade ganha por peido")]
    [SerializeField] private float fartImpulse = 1.5f;

    [Header("Fogo")]
    [SerializeField] private float igniteRadius = 2.5f;
    [Range(0f, 1f)]
    [SerializeField] private float igniteChance = 1f;
    [SerializeField] private float flameOffset = 1.2f;

    [Header("Referencias")]
    [SerializeField] private ParticleSystem fartParticles;

    private Vector3 desiredFacing;
    private float aiNextTurn;
    private float aiNextFart;

    protected override void Awake()
    {
        base.Awake();
        desiredFacing = transform.forward;
        if (fartParticles == null) fartParticles = GetComponentInChildren<ParticleSystem>();
    }

    protected override void PlayerInput()
    {
        if (ReadWASD(out Vector3 dir)) desiredFacing = dir;
        if (Input.GetKeyDown(KeyCode.Space)) Fart();
    }

    protected override void AIStep()
    {
        Vector3 p = transform.position;
        if (Time.time >= aiNextTurn)
        {
            aiNextTurn = Time.time + Random.Range(3f, 6f);
            desiredFacing = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
        }
        if (Mathf.Abs(p.x) > 34f || Mathf.Abs(p.z) > 34f)
            desiredFacing = new Vector3(-p.x, 0f, -p.z).normalized;

        if (Time.time >= aiNextFart && Vector3.Angle(transform.forward, desiredFacing) < 20f)
        {
            aiNextFart = Time.time + Random.Range(2.5f, 4.5f);
            Fart();
        }
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
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
    }

    private void Fart()
    {
        if (!TrySpendGas()) return;

        Vector3 forward = FlatForward();
        rb.AddForce(forward * fartImpulse, ForceMode.Impulse);

        if (fartParticles != null) fartParticles.Play();

        Vector3 flame = transform.position - forward * flameOffset;
        Collider[] hits = Physics.OverlapSphere(flame, igniteRadius, ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            FlammableTree tree = hit.GetComponentInParent<FlammableTree>();
            if (tree != null && Random.value <= igniteChance) tree.Ignite();
        }
    }
}
