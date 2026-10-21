using UnityEngine;

/// <summary>
/// Camera com 3 modos:
///  - Overview: visao de cima (tela de selecao de personagem), como era antes.
///  - Transitioning: anima descendo da visao de cima ate atras da nuca (ao clicar Jogar).
///  - Chase: orbita o personagem (mouse gira, scroll aproxima/afasta), como em jogos de acao.
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;

    [Header("Orbita (mouse gira, scroll zoom)")]
    [Tooltip("Distancia inicial/padrao do personagem")]
    [SerializeField] private float distance = 4.2f;
    [SerializeField] private float minDistance = 2.2f;
    [SerializeField] private float maxDistance = 9f;
    [Tooltip("Angulo vertical inicial (graus acima do horizonte)")]
    [SerializeField] private float pitch = 14f;
    [SerializeField] private float minPitch = 2f;
    [SerializeField] private float maxPitch = 75f;
    [Tooltip("Altura do ponto ao redor do qual a camera orbita (nuca/cabeca)")]
    [SerializeField] private float lookHeight = 1.5f;
    [SerializeField] private float mouseSensitivity = 3.5f;
    [SerializeField] private float scrollSensitivity = 6f;
    [SerializeField] private float positionSmoothTime = 0.08f;

    [Header("Colisao (evita atravessar parede/arvore)")]
    [SerializeField] private float collisionRadius = 0.3f;
    [SerializeField] private int collisionMask = ~0;

    [Header("Visao geral (tela de selecao de personagem)")]
    [SerializeField] private float overviewDistance = 16f;
    [SerializeField] private float overviewPitch = 50f;
    [SerializeField] private float overviewYaw = 0f;
    [SerializeField] private float overviewLookHeight = 0.5f;

    [Header("Transicao ao clicar Jogar")]
    [Tooltip("Duracao da animacao descendo ate atras do personagem")]
    [SerializeField] private float introDuration = 1.3f;

    private enum Mode { Overview, Transitioning, Chase }
    private Mode mode = Mode.Overview;

    private Vector3 velocity;
    private float orbitYaw;
    private float orbitPitch;
    private float orbitDistance;
    private CharacterBase targetCharacter;
    private Transform lastTarget;

    private Vector3 transStartPos;
    private Quaternion transStartRot;
    private float transTimer;

    private void Start()
    {
        if (target == null)
        {
            VacaController vaca = FindFirstObjectByType<VacaController>();
            if (vaca != null) target = vaca.transform;
        }
        orbitPitch = pitch;
        orbitDistance = distance;
        if (target != null)
        {
            orbitYaw = target.eulerAngles.y;
            transform.position = OverviewPosition();
            transform.rotation = OverviewRotation();
        }
        ApplyCursorState();
    }

    private Quaternion OverviewRotation() => Quaternion.Euler(overviewPitch, overviewYaw, 0f);

    private Vector3 OverviewPosition()
    {
        return target.position + Vector3.up * overviewLookHeight + OverviewRotation() * Vector3.back * overviewDistance;
    }

    private Vector3 Pivot() => target.position + Vector3.up * lookHeight;

    private Vector3 OrbitPosition(float yaw, float pitchDeg, float dist)
    {
        Quaternion rot = Quaternion.Euler(pitchDeg, yaw, 0f);
        Vector3 pivot = Pivot();
        Vector3 back = rot * Vector3.back;
        Vector3 wanted = pivot + back * dist;

        Vector3 dir = wanted - pivot;
        float d = dir.magnitude;
        if (d > 0.01f)
        {
            Vector3 dirN = dir / d;
            if (Physics.SphereCast(pivot, collisionRadius, dirN, out RaycastHit hit, d, collisionMask, QueryTriggerInteraction.Ignore))
                wanted = pivot + dirN * Mathf.Max(hit.distance - collisionRadius, 0.6f);
        }
        return wanted;
    }

    /// <summary>Chamado ao clicar Jogar: anima a camera descendo ate atras do personagem.</summary>
    public void BeginChase()
    {
        orbitYaw = target != null ? target.eulerAngles.y : orbitYaw;
        orbitPitch = pitch;
        orbitDistance = distance;
        if (target == null) { mode = Mode.Chase; return; }
        transStartPos = transform.position;
        transStartRot = transform.rotation;
        transTimer = 0f;
        mode = Mode.Transitioning;
    }

    /// <summary>
    /// Modos com 2+ jogadores locais (Dupla/Multi): fica em Overview (visao de cima)
    /// olhando para um ponto fixo (ex.: centro da arena) em vez de perseguir 1 personagem,
    /// com distancia maior para enxergar os dois times. Placeholder ate existir
    /// multiplayer em rede de verdade (cada PC com a propria camera).
    /// </summary>
    public void SetWideOverview(Transform fixedTarget, float distance)
    {
        target = fixedTarget;
        overviewDistance = distance;
        mode = Mode.Overview;
    }

    private void ApplyCursorState()
    {
        bool lockIt = mode == Mode.Chase || mode == Mode.Transitioning;
        Cursor.lockState = lockIt ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockIt;
    }

    private void ReadMouseOrbit()
    {
        orbitYaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        orbitPitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        orbitPitch = Mathf.Clamp(orbitPitch, minPitch, maxPitch);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
            orbitDistance = Mathf.Clamp(orbitDistance - scroll * scrollSensitivity, minDistance, maxDistance);
    }

    private void LateUpdate()
    {
        if (target == null) return;
        if (target != lastTarget)
        {
            lastTarget = target;
            targetCharacter = target.GetComponent<CharacterBase>();
        }

        ApplyCursorState();

        if (mode == Mode.Overview)
        {
            transform.position = OverviewPosition();
            transform.rotation = OverviewRotation();
            return;
        }

        if (mode == Mode.Chase) ReadMouseOrbit();

        Vector3 chasePos = OrbitPosition(orbitYaw, orbitPitch, orbitDistance);
        Vector3 lookPoint = Pivot();

        if (mode == Mode.Transitioning)
        {
            transTimer += Time.deltaTime;
            float t = Mathf.Clamp01(transTimer / introDuration);
            float eased = t * t * (3f - 2f * t); // smoothstep: comeca e termina suave

            transform.position = Vector3.Lerp(transStartPos, chasePos, eased);
            Quaternion chaseRot = Quaternion.LookRotation((lookPoint - transform.position).normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transStartRot, chaseRot, eased);

            if (t >= 1f) mode = Mode.Chase;
            return;
        }

        // Mode.Chase: orbita suavemente com o mouse/scroll
        transform.position = Vector3.SmoothDamp(transform.position, chasePos, ref velocity, positionSmoothTime);
        transform.LookAt(lookPoint);
    }
}
