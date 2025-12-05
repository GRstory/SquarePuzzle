using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapDataVisualizer : MonoBehaviour
{
    [SerializeField] private List<TextAsset> _levelJsonList = new List<TextAsset>();
    [SerializeField] private StageDrawer _stageDrawerPrefab;
    
    // Track loaded file paths to prevent duplicates
    private HashSet<string> _loadedFilePaths = new HashSet<string>();

    [SerializeField] private TMP_Text _stageNumberText;
    [SerializeField] private TMP_Text _moveCountText;
    [SerializeField] private Button _prevStageButton;
    [SerializeField] private Button _nextStageButton;
    [SerializeField] private Button _prevMoveButton;
    [SerializeField] private Button _nextMoveButton;
    [SerializeField] private Button _worldListButton;
    [SerializeField] private Button _playButton;
    [SerializeField] private TMP_Text _playButtonText;
    
    [Header("Play Mode UI")]
    [SerializeField] private TMP_Text _playModeInfoText; // "Current / Optimal"
    [SerializeField] private List<Image> _starImages; // 3 stars
    [SerializeField] private Sprite _starFullSprite;
    [SerializeField] private Sprite _starEmptySprite;

    [SerializeField] private Transform _mainViewParent;
    [SerializeField] private Transform _gridParent;
    [SerializeField] private WorldListPanel _worldListPanel;
    
    // Play mode prefabs
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private GameObject _wallPrefab;
    [SerializeField] private GameObject _goalPrefab;
    [SerializeField] private GameObject _breakableWallPrefab;
    [SerializeField] private GameObject _slideWallUpPrefab;
    [SerializeField] private GameObject _slideWallRightPrefab;
    [SerializeField] private GameObject _slideWallDownPrefab;
    [SerializeField] private GameObject _slideWallLeftPrefab;

    private int _currentStageIndex = 0;
    private int _currentMoveIndex = -1;
    private MapData _currentMapData = null;
    private StageDrawer _mainDrawer;
    private readonly List<StageDrawer> _gridDrawerList = new List<StageDrawer>();
    
    // Play mode state
    private bool _isPlayMode = false;
    private GameObject _playModeParent;
    private PlayerController _playModePlayer;
    private readonly List<GameObject> _playModeObjects = new List<GameObject>();
    private GameObject[,] _playModeGrid;
    private Dictionary<int, GameObject> _playModeWallIdMap = new Dictionary<int, GameObject>();

    private void OnPlayModeMove(int currentMoves)
    {
        if (_currentMapData == null) return;
        
        int optimalMoves = _currentMapData.OptimalPath.Count;
        
        // Update text: "Current / Optimal"
        if (_playModeInfoText != null)
        {
            _playModeInfoText.text = $"{currentMoves} / {optimalMoves}";
        }
        
        // Calculate stars
        // Base: 3 stars
        // Penalty: Every (Optimal * 0.3) extra moves reduces 1 star
        // Min stars: 1
        
        int stars = 3;
        if (currentMoves > optimalMoves)
        {
            float threshold = Mathf.Max(1f, optimalMoves * 0.3f);
            int penalty = Mathf.FloorToInt((currentMoves - optimalMoves) / threshold);
            stars = Mathf.Max(1, 3 - penalty);
        }
        
        // Update star sprites
        if (_starImages != null)
        {
            for (int i = 0; i < _starImages.Count; i++)
            {
                if (_starImages[i] != null)
                {
                    _starImages[i].sprite = (i < stars) ? _starFullSprite : _starEmptySprite;
                    _starImages[i].gameObject.SetActive(true);
                }
            }
        }
    }

    private void Start()
    {
        _prevStageButton.onClick.AddListener(LoadPrevStage);
        _nextStageButton.onClick.AddListener(LoadNextStage);
        _prevMoveButton.onClick.AddListener(StepPrevMove);
        _nextMoveButton.onClick.AddListener(StepNextMove);
        
        if (_worldListButton != null)
        {
            _worldListButton.onClick.AddListener(ToggleWorldList);
        }
        
        if (_playButton != null)
        {
            _playButton.onClick.AddListener(OnPlayStopButtonClicked);
        }

        // Hide Play Mode UI initially
        if (_playModeInfoText != null) _playModeInfoText.gameObject.SetActive(false);
        if (_starImages != null)
        {
            foreach (var star in _starImages)
            {
                if (star != null) star.gameObject.SetActive(false);
            }
        }

        _mainDrawer = Instantiate(_stageDrawerPrefab, _mainViewParent);
        
        // Load generated maps from folder
        LoadGeneratedMaps();
        
        if (_worldListPanel != null)
        {
            _worldListPanel.Initialize(_levelJsonList, LoadStage, OnWorldListVisibilityChanged);
            _worldListPanel.Hide();
        }
        
        LoadStage(_currentStageIndex);
    }

    private void OnDestroy()
    {
        _prevStageButton.onClick.RemoveAllListeners();
        _nextStageButton.onClick.RemoveAllListeners();
        _prevMoveButton.onClick.RemoveAllListeners();
        _nextMoveButton.onClick.RemoveAllListeners();
        
        if (_worldListButton != null)
        {
            _worldListButton.onClick.RemoveAllListeners();
        }
        
        if (_playButton != null)
        {
            _playButton.onClick.RemoveAllListeners();
        }
    }

    #region UI Functions
    private void LoadPrevStage()
    {
        LoadStage(_currentStageIndex - 1);
    }

    private void LoadNextStage()
    {
        LoadStage(_currentStageIndex + 1);
    }

    private void StepPrevMove()
    {
        _currentMoveIndex = Mathf.Max(_currentMoveIndex - 1, -1);
        _mainDrawer.DrawMap(_currentMapData, _currentMoveIndex, true);
        UpdateUI();
    }

    private void StepNextMove()
    {
        _currentMoveIndex = Mathf.Min(_currentMoveIndex + 1, _currentMapData.OptimalPath.Count - 1);
        _mainDrawer.DrawMap(_currentMapData, _currentMoveIndex, true);
        UpdateUI();
    }
    
    private void ToggleWorldList()
    {
        if (_worldListPanel != null)
        {
            // Check for new maps before showing the list
            int previousCount = _levelJsonList.Count;
            LoadGeneratedMaps();
            
            // Only reinitialize if new maps were loaded
            if (_levelJsonList.Count > previousCount)
            {
                _worldListPanel.Initialize(_levelJsonList, LoadStage, OnWorldListVisibilityChanged);
            }
            
            _worldListPanel.Toggle();
        }
    }
    
    private void OnWorldListVisibilityChanged(bool isVisible)
    {
        // Hide PlayMode button when WorldList is shown, show it when hidden
        if (_playButton != null)
        {
            _playButton.gameObject.SetActive(!isVisible);
        }
    }
    
    private void OnPlayStopButtonClicked()
    {
        if (_isPlayMode)
        {
            StopPlayMode();
        }
        else
        {
            StartPlayMode();
        }
    }
    
    private void StartPlayMode()
    {
        if (_isPlayMode || _currentMapData == null) return;
        
        _isPlayMode = true;
        
        // Hide visualization
        _mainDrawer.gameObject.SetActive(false);
        _prevMoveButton.gameObject.SetActive(false);
        _nextMoveButton.gameObject.SetActive(false);
        _moveCountText.gameObject.SetActive(false);
        
        // Show Play Mode UI
        if (_playModeInfoText != null) _playModeInfoText.gameObject.SetActive(true);
        if (_starImages != null)
        {
            foreach (var star in _starImages)
            {
                if (star != null) star.gameObject.SetActive(true);
            }
        }
        
        // Initialize UI
        OnPlayModeMove(0);
        
        // Update button
        if (_playButtonText != null)
        {
            _playButtonText.text = "Stop Playmode";
            _playButtonText.color = new Color32(0xBD, 0x49, 0x32, 0xFF);
        }
        
        // Create play mode parent
        _playModeParent = new GameObject("PlayModeObjects");
        RectTransform parentRect = _playModeParent.AddComponent<RectTransform>();
        parentRect.SetParent(_mainViewParent, false);
        
        // Match MainDrawer's RectTransform
        RectTransform mainDrawerRect = _mainDrawer.GetComponent<RectTransform>();
        parentRect.anchorMin = mainDrawerRect.anchorMin;
        parentRect.anchorMax = mainDrawerRect.anchorMax;
        parentRect.anchoredPosition = mainDrawerRect.anchoredPosition;
        parentRect.sizeDelta = mainDrawerRect.sizeDelta;
        parentRect.localScale = Vector3.one;
        
        // Calculate cell size
        float width = parentRect.rect.width;
        float height = parentRect.rect.height;
        float cellSize = Mathf.Min(width / _currentMapData.MapSize.x, height / _currentMapData.MapSize.y);
        
        _playModeParent.AddComponent<PlayModeCellSizeHolder>().cellSize = cellSize;
        
        // Initialize grid
        _playModeGrid = new GameObject[_currentMapData.MapSize.x, _currentMapData.MapSize.y];
        _playModeWallIdMap.Clear();
        
        // Create MapManager if needed
        if (MapManager.Instance == null)
        {
            GameObject mapManagerObj = new GameObject("TempMapManager");
            MapManager tempManager = mapManagerObj.AddComponent<MapManager>();
            _playModeObjects.Add(mapManagerObj);
        }
        
        MapManager.Instance.ParseJsonData(_levelJsonList[_currentStageIndex]);
        
        // Instantiate objects
        Vector2Int playerStartPos = Vector2Int.zero;
        int breakableWallIdCounter = 0;
        
        foreach (var obj in _currentMapData.MapObjects)
        {
            GameObject prefab = GetPrefabForType(obj.Type);
            if (prefab == null) continue;
            
            GameObject instance = Instantiate(prefab, _playModeParent.transform);
            _playModeObjects.Add(instance);
            
            RectTransform rectTransform = instance.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = instance.AddComponent<RectTransform>();
            }
            
            rectTransform.sizeDelta = new Vector2(cellSize, cellSize);
            rectTransform.anchoredPosition = new Vector2(obj.X * cellSize, obj.Y * cellSize);
            
            if (obj.Type == EMapObjectType.Player)
            {
                playerStartPos = new Vector2Int(obj.X, obj.Y);
                _playModePlayer = instance.GetComponent<PlayerController>();
                if (_playModePlayer != null)
                {
                    // Don't set position yet - wait until MapManager is ready
                    _playModePlayer.SetControlEnabled(true);
                    _playModePlayer.OnMoveCountChanged += OnPlayModeMove;
                }
            }
            else if (obj.Type == EMapObjectType.BreakableWall)
            {
                BreakableWall breakableWall = instance.GetComponent<BreakableWall>();
                if (breakableWall != null)
                {
                    breakableWall.SetWallId(breakableWallIdCounter);
                    _playModeWallIdMap[breakableWallIdCounter] = instance;
                    breakableWallIdCounter++;
                }
                _playModeGrid[obj.X, obj.Y] = instance;
            }
            
            if (obj.Type == EMapObjectType.Wall || obj.Type == EMapObjectType.Goal ||
                obj.Type == EMapObjectType.SlideWallUp || obj.Type == EMapObjectType.SlideWallRight ||
                obj.Type == EMapObjectType.SlideWallDown || obj.Type == EMapObjectType.SlideWallLeft)
            {
                _playModeGrid[obj.X, obj.Y] = instance;
            }
        }
        
        // Ensure player is rendered on top (last sibling)
        if (_playModePlayer != null)
        {
            _playModePlayer.transform.SetAsLastSibling();
        }
        
        // Register with MapManager
        if (MapManager.Instance != null)
        {
            for (int x = 0; x < _currentMapData.MapSize.x; x++)
            {
                for (int y = 0; y < _currentMapData.MapSize.y; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (_playModeGrid[x, y] != null)
                    {
                        MapManager.Instance.SetObjectAt(pos, _playModeGrid[x, y]);
                    }
                }
            }
            
            foreach (var kvp in _playModeWallIdMap)
            {
                MapManager.Instance.AddWallToIdMap(kvp.Key, kvp.Value);
            }
            
            // Now set player position and check for adjacent walls
            if (_playModePlayer != null)
            {
                _playModePlayer.SetInitialPosition(playerStartPos);
                Debug.Log($"Player position set to {playerStartPos} after MapManager registration");
            }
        }
    }
    
    private void StopPlayMode()
    {
        if (!_isPlayMode) return;
        
        _isPlayMode = false;
        
        // Hide Play Mode UI
        if (_playModeInfoText != null) _playModeInfoText.gameObject.SetActive(false);
        if (_starImages != null)
        {
            foreach (var star in _starImages)
            {
                if (star != null) star.gameObject.SetActive(false);
            }
        }
        
        if (_playModePlayer != null)
        {
            _playModePlayer.OnMoveCountChanged -= OnPlayModeMove;
            _playModePlayer.SetControlEnabled(false);
        }
        
        foreach (var obj in _playModeObjects)
        {
            if (obj != null) Destroy(obj);
        }
        _playModeObjects.Clear();
        
        if (_playModeParent != null)
        {
            Destroy(_playModeParent);
            _playModeParent = null;
        }
        
        _playModePlayer = null;
        _playModeGrid = null;
        _playModeWallIdMap.Clear();
        
        if (MapManager.Instance != null)
        {
            MapManager.Instance.ClearGrid();
        }
        
        // Show visualization
        _mainDrawer.gameObject.SetActive(true);
        _prevMoveButton.gameObject.SetActive(true);
        _nextMoveButton.gameObject.SetActive(true);
        _moveCountText.gameObject.SetActive(true);
        
        if (_playButtonText != null)
        {
            _playButtonText.text = "Play Manual";
            _playButtonText.color = Color.black;
        }
        
        LoadStage(_currentStageIndex);
    }
    
    private GameObject GetPrefabForType(EMapObjectType type)
    {
        switch (type)
        {
            case EMapObjectType.Player: return _playerPrefab;
case EMapObjectType.Wall: return _wallPrefab;
            case EMapObjectType.Goal: return _goalPrefab;
            case EMapObjectType.BreakableWall: return _breakableWallPrefab;
            case EMapObjectType.SlideWallUp: return _slideWallUpPrefab;
            case EMapObjectType.SlideWallRight: return _slideWallRightPrefab;
            case EMapObjectType.SlideWallDown: return _slideWallDownPrefab;
            case EMapObjectType.SlideWallLeft: return _slideWallLeftPrefab;
            default: return null;
        }
    }
    
    public void ResetPlayMode()
    {
        if (!_isPlayMode) return;
        
        StopPlayMode();
        StartPlayMode();
    }
    #endregion

    private void LoadGeneratedMaps()
    {
        // Always load from StreamingAssets/Maps folder (works in both Editor and Build)
        string streamingAssetsPath = System.IO.Path.Combine(Application.streamingAssetsPath, "Maps");
        
        if (!System.IO.Directory.Exists(streamingAssetsPath))
        {
            Debug.LogWarning($"[MapDataVisualizer] StreamingAssets/Maps folder not found: {streamingAssetsPath}");
            return;
        }

        string[] jsonFiles = System.IO.Directory.GetFiles(streamingAssetsPath, "*.json");
        int newMapsLoaded = 0;
        
        foreach (string filePath in jsonFiles)
        {
            // Skip if already loaded
            if (_loadedFilePaths.Contains(filePath))
            {
                continue;
            }
            
            try
            {
                string jsonText = System.IO.File.ReadAllText(filePath);
                
                // Create a TextAsset from the JSON text
                TextAsset textAsset = new TextAsset(jsonText);
                
                _levelJsonList.Add(textAsset);
                _loadedFilePaths.Add(filePath);
                newMapsLoaded++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MapDataVisualizer] Failed to load map from {filePath}: {e.Message}");
            }
        }

        if (newMapsLoaded > 0)
        {
            Debug.Log($"[MapDataVisualizer] Loaded {newMapsLoaded} new maps from StreamingAssets/Maps (Total: {_levelJsonList.Count})");
        }
    }

    private void LoadStage(int index)
    {
        if (_levelJsonList.Count <= index || index < 0) return;
        _currentStageIndex = index;
        _currentMapData = JsonUtility.FromJson<MapData>(_levelJsonList[_currentStageIndex].text);
        _currentMoveIndex = -1;

        var mainDrawerRect = _mainDrawer.GetComponent<RectTransform>();
        mainDrawerRect.anchorMin = Vector2.zero;
        mainDrawerRect.anchorMax = Vector2.one;
        mainDrawerRect.anchoredPosition = Vector2.zero;
        mainDrawerRect.sizeDelta = Vector2.one;

        _mainDrawer.DrawMap(_currentMapData, _currentMoveIndex, true);

        foreach(var drawer in _gridDrawerList)
        {
            if (drawer != null)
            {
                Destroy(drawer.gameObject);
            }
        }
        _gridDrawerList.Clear();

        for(int i = 0; i <= _currentMapData.OptimalPath.Count; i++)
        {
            var drawer = Instantiate(_stageDrawerPrefab, _gridParent);
            _gridDrawerList.Add(drawer);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_gridParent.GetComponent<RectTransform>());
        }

        for (int i = 0; i <= _currentMapData.OptimalPath.Count; i++)
        {
            _gridDrawerList[i].DrawMap(_currentMapData, i - 1, false, true, i);
        }
        
        UpdateUI();
    }

    private void UpdateUI()
    {
        _stageNumberText.text = $"World: {_currentStageIndex.ToString()}";
        _moveCountText.text = $"Current Step: {(_currentMoveIndex + 1).ToString()}";
    }
}
