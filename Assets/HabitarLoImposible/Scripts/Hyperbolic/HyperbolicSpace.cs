using UnityEngine;

/// <summary>
/// Contiene la configuración compartida del disco de Poincaré.
/// Es la única clase que almacena DiskScale y la configuración
/// general de compresión visual.
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


    public float DiskScale => diskScale;

    public float ScaleIntensity => scaleIntensity;

    public float MinimumVisualScale =>
        minimumVisualScale;

    public float UnityRadius => diskScale;


    /// <summary>
    /// Convierte una coordenada del disco en una
    /// posición local del plano X-Z de Unity.
    /// </summary>
    public Vector3 PoincareToUnity(
        Vector2 poincarePosition,
        float height = 0f
    )
    {
        return new Vector3(
            poincarePosition.x * diskScale,
            height,
            poincarePosition.y * diskScale
        );
    }


    /// <summary>
    /// Convierte una coordenada del disco en una
    /// posición bidimensional del plano X-Z.
    /// </summary>
    public Vector2 PoincareToUnity2D(
        Vector2 poincarePosition
    )
    {
        return poincarePosition * diskScale;
    }


    /// <summary>
    /// Convierte una posición local de Unity
    /// en una coordenada del disco de Poincaré.
    /// </summary>
    public Vector2 UnityToPoincare(
        Vector3 unityPosition
    )
    {
        return new Vector2(
            unityPosition.x / diskScale,
            unityPosition.z / diskScale
        );
    }


    /// <summary>
    /// Convierte una posición bidimensional de Unity
    /// en una coordenada del disco de Poincaré.
    /// </summary>
    public Vector2 UnityToPoincare(
        Vector2 unityPosition
    )
    {
        return unityPosition / diskScale;
    }


    /// <summary>
    /// Calcula el factor de compresión visual:
    ///
    /// s(p) = (1 - |p|²)^intensidad
    /// </summary>
    public float GetScaleFactor(
        Vector2 poincarePosition
    )
    {
        float baseScale = Mathf.Clamp01(
            1f - poincarePosition.sqrMagnitude
        );

        float scaleFactor = Mathf.Pow(
            baseScale,
            scaleIntensity
        );

        return Mathf.Max(
            minimumVisualScale,
            scaleFactor
        );
    }


    /// <summary>
    /// Comprueba si un punto está dentro del disco.
    /// </summary>
    public bool IsInsideDisk(
        Vector2 poincarePosition
    )
    {
        return poincarePosition.sqrMagnitude < 1f;
    }


    /// <summary>
    /// Comprueba si un punto está dentro
    /// del radio matemático indicado.
    /// </summary>
    public bool IsInsideDisk(
        Vector2 poincarePosition,
        float allowedRadius
    )
    {
        float radius = Mathf.Clamp01(
            allowedRadius
        );

        return poincarePosition.sqrMagnitude <
               radius * radius;
    }


    /// <summary>
    /// Limita un punto para impedir que atraviese
    /// el radio permitido del disco.
    /// </summary>
    public Vector2 ClampInsideDisk(
        Vector2 poincarePosition,
        float allowedRadius = 0.99f
    )
    {
        float radius = Mathf.Clamp(
            allowedRadius,
            0.001f,
            0.999f
        );

        if (poincarePosition.sqrMagnitude <=
            radius * radius)
        {
            return poincarePosition;
        }

        return poincarePosition.normalized * radius;
    }
}