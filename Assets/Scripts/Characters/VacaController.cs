using UnityEngine;

public class VacaController : CharacterBase
{
    [Header("Movimento (WASD anda, mas escorrega nos patins)")]
    [Tooltip("Velocidade maxima")]
    [SerializeField] private float moveSpeed = 5f;
    [Tooltip("Aceleracao: baixa = demora para pegar velocidade e para trocar de direcao")]
    [SerializeField] private float acceleration = 6f;
    [Tooltip("Atrito por segundo quando solta as teclas (baixo = desliza)")]
    [SerializeField] private float iceDrag = 0.6f;
    [SerializeField] private float turnSpring = 70f;
    [SerializeField] private float turnDamping = 9f;
    [Tooltip("Multiplicador de velocidade enquanto levanta e solta leite")]
    [SerializeField] private float milkingSpeedMultiplier = 0.3f;

    [Header("Leite")]
    [SerializeField] private float milkReach = 2.2f;
    [SerializeField] private float milkRadius = 2.5f;
    [Tooltip("Dano TOTAL de um jato inteiro no fogo (o fogo tem 100 de vida)")]
    [SerializeField] private float milkPower = 130f;

    private Vector3 moveInput;
    private Vector3 desiredFacing;
    private CowMilkAnimator milkAnim;
    private TailAnimator tail;
    private float milkTick;

    protected override void Awake()
    {
        base.Awake();
        desiredFacing = transform.forward;
        milkAnim = GetComponent<CowMilkAnimator>();
        tail = GetComponentInChildren<TailAnimator>();
    }

    protected override void PlayerInput()
    {
        moveInput = ReadWASD(out Vector3 dir) ? dir : Vector3.zero;
        if (moveInput != Vector3.zero) desiredFacing = moveInput;
        if (Input.GetKeyDown(keyAction)) Milk();
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
        bool busy = milkAnim != null && milkAnim.IsBusy;

        Steer(desiredFacing, turnSpring, turnDamping, dt);

        float speed = moveSpeed * (busy ? milkingSpeedMultiplier : 1f);
        Vector3 v = Vel;
        Vector3 horizontal = new Vector3(v.x, 0f, v.z);
        if (moveInput != Vector3.zero)
            horizontal = Vector3.MoveTowards(horizontal, moveInput * speed, acceleration * dt);
        else
            horizontal *= Mathf.Exp(-iceDrag * dt);
        Vel = new Vector3(horizontal.x, v.y, horizontal.z);

        if (milkAnim != null && milkAnim.IsSpraying) ApplyMilk(dt);
        else milkTick = 0f;
    }

    private void Milk()
    {
        if (milkAnim != null && milkAnim.IsBusy) return;
        if (!TrySpendGas()) return;

        if (milkAnim != null) milkAnim.Play();
        if (tail != null) tail.Flick(1.4f);
    }

    // O dano e aplicado enquanto o leite esta saindo, em passos de 0.1s.
    private void ApplyMilk(float dt)
    {
        milkTick -= dt;
        if (milkTick > 0f) return;

        const float interval = 0.1f;
        milkTick = interval;
        float power = milkPower * interval / Mathf.Max(0.05f, milkAnim.SprayTime);

        Vector3 center = transform.position + FlatForward() * milkReach;
        Collider[] hits = Physics.OverlapSphere(center, milkRadius, ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            FlammableTree tree = hit.GetComponentInParent<FlammableTree>();
            if (tree != null) tree.Extinguish(power);
        }
    }
}
