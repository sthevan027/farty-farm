using UnityEngine;

public class VacaController : CharacterBase
{
    [Header("Movimento (WASD anda)")]
    [Tooltip("Trava de velocidade maxima")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float turnSpeed = 12f;

    [Header("Leite")]
    [SerializeField] private float milkReach = 2.2f;
    [SerializeField] private float milkRadius = 2.5f;
    [Tooltip("Cada tiro tira isso da vida do fogo (100 = apaga com 2 tiros de 50)")]
    [SerializeField] private float milkPower = 50f;

    [Header("Referencias")]
    [SerializeField] private ParticleSystem milkParticles;

    private Vector3 moveInput;
    private Vector3 desiredFacing;

    protected override void Awake()
    {
        base.Awake();
        desiredFacing = transform.forward;
        if (milkParticles == null) milkParticles = GetComponentInChildren<ParticleSystem>();
    }

    protected override void PlayerInput()
    {
        moveInput = ReadWASD(out Vector3 dir) ? dir : Vector3.zero;
        if (moveInput != Vector3.zero) desiredFacing = moveInput;
        if (Input.GetKeyDown(KeyCode.Space)) Milk();
    }

    protected override void AIStep()
    {
        FlammableTree target = FlammableTree.FindNearestBurning(transform.position);
        if (target == null)
        {
            moveInput = Vector3.zero;
            return;
        }

        Vector3 to = target.transform.position - transform.position;
        to.y = 0f;
        float dist = to.magnitude;
        desiredFacing = to.normalized;
        moveInput = dist > milkReach + 0.6f ? desiredFacing : Vector3.zero;

        if (dist <= milkReach + 0.8f && Vector3.Angle(transform.forward, desiredFacing) < 25f)
            Milk();
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
        horizontal = Vector3.MoveTowards(horizontal, moveInput * moveSpeed, acceleration * dt);
        Vel = new Vector3(horizontal.x, v.y, horizontal.z);
    }

    private void Milk()
    {
        if (!TrySpendGas()) return;

        if (milkParticles != null) milkParticles.Play();

        Vector3 center = transform.position + FlatForward() * milkReach;
        Collider[] hits = Physics.OverlapSphere(center, milkRadius, ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            FlammableTree tree = hit.GetComponentInParent<FlammableTree>();
            if (tree != null) tree.Extinguish(milkPower);
        }
    }
}
