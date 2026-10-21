using UnityEngine;

public class BoiController : CharacterBase
{
    [Header("Direcao (WASD gira com inercia)")]
    [Tooltip("Forca da mola de giro: maior = vira mais rapido")]
    [SerializeField] private float turnSpring = 45f;
    [Tooltip("Amortecimento do giro: MENOR = mais balanco/descontrole")]
    [SerializeField] private float turnDamping = 4.5f;

    [Header("Gelo (so o peido move)")]
    [Tooltip("Atrito por segundo: bem baixo = escorrega muito")]
    [SerializeField] private float iceDrag = 0.22f;
    [Tooltip("Velocidade maxima")]
    [SerializeField] private float maxSpeed = 11f;

    [Header("Peido (SEGURAR Espaco)")]
    [Tooltip("Aceleracao (m/s2) enquanto segura")]
    [SerializeField] private float fartThrust = 13f;
    [Tooltip("Tranco instantaneo (m/s) ao comecar o peido")]
    [SerializeField] private float startKick = 3f;
    [Tooltip("Tremedeira de direcao enquanto peida (descontrole)")]
    [SerializeField] private float fartWobble = 220f;
    [Tooltip("Gas gasto por segundo segurando")]
    [SerializeField] private float gasDrainPerSecond = 30f;
    [Tooltip("Gas minimo para conseguir comecar a peidar")]
    [SerializeField] private float minGasToStart = 10f;

    [Header("Fogo")]
    [SerializeField] private float igniteRadius = 2.5f;
    [Range(0f, 1f)]
    [SerializeField] private float igniteChance = 1f;
    [SerializeField] private float flameOffset = 1.2f;
    [Tooltip("Intervalo entre tentativas de incendiar enquanto peida")]
    [SerializeField] private float igniteInterval = 0.12f;

    [Header("Referencias")]
    [SerializeField] private ParticleSystem fartParticles;

    private Vector3 desiredFacing;
    private float aiNextTurn;
    private float aiNextFart;
    private float aiFartUntil;
    private float noiseSeed;

    private bool wantFart;
    private bool farting;
    private bool exhausted;
    private float igniteTimer;
    private TailAnimator tail;

    protected override void Awake()
    {
        base.Awake();
        desiredFacing = transform.forward;
        noiseSeed = Random.value * 100f;
        if (fartParticles == null) fartParticles = GetComponentInChildren<ParticleSystem>();
        tail = GetComponentInChildren<TailAnimator>();
    }

    private void OnDisable()
    {
        wantFart = false;
        SetFarting(false);
    }

    protected override void PlayerInput()
    {
        if (ReadWASD(out Vector3 dir)) desiredFacing = dir;
        wantFart = Input.GetKey(keyAction);
    }

    protected override void AIStep()
    {
        Vector3 p = transform.position;
        if (Time.time >= aiNextTurn)
        {
            aiNextTurn = Time.time + Random.Range(2f, 5f);
            desiredFacing = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
        }
        if (Mathf.Abs(p.x) > 34f || Mathf.Abs(p.z) > 34f)
            desiredFacing = new Vector3(-p.x, 0f, -p.z).normalized;

        if (Time.time >= aiNextFart && Vector3.Angle(transform.forward, desiredFacing) < 25f)
        {
            aiFartUntil = Time.time + Random.Range(0.5f, 1.2f);
            aiNextFart = aiFartUntil + Random.Range(2f, 4f);
        }
        wantFart = Time.time < aiFartUntil;
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        float dt = Time.fixedDeltaTime;

        UpdateFart(dt);

        // giro com inercia (+ tremedeira enquanto peida)
        float wobble = 0f;
        if (farting)
            wobble = (Mathf.PerlinNoise(Time.time * 3.5f, noiseSeed) - 0.5f) * 2f * fartWobble;
        Steer(desiredFacing, turnSpring, turnDamping, dt, wobble);

        // movimento: a velocidade e escrita direto (nada de AddForce sendo sobrescrito)
        Vector3 v = Vel;
        Vector3 h = new Vector3(v.x, 0f, v.z);

        if (farting) h += FlatForward() * (fartThrust * dt);

        h *= Mathf.Exp(-iceDrag * dt);

        float speed = h.magnitude;
        if (speed > maxSpeed) h = h / speed * Mathf.MoveTowards(speed, maxSpeed, 40f * dt);

        Vel = new Vector3(h.x, v.y, h.z);
    }

    private void UpdateFart(float dt)
    {
        if (!wantFart)
        {
            exhausted = false;
            SetFarting(false);
            return;
        }

        if (exhausted)
        {
            SetFarting(false);
            return;
        }

        if (!farting && CurrentGas < minGasToStart)
        {
            NotifyNoGas();
            exhausted = true;
            return;
        }

        if (!DrainGas(gasDrainPerSecond * dt))
        {
            exhausted = true;
            SetFarting(false);
            return;
        }

        if (!farting)
        {
            SetFarting(true);
            Vector3 v = Vel;
            Vel = v + FlatForward() * startKick; // tranco inicial
        }

        igniteTimer -= dt;
        if (igniteTimer <= 0f)
        {
            igniteTimer = igniteInterval;
            TryIgnite();
        }
    }

    private void SetFarting(bool on)
    {
        if (farting == on) return;
        farting = on;

        if (fartParticles != null)
        {
            if (on) fartParticles.Play(true);
            else fartParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        if (tail != null) tail.Boost = on;
        if (on) igniteTimer = 0f;
    }

    private void TryIgnite()
    {
        Vector3 flame = transform.position - FlatForward() * flameOffset;
        Collider[] hits = Physics.OverlapSphere(flame, igniteRadius, ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            FlammableTree tree = hit.GetComponentInParent<FlammableTree>();
            if (tree != null && Random.value <= igniteChance) tree.Ignite();
        }
    }
}
