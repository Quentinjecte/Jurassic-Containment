using UnityEngine;
using UnityEditor;
using System.IO;

public class ScreenShotWindow : EditorWindow
{
    [MenuItem("Tools/Screenshots/Take Screenshot")]
    public static void ScreenShot()
    {
        string folder = "C:/Users/quent/Desktop/UnityProg/Jurassic-Containment/Assets/ScreenShots/";

        // Crée le dossier si nécessaire
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string filePath = $"Assets/ScreenShots/screenshot_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
        ScreenCapture.CaptureScreenshot(filePath, 4);

        Debug.Log("Screenshot saved to: " + filePath);
    }
}
