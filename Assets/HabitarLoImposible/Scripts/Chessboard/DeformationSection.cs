using System.Collections.Generic;

/// <summary>
/// Representa una sección concéntrica de la malla y almacena sus vértices
/// y el desplazamiento vertical acumulado.
/// </summary>
public class DeformationSection
{
    private int sectionIndex;
    private float innerRadius;
    private float outerRadius;
    private List<int> vertexIndices = new List<int>();
    private float currentOffset = 0f;

    public float InnerRadius => innerRadius;
    public float OuterRadius => outerRadius;
    public List<int> VertexIndices => vertexIndices;
    public float CurrentOffset => currentOffset;

    /// <summary>
    /// Crea una sección comprendida entre un radio interior y uno exterior.
    /// </summary>
    public DeformationSection(int index, float inner, float outer)
    {
        sectionIndex = index;
        innerRadius = inner;
        outerRadius = outer;
    }

    /// <summary>
    /// Añade un vértice de la malla a la sección.
    /// </summary>
    public void AddVertex(int vertexIndex)
    {
        vertexIndices.Add(vertexIndex);
    }

    /// <summary>
    /// Acumula un desplazamiento vertical en la sección.
    /// </summary>
    public void AddDeformation(float amount)
    {
        currentOffset += amount;
    }
}