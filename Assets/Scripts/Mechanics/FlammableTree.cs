using System.Collections.Generic;
using UnityEngine;

public class FlammableTree : MonoBehaviour
{
    public enum TreeState { Healthy, Burning, Burnt }

    public static readonly List<FlammableTree> All = new List<FlammableTree>();
    public static int BurntCount { get; private set; }
    public static int ExtinguishedCount { get; private set; }
    public static int ActiveFires { get; private set; }

    [Header("Fogo")]
    [SerializeField] private float burnDuration = 12f;
    [SerializeField] private float maxFireHealth = 100f;
    [SerializeField] private float spreadRadius = 3.5f;
    [Range(0f, 1f)]
    [SerializeField] private float spreadChancePerSecond = 0.15f;
    [SerializeField] private int maxSpreads = 2;

    [Header("Referencias")]
    [SerializeField] private ParticleSystem fire;
    [SerializeField] private Renderer[] foliage;
    [SerializeField] private Material burntMaterial;
    [SerializeField] private Renderer[] trunk;
    [SerializeField] private Material burntTrunkMaterial;

    private TreeState state = TreeState.Healthy;
    private float fireHealth;
    private float burnTimer;
    private float spreadTimer;
    private int spreads;

    public TreeState State => state;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        All.Clear();
        BurntCount = 0;
        ExtinguishedCount = 0;
        ActiveFires = 0;
    }

    private void OnEnable()
    {
        All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    public bool IsBurning => state == TreeState.Burning;

    public static FlammableTree FindNearestBurning(Vector3 from)
    {
        FlammableTree best = null;
        float bestDist = float.MaxValue;
        foreach (FlammableTree t in All)
        {
            if (t.state != TreeState.Burning) continue;
            float d = (t.transform.position - from).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = t;
            }
        }
        return best;
    }

    public void Ignite()
    {
        if (state != TreeState.Healthy) return;
        state = TreeState.Burning;
        fireHealth = maxFireHealth;
        burnTimer = 0f;
        spreadTimer = 0f;
        spreads = 0;
        ActiveFires++;
        if (fire != null) fire.Play();
    }

    public bool Extinguish(float power)
    {
        if (state != TreeState.Burning) return false;
        fireHealth -= power;
        if (fireHealth > 0f) return false;

        state = TreeState.Healthy;
        ActiveFires--;
        ExtinguishedCount++;
        if (fire != null) fire.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        return true;
    }

    private void Update()
    {
        if (state != TreeState.Burning) return;

        burnTimer += Time.deltaTime;
        spreadTimer += Time.deltaTime;

        if (spreadTimer >= 1f)
        {
            spreadTimer -= 1f;
            if (spreads < maxSpreads && Random.value < spreadChancePerSecond) TrySpread();
        }

        if (burnTimer >= burnDuration) BurnOut();
    }

    private void TrySpread()
    {
        List<FlammableTree> candidates = new List<FlammableTree>();
        foreach (FlammableTree t in All)
        {
            if (t.state != TreeState.Healthy) continue;
            Vector3 d = t.transform.position - transform.position;
            d.y = 0f;
            if (d.magnitude <= spreadRadius) candidates.Add(t);
        }
        if (candidates.Count == 0) return;
        candidates[Random.Range(0, candidates.Count)].Ignite();
        spreads++;
    }

    private void BurnOut()
    {
        state = TreeState.Burnt;
        ActiveFires--;
        BurntCount++;
        if (fire != null) fire.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (burntMaterial != null && foliage != null)
            foreach (Renderer r in foliage) if (r != null) r.sharedMaterial = burntMaterial;
        if (burntTrunkMaterial != null && trunk != null)
            foreach (Renderer r in trunk) if (r != null) r.sharedMaterial = burntTrunkMaterial;
    }
}
