using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Moeda coletavel com seta flutuante estilo GTA V (a seta e criada pelo CollectibleManager).
/// So o personagem controlado pelo jogador coleta.
/// </summary>
public class Collectible : MonoBehaviour
{
    public static readonly List<Collectible> All = new List<Collectible>();

    [SerializeField] private Transform spinner;
    [SerializeField] private Transform marker;
    [SerializeField] private float spinSpeed = 140f;

    private CollectibleManager manager;
    private Camera cam;
    private float phase;
    private bool collected;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        All.Clear();
    }

    public void Init(CollectibleManager owner, Transform spinnerTransform, Transform markerTransform)
    {
        manager = owner;
        spinner = spinnerTransform;
        marker = markerTransform;
        phase = Random.value * 6.28f;
    }

    private void OnEnable() { All.Add(this); }
    private void OnDisable() { All.Remove(this); }

    private void Update()
    {
        if (cam == null && Camera.main != null) cam = Camera.main;
        float t = Time.time + phase;

        if (spinner != null)
        {
            spinner.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
            spinner.localPosition = new Vector3(0f, 1.0f + Mathf.Sin(t * 2.2f) * 0.12f, 0f);
        }

        if (marker != null)
        {
            // a seta cresce com a distancia para continuar legivel de longe (como no GTA)
            float dist = cam != null ? Vector3.Distance(cam.transform.position, transform.position) : 16f;
            float scale = Mathf.Clamp(dist / 16f, 0.9f, 2.4f);
            marker.localScale = Vector3.one * scale;
            marker.localPosition = new Vector3(0f, 2.5f * scale + Mathf.Sin(t * 3f) * 0.2f, 0f);
            marker.Rotate(0f, 180f * Time.deltaTime, 0f, Space.World);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        CharacterBase character = other.GetComponentInParent<CharacterBase>();
        if (character == null || !character.IsPlayer) return;

        collected = true;
        if (manager != null) manager.OnCollected(this, character);
        Destroy(gameObject);
    }
}
