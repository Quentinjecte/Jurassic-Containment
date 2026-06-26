#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RaptorAttackCinematicDirector))]
public class RaptorAttackCinematicDirectorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        var director = (RaptorAttackCinematicDirector)target;

        EditorGUI.BeginDisabledGroup(!Application.isPlaying);
        if (GUILayout.Button("▶ Play Cinematic"))
            director.PlayCinematic();

        if (GUILayout.Button("■ Stop Cinematic"))
            director.StopCinematic();
        EditorGUI.EndDisabledGroup();

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("Lancez le mode Play pour tester la cinématique depuis l'Inspector.", MessageType.Info);
    }
}
#endif
