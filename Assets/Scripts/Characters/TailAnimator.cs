using UnityEngine;

/// <summary>
/// Rabinho com varias juntas. Balanca devagar parado, balanca mais ao andar e
/// levanta/chacoalha rapido quando Boost = true (peido) ou Flick() (leite).
/// </summary>
public class TailAnimator : MonoBehaviour
{
    [Tooltip("Juntas do rabo, da base (junto ao corpo) ate a ponta")]
    [SerializeField] private Transform[] segments;

    [Header("Pose de descanso (graus)")]
    [SerializeField] private float baseTilt = 28f;
    [SerializeField] private float curl = 9f;

    [Header("Balanco parado")]
    [SerializeField] private float idleAmplitude = 12f;
    [SerializeField] private float idleFrequency = 1.3f;

    [Header("Balanco agitado")]
    [SerializeField] private float wagAmplitude = 38f;
    [SerializeField] private float wagFrequency = 7f;
    [Tooltip("Quanto o rabo sobe quando agitado")]
    [SerializeField] private float liftAngle = 65f;

    [Header("Ondulacao")]
    [Tooltip("Atraso de fase entre juntas (radianos): faz a ponta chicotear")]
    [SerializeField] private float lag = 0.7f;
    [SerializeField] private float smooth = 6f;

    /// <summary>Enquanto true, o rabo levanta e chacoalha.</summary>
    public bool Boost { get; set; }

    private CharacterBase character;
    private float flickTimer;
    private float excite;
    private float lift;
    private float phase;

    private void Awake()
    {
        character = GetComponentInParent<CharacterBase>();
    }

    /// <summary>Agita o rabo por alguns segundos.</summary>
    public void Flick(float seconds)
    {
        flickTimer = Mathf.Max(flickTimer, seconds);
    }

    private void Update()
    {
        if (segments == null || segments.Length == 0) return;
        float dt = Time.deltaTime;

        bool agitated = Boost || flickTimer > 0f;
        if (flickTimer > 0f) flickTimer -= dt;

        float move = character != null ? Mathf.Clamp01(character.Speed / 2f) * 0.5f : 0f;
        float targetExcite = Mathf.Max(move, agitated ? 1f : 0f);
        float k = 1f - Mathf.Exp(-smooth * dt);
        excite = Mathf.Lerp(excite, targetExcite, k);
        lift = Mathf.Lerp(lift, agitated ? 1f : 0f, k);

        float freq = Mathf.Lerp(idleFrequency, wagFrequency, excite);
        phase += freq * dt * Mathf.PI * 2f;
        float amp = Mathf.Lerp(idleAmplitude, wagAmplitude, excite);

        for (int i = 0; i < segments.Length; i++)
        {
            Transform s = segments[i];
            if (s == null) continue;

            float side = Mathf.Sin(phase - i * lag) * amp * (i == 0 ? 0.55f : 1f);
            float pitch = (i == 0 ? baseTilt : curl) + lift * liftAngle * (i == 0 ? 1f : 0.2f);
            s.localRotation = Quaternion.Euler(pitch, 0f, side);
        }
    }
}
