using UnityEngine;
using UnityEditor;
using System.IO;

public class FixExternalMaterialWarnings : EditorWindow
{
    [MenuItem("Tools/Fixer/Convert External Materials")]
    public static void FixMaterials()
    {
        string[] fbxGUIDs = AssetDatabase.FindAssets("t:Model");

        int count = 0;

        foreach (string guid in fbxGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                continue;

            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;

            if (importer == null)
                continue;

            // Si l'ancien mode "External" est détecté, on le remplace
            if (importer.materialLocation != ModelImporterMaterialLocation.InPrefab)
            {
                importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                importer.SaveAndReimport();
                count++;
                continue;
            }
            else
            {
                Debug.Log($"⚠ Le fichier FBX '{path}' n'utilise pas le mode 'External'. Aucune modification effectuée.");
            }
        }

        Debug.Log($"✔ Conversion terminée. FBX corrigés : {count}");
    }
}
