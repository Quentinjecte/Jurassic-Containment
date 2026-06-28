using UnityEditor;
using UnityEngine;

// ② L'editor (dans Assets/Editor/)
[CustomEditor(typeof(CinemachineSplineSpeedController))]
public class CinemachineSplineSpeedControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Dessine l'inspector Unity standard (tous les champs publics)
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Refrech"))
        {
            // Accès direct à target (le composant) pour des opérations custom
            var comp = (CinemachineSplineSpeedController)target;
            comp.RefreshKnotList();
            EditorUtility.SetDirty(comp);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
