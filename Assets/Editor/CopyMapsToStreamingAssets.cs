using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor utility to copy generated maps to StreamingAssets folder
/// This ensures maps are available in builds
/// </summary>
public class CopyMapsToStreamingAssets : EditorWindow
{
    private string sourceFolder = "Assets/Map/Generate";
    private string targetFolder = "StreamingAssets/Maps";

    [MenuItem("Tools/Copy Maps to StreamingAssets")]
    public static void ShowWindow()
    {
        GetWindow<CopyMapsToStreamingAssets>("Copy Maps");
    }

    private void OnGUI()
    {
        GUILayout.Label("Copy Generated Maps to StreamingAssets", EditorStyles.boldLabel);
        GUILayout.Space(10);

        sourceFolder = EditorGUILayout.TextField("Source Folder:", sourceFolder);
        targetFolder = EditorGUILayout.TextField("Target Folder:", targetFolder);

        GUILayout.Space(10);

        if (GUILayout.Button("Copy All Maps", GUILayout.Height(30)))
        {
            CopyMaps();
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "This will copy all JSON files from the source folder to StreamingAssets/Maps.\n" +
            "Maps in StreamingAssets will be included in builds.",
            MessageType.Info);
    }

    private void CopyMaps()
    {
        try
        {
            // Ensure source directory exists
            if (!Directory.Exists(sourceFolder))
            {
                EditorUtility.DisplayDialog("Error", $"Source folder not found: {sourceFolder}", "OK");
                return;
            }

            // Create target directory if it doesn't exist
            string fullTargetPath = Path.Combine(Application.dataPath, "..", targetFolder);
            if (!Directory.Exists(fullTargetPath))
            {
                Directory.CreateDirectory(fullTargetPath);
                Debug.Log($"Created directory: {fullTargetPath}");
            }

            // Get all JSON files from source
            string[] jsonFiles = Directory.GetFiles(sourceFolder, "*.json");

            if (jsonFiles.Length == 0)
            {
                EditorUtility.DisplayDialog("Info", "No JSON files found in source folder.", "OK");
                return;
            }

            int copiedCount = 0;
            foreach (string sourceFile in jsonFiles)
            {
                string fileName = Path.GetFileName(sourceFile);
                string targetFile = Path.Combine(fullTargetPath, fileName);

                File.Copy(sourceFile, targetFile, true);
                copiedCount++;
            }

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Success",
                $"Copied {copiedCount} map files to {targetFolder}",
                "OK");

            Debug.Log($"[CopyMapsToStreamingAssets] Copied {copiedCount} files from {sourceFolder} to {fullTargetPath}");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to copy maps: {e.Message}", "OK");
            Debug.LogError($"[CopyMapsToStreamingAssets] Error: {e.Message}");
        }
    }
}
