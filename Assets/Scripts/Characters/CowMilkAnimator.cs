using UnityEngine;

/// <summary>
/// Animacao do leite: a vaca se levanta nas patas de tras (pivo nas rodinhas
/// traseiras), o leite sai pelas 4 tetas, e depois ela volta ao chao.
/// </summary>
public class CowMilkAnimator : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform visual;
    [SerializeField] private Transform udder;
    [SerializeField] private ParticleSystem[] teatSprays;

    [Header("Levantar")]
    [Tooltip("Ponto (espaco do Visual) em volta do qual ela se levanta: as rodinhas de tras")]
    [SerializeField] private Vector3 rearPivot = new Vector3(0f, -0.78f, -0.6f);
    [SerializeField] private float rearAngle = 50f;
    [SerializeField] private float riseTime = 0.28f;
    [SerializeField] private float sprayTime = 0.8f;
    [SerializeField] private float fallTime = 0.35f;

    [Header("Jato")]
    [Tooltip("Inclinacao do jato para cima (graus)")]
    [SerializeField] private float sprayPitch = 18f;

    private enum Phase { Idle, Rising, Spraying, Falling }

    private Phase phase = Phase.Idle;
    private float timer;
    private float angle;
    private Vector3 udderBase = Vector3.one;

    public bool IsBusy => phase != Phase.Idle;
    public bool IsSpraying => phase == Phase.Spraying;
    public float SprayTime => sprayTime;

    private void Awake()
    {
        if (visual == null) visual = transform.Find("Visual");
        if (udder != null) udderBase = udder.localScale;
    }

    private void OnDisable()
    {
        phase = Phase.Idle;
        timer = 0f;
        angle = 0f;
        SetSpray(false);
        ApplyPose();
        if (udder != null) udder.localScale = udderBase;
    }

    public void Play()
    {
        if (phase != Phase.Idle) return;
        phase = Phase.Rising;
        timer = 0f;
    }

    private void LateUpdate()
    {
        if (phase == Phase.Idle) return;

        timer += Time.deltaTime;

        switch (phase)
        {
            case Phase.Rising:
            {
                float k = Mathf.Clamp01(timer / riseTime);
                angle = rearAngle * EaseOutBack(k);
                if (k >= 1f)
                {
                    phase = Phase.Spraying;
                    timer = 0f;
                    SetSpray(true);
                }
                break;
            }
            case Phase.Spraying:
            {
                angle = rearAngle;
                if (udder != null) udder.localScale = udderBase * (1f + 0.1f * Mathf.Sin(timer * 38f));
                AimSprays();
                if (timer >= sprayTime)
                {
                    SetSpray(false);
                    if (udder != null) udder.localScale = udderBase;
                    phase = Phase.Falling;
                    timer = 0f;
                }
                break;
            }
            case Phase.Falling:
            {
                float k = Mathf.Clamp01(timer / fallTime);
                angle = rearAngle * (1f - Mathf.SmoothStep(0f, 1f, k));
                if (k >= 1f)
                {
                    angle = 0f;
                    phase = Phase.Idle;
                }
                break;
            }
        }

        ApplyPose();
    }

    private void ApplyPose()
    {
        if (visual == null) return;
        Quaternion q = Quaternion.Euler(-angle, 0f, 0f);
        visual.localRotation = q;
        visual.localPosition = rearPivot - q * rearPivot;
    }

    private void SetSpray(bool on)
    {
        if (teatSprays == null) return;
        if (on) AimSprays();
        foreach (ParticleSystem ps in teatSprays)
        {
            if (ps == null) continue;
            if (on) ps.Play();
            else ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    // Os jatos apontam sempre para a frente da vaca (e um pouco para cima),
    // mesmo com o corpo inclinado.
    private void AimSprays()
    {
        if (teatSprays == null) return;
        Vector3 f = transform.forward;
        f.y = 0f;
        if (f.sqrMagnitude < 0.0001f) f = Vector3.forward;
        f.Normalize();

        float r = sprayPitch * Mathf.Deg2Rad;
        Vector3 dir = f * Mathf.Cos(r) + Vector3.up * Mathf.Sin(r);
        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

        foreach (ParticleSystem ps in teatSprays)
            if (ps != null) ps.transform.rotation = rot;
    }

    private static float EaseOutBack(float k)
    {
        const float c1 = 1.4f;
        const float c3 = c1 + 1f;
        float x = k - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }
}
