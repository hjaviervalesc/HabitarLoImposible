using UnityEngine;

/// <summary>
/// Contiene la configuración compartida del disco de Poincaré, su escala de representación y la compresión visual.
/// </summary>
public class HyperbolicSpace : MonoBehaviour
{
    [Header("Representación del disco")]
    [Min(0.01f)]
    [SerializeField]
    private float diskScale = 50f;

    [Header("Compresión hiperbólica")]
    [Range(0.1f, 2f)]
    [SerializeField]
    private float scaleIntensity = 1f;

    [Range(0.001f, 0.25f)]
    [SerializeField]
    private float minimumVisualScale = 0.02f;


    /// <summary>
    /// Convierte una coordenada del disco en una posición local del plano X-Z de Unity con la altura indicada.
    /// </summary>
    public Vector3 PoincareToUnity(Vector2 poincarePosition, float height = 0f)
    {
        return new Vector3(poincarePosition.x * diskScale, height, poincarePosition.y * diskScale);
    }

    /// <summary>
    /// Convierte una coordenada del disco en una posición bidimensional que representa el plano X-Z local de Unity.
    /// </summary>
    public Vector2 PoincareToUnity2D(Vector2 poincarePosition)
    {
        return poincarePosition * diskScale;
    }


    /// <summary>
    /// Convierte una posición bidimensional del plano local de Unity en una coordenada del disco de Poincaré.
    /// </summary>
    public Vector2 UnityToPoincare(Vector2 unityPosition)
    {
        return unityPosition / diskScale;
    }

    /// <summary>
    /// Calcula el factor de compresión visual a partir de la distancia al centro y la intensidad, respetando la escala visual mínima.
    /// </summary>
    public float GetScaleFactor(Vector2 poincarePosition)
    {
        float baseScale = Mathf.Clamp01(1f - poincarePosition.sqrMagnitude);

        float scaleFactor = Mathf.Pow(baseScale, scaleIntensity);

        return Mathf.Max(minimumVisualScale, scaleFactor);
    }


    /// <summary>
    /// Comprueba si un punto está estrictamente dentro del radio indicado, limitado al intervalo entre cero y uno.
    /// </summary>
    public bool IsInsideDisk(Vector2 poincarePosition, float allowedRadius)
    {
        float radius = Mathf.Clamp01(allowedRadius);

        return poincarePosition.sqrMagnitude < radius * radius;
    }

    /// <summary>
    /// Conserva los puntos dentro del radio permitido y desplaza radialmente hasta ese límite los que lo superan.
    /// </summary>
    public Vector2 ClampInsideDisk(Vector2 poincarePosition, float allowedRadius = 0.99f)
    {
        float radius = Mathf.Clamp(allowedRadius, 0.001f, 0.999f);

        if (poincarePosition.sqrMagnitude <= radius * radius)
        {
            return poincarePosition;
        }

        return poincarePosition.normalized * radius;
    }
}