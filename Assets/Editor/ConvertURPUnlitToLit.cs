using UnityEditor;
using UnityEngine;

public static class ConvertURPUnlitToLit
{
    [MenuItem("Tools/Materials/Convert All URP Unlit To URP Lit")]
    public static void ConvertAllMaterials()
    {
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");

        if (litShader == null)
        {
            Debug.LogError("Shader 'Universal Render Pipeline/Lit' não encontrado. Verifique se o projeto está usando URP.");
            return;
        }

        string[] materialGuids = AssetDatabase.FindAssets("t:Material");

        int convertedCount = 0;

        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null || material.shader == null)
                continue;

            string shaderName = material.shader.name;

            bool isURPUnlit =
                shaderName == "Universal Render Pipeline/Unlit" ||
                shaderName.Contains("URP") && shaderName.Contains("Unlit") ||
                shaderName.Contains("Universal Render Pipeline") && shaderName.Contains("Unlit");

            if (!isURPUnlit)
                continue;

            Texture baseMap = null;
            Color baseColor = Color.white;

            if (material.HasProperty("_BaseMap"))
                baseMap = material.GetTexture("_BaseMap");

            if (material.HasProperty("_BaseColor"))
                baseColor = material.GetColor("_BaseColor");

            Undo.RecordObject(material, "Convert URP Unlit To Lit");

            material.shader = litShader;

            if (material.HasProperty("_BaseMap") && baseMap != null)
                material.SetTexture("_BaseMap", baseMap);

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", baseColor);

            EditorUtility.SetDirty(material);
            convertedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Conversão concluída. Materiais convertidos: {convertedCount}");
    }
}