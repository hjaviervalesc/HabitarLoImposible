using UnityEngine;
using UnityEditor;

public class ConvertStandardToURP
{
    [MenuItem("Tools/Materials/Convert Selected Materials to URP")]
    static void ConvertSelectedMaterials()
    {
        Object[] selectedObjects = Selection.objects;

        int converted = 0;

        foreach (Object obj in selectedObjects)
        {
            Material material = obj as Material;

            if (material == null)
                continue;

            if (material.shader == null)
                continue;

            // Solo convertir materiales que utilizan el Standard Shader
            if (material.shader.name != "Standard")
                continue;

            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");

            if (urpShader == null)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "No se ha encontrado el shader Universal Render Pipeline/Lit.",
                    "OK"
                );

                return;
            }

            // Guardamos las propiedades del Standard
            Texture albedo = material.GetTexture("_MainTex");
            Color color = material.GetColor("_Color");

            Texture metallicGloss = null;

            if (material.HasProperty("_MetallicGlossMap"))
                metallicGloss = material.GetTexture("_MetallicGlossMap");

            float metallic = 0f;

            if (material.HasProperty("_Metallic"))
                metallic = material.GetFloat("_Metallic");

            float smoothness = 0.5f;

            if (material.HasProperty("_Glossiness"))
                smoothness = material.GetFloat("_Glossiness");

            Texture normalMap = null;

            if (material.HasProperty("_BumpMap"))
                normalMap = material.GetTexture("_BumpMap");

            float normalScale = 1f;

            if (material.HasProperty("_BumpScale"))
                normalScale = material.GetFloat("_BumpScale");

            // Cambiamos al shader URP
            material.shader = urpShader;

            // Base Color / Albedo
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);

            // Base Map
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", albedo);

            // Metallic
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);

            // Metallic Map
            if (metallicGloss != null &&
                material.HasProperty("_MetallicGlossMap"))
            {
                material.SetTexture("_MetallicGlossMap", metallicGloss);
            }

            // Smoothness
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);

            // Normal Map
            if (normalMap != null &&
                material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap", normalMap);

                if (material.HasProperty("_BumpScale"))
                    material.SetFloat("_BumpScale", normalScale);
            }

            EditorUtility.SetDirty(material);

            converted++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Conversión completada",
            "Materiales convertidos: " + converted,
            "OK"
        );
    }
}