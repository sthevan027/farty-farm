using UnityEngine;

/// <summary>Gira as rodinhas dos patins de acordo com a velocidade.</summary>
public class SkateWheels : MonoBehaviour
{
    [SerializeField] private Transform[] wheels;
    [SerializeField] private float radius = 0.14f;

    private CharacterBase character;

    private void Awake()
    {
        character = GetComponent<CharacterBase>();
    }

    private void Update()
    {
        if (character == null || wheels == null) return;
        // roda cilindro deitado (Euler 0,0,90): o eixo local Y aponta para -X do mundo
        float angle = -character.ForwardSpeed / radius * Mathf.Rad2Deg * Time.deltaTime;
        foreach (Transform w in wheels)
            if (w != null) w.Rotate(0f, angle, 0f, Space.Self);
    }
}
