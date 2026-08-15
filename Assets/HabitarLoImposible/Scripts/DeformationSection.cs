using System.Collections.Generic;

public class DeformationSection
{
    private int sectionIndex;

    private float innerRadius;
    private float outerRadius;

    private List<int> vertexIndices =
        new List<int>();

    private float currentOffset = 0f;


    public int SectionIndex => sectionIndex;

    public float InnerRadius => innerRadius;

    public float OuterRadius => outerRadius;

    public List<int> VertexIndices => vertexIndices;

    public float CurrentOffset => currentOffset;


    public DeformationSection(
        int index,
        float inner,
        float outer)
    {
        sectionIndex = index;

        innerRadius = inner;

        outerRadius = outer;
    }


    public void AddVertex(int vertexIndex)
    {
        vertexIndices.Add(vertexIndex);
    }


    public void AddDeformation(float amount)
    {
        currentOffset += amount;
    }
}