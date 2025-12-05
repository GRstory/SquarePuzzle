using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI panel for automatic map generation
/// Generates maps, validates them, and saves to JSON files
/// </summary>
public class MapGeneratorUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField _targetMovesInput;
    [SerializeField] private TMP_InputField _seedInput;
    [SerializeField] private Button _generateButton;
    [SerializeField] private TMP_InputField _batchCountInput;
    [SerializeField] private TMP_InputField _batchMinMovesInput;
    [SerializeField] private Button _batchGenerateButton;
    [SerializeField] private TMP_Text _statusText;
    [SerializeField] private TMP_Text _statsText;
    [SerializeField] private GameObject _panel;

    [Header("Settings")]
    [SerializeField] private string _saveDirectory = "Assets/Map/Generate";
    [SerializeField] private string _filePrefix = "generated_map_";
    [SerializeField] private int _maxRetries = 10000;

    private MapData _lastGeneratedMap;
    private bool _isGenerating = false;
    private HashSet<int> _existingSeeds = new HashSet<int>();

    private void Start()
    {
        if (_generateButton != null)
        {
            _generateButton.onClick.AddListener(OnGenerateButtonClicked);
        }
        
        if (_batchGenerateButton != null)
        {
            _batchGenerateButton.onClick.AddListener(OnBatchGenerateButtonClicked);
        }

        // Load existing seeds from saved maps
        LoadExistingSeeds();

        UpdateStatus("Ready to generate maps", Color.white);
        UpdateStats("");
    }

    private void OnDestroy()
    {
        if (_generateButton != null)
        {
            _generateButton.onClick.RemoveAllListeners();
        }
        
        if (_batchGenerateButton != null)
        {
            _batchGenerateButton.onClick.RemoveAllListeners();
        }
    }

    public void Show()
    {
        if (_panel != null)
        {
            _panel.SetActive(true);
        }
    }

    public void Hide()
    {
        if (_panel != null)
        {
            _panel.SetActive(false);
        }
    }

    public void Toggle()
    {
        if (_panel != null)
        {
            _panel.SetActive(!_panel.activeSelf);
        }
    }

    private void OnGenerateButtonClicked()
    {
        if (_isGenerating)
        {
            UpdateStatus("Already generating...", Color.yellow);
            return;
        }

        // Parse target moves
        if (!int.TryParse(_targetMovesInput.text, out int targetMoves))
        {
            UpdateStatus("Invalid target moves! Please enter a number (1-20)", Color.red);
            return;
        }

        if (targetMoves < 1 || targetMoves > 20)
        {
            UpdateStatus("Target moves must be between 1 and 20", Color.red);
            return;
        }

        // Parse seed (optional)
        int? seed = null;
        if (!string.IsNullOrEmpty(_seedInput.text))
        {
            if (int.TryParse(_seedInput.text, out int parsedSeed))
            {
                seed = parsedSeed;
            }
        }

        // Start generation
        StartCoroutine(GenerateMapCoroutine(targetMoves, seed));
    }

    private void OnBatchGenerateButtonClicked()
    {
        if (_isGenerating)
        {
            UpdateStatus("Already generating...", Color.yellow);
            return;
        }

        // Parse batch count
        if (!int.TryParse(_batchCountInput.text, out int batchCount))
        {
            UpdateStatus("Invalid batch count! Please enter a number", Color.red);
            return;
        }

        if (batchCount < 1 || batchCount > 100)
        {
            UpdateStatus("Batch count must be between 1 and 100", Color.red);
            return;
        }

        // Parse min moves
        int minMoves = 1;
        if (!string.IsNullOrEmpty(_batchMinMovesInput.text))
        {
            if (!int.TryParse(_batchMinMovesInput.text, out minMoves))
            {
                UpdateStatus("Invalid min moves! Please enter a number", Color.red);
                return;
            }

            if (minMoves < 1 || minMoves > 20)
            {
                UpdateStatus("Min moves must be between 1 and 20", Color.red);
                return;
            }
        }

        // Start batch generation
        StartCoroutine(BatchGenerateCoroutine(batchCount, minMoves));
    }

    private IEnumerator GenerateMapCoroutine(int targetMoves, int? seed)
    {
        _isGenerating = true;
        _generateButton.interactable = false;

        UpdateStatus($"Generating map with {targetMoves} moves...", Color.yellow);
        UpdateStats("Generation in progress...");

        float startTime = Time.realtimeSinceStartup;

        // Run generation in background (note: this still blocks but shows status)
        yield return null;

        MapData generatedMap = MapGenerator.GenerateMapWithRetry(targetMoves, _maxRetries, seed);

        float elapsedTime = Time.realtimeSinceStartup - startTime;

        if (generatedMap != null)
        {
            _lastGeneratedMap = generatedMap;

            // Check if duplicate before saving
            bool isDuplicate = IsDuplicateSeed(generatedMap.Seed);

            // Auto-save the map
            string savedPath = SaveMapToFile(generatedMap, targetMoves);

            if (!string.IsNullOrEmpty(savedPath))
            {
                UpdateStatus($"SUCCESS! Map saved to: {savedPath}", Color.green);
                UpdateStats($"Target Moves: {targetMoves}\n" +
                           $"Actual Moves: {generatedMap.OptimalPath.Count}\n" +
                           $"Objects: {generatedMap.MapObjects.Count}\n" +
                           $"Seed: {generatedMap.Seed}\n" +
                           $"Time: {elapsedTime:F2}s");
            }
            else if (isDuplicate)
            {
                UpdateStatus($"Map with seed {generatedMap.Seed} already exists. Not saved.", Color.yellow);
                UpdateStats($"Target Moves: {targetMoves}\n" +
                           $"Actual Moves: {generatedMap.OptimalPath.Count}\n" +
                           $"Seed: {generatedMap.Seed} (DUPLICATE)\n" +
                           $"Time: {elapsedTime:F2}s");
            }
            else
            {
                UpdateStatus($"Map generated but failed to save", Color.yellow);
                UpdateStats($"Target Moves: {targetMoves}\n" +
                           $"Actual Moves: {generatedMap.OptimalPath.Count}\n" +
                           $"Time: {elapsedTime:F2}s");
            }
        }
        else
        {
            UpdateStatus($"FAILED to generate map with {targetMoves} moves after {_maxRetries} attempts", Color.red);
            UpdateStats($"Time: {elapsedTime:F2}s\n" +
                       $"Try adjusting target moves or seed");
        }

        _isGenerating = false;
        _generateButton.interactable = true;
    }

    private IEnumerator BatchGenerateCoroutine(int count, int minMoves)
    {
        _isGenerating = true;
        _generateButton.interactable = false;
        _batchGenerateButton.interactable = false;

        UpdateStatus($"Batch generating {count} maps (min {minMoves} moves)...", Color.yellow);
        UpdateStats("Generation in progress...");

        float startTime = Time.realtimeSinceStartup;
        int successCount = 0;
        int totalAttempts = 0;
        int maxTotalAttempts = count * 100; // Prevent infinite loop

        while (successCount < count && totalAttempts < maxTotalAttempts)
        {
            totalAttempts++;
            UpdateStatus($"Generating map {successCount + 1}/{count} (attempt {totalAttempts})...", Color.yellow);
            yield return null;

            // Generate random map (random moves between minMoves and 15, no seed)
            int randomMoves = Random.Range(minMoves, 16);
            MapData generatedMap = MapGenerator.GenerateMapWithRetry(randomMoves, _maxRetries, null);

            if (generatedMap != null)
            {
                // Check if duplicate before saving
                bool isDuplicate = IsDuplicateSeed(generatedMap.Seed);
                
                string savedPath = SaveMapToFile(generatedMap, randomMoves);
                if (!string.IsNullOrEmpty(savedPath))
                {
                    successCount++;
                    Debug.Log($"[Batch {successCount}/{count}] SUCCESS: {randomMoves} moves -> {savedPath}");
                }
                else if (isDuplicate)
                {
                    Debug.LogWarning($"[Batch attempt {totalAttempts}] DUPLICATE: Map with seed {generatedMap.Seed} already exists. Skipping.");
                }
                else
                {
                    Debug.LogWarning($"[Batch attempt {totalAttempts}] Map generated but failed to save");
                }
            }
            else
            {
                Debug.LogWarning($"[Batch attempt {totalAttempts}] FAILED: Could not generate {randomMoves} moves map");
            }
        }

        float elapsedTime = Time.realtimeSinceStartup - startTime;

        UpdateStatus($"Batch generation complete!", Color.green);
        UpdateStats($"Requested: {count}\n" +
                   $"Generated: {successCount}\n" +
                   $"Total Attempts: {totalAttempts}\n" +
                   $"Success Rate: {(successCount * 100f / totalAttempts):F1}%\n" +
                   $"Min Moves: {minMoves}\n" +
                   $"Time: {elapsedTime:F2}s\n" +
                   $"Avg: {(elapsedTime / successCount):F2}s per map");

        _isGenerating = false;
        _generateButton.interactable = true;
        _batchGenerateButton.interactable = true;
    }

    private void LoadExistingSeeds()
    {
        _existingSeeds.Clear();
        
        string streamingAssetsPath = System.IO.Path.Combine(Application.streamingAssetsPath, "Maps");
        if (!System.IO.Directory.Exists(streamingAssetsPath))
        {
            return;
        }

        string[] jsonFiles = System.IO.Directory.GetFiles(streamingAssetsPath, "*.json");
        
        foreach (string filePath in jsonFiles)
        {
            try
            {
                string jsonText = System.IO.File.ReadAllText(filePath);
                MapData mapData = JsonUtility.FromJson<MapData>(jsonText);
                
                if (mapData != null && mapData.Seed != 0)
                {
                    _existingSeeds.Add(mapData.Seed);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[MapGeneratorUI] Failed to read seed from {filePath}: {e.Message}");
            }
        }

        Debug.Log($"[MapGeneratorUI] Loaded {_existingSeeds.Count} existing seeds");
    }

    private bool IsDuplicateSeed(int seed)
    {
        return _existingSeeds.Contains(seed);
    }

    private string SaveMapToFile(MapData mapData, int targetMoves)
    {
        try
        {
            // Check for duplicate seed
            if (IsDuplicateSeed(mapData.Seed))
            {
                Debug.LogWarning($"[MapGeneratorUI] Map with seed {mapData.Seed} already exists. Skipping save.");
                return null;
            }

            // Create filename with timestamp
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filename = $"{_filePrefix}{targetMoves}moves_{timestamp}.json";
            
            // Convert to JSON
            string json = JsonUtility.ToJson(mapData, true);
            
            // Save to StreamingAssets/Maps folder (works for both Editor and Build)
            string streamingAssetsPath = System.IO.Path.Combine(Application.streamingAssetsPath, "Maps");
            if (!System.IO.Directory.Exists(streamingAssetsPath))
            {
                System.IO.Directory.CreateDirectory(streamingAssetsPath);
                Debug.Log($"[MapGeneratorUI] Created StreamingAssets/Maps directory: {streamingAssetsPath}");
            }
            
            string fullPath = System.IO.Path.Combine(streamingAssetsPath, filename);
            System.IO.File.WriteAllText(fullPath, json);
            
            // Add seed to existing seeds set
            _existingSeeds.Add(mapData.Seed);
            
            Debug.Log($"[MapGeneratorUI] Saved map to: {fullPath} (Seed: {mapData.Seed})");

#if UNITY_EDITOR
            // Refresh asset database in editor
            UnityEditor.AssetDatabase.Refresh();
#endif

            return fullPath;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MapGeneratorUI] Failed to save map: {e.Message}");
            return null;
        }
    }

    private void UpdateStatus(string message, Color color)
    {
        if (_statusText != null)
        {
            _statusText.text = message;
            _statusText.color = color;
        }
        Debug.Log($"[MapGeneratorUI] {message}");
    }

    private void UpdateStats(string stats)
    {
        if (_statsText != null)
        {
            _statsText.text = stats;
        }
    }

    public MapData GetLastGeneratedMap()
    {
        return _lastGeneratedMap;
    }
}