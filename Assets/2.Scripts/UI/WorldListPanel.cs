using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WorldListPanel : MonoBehaviour
{
    [SerializeField] private GameObject _worldPreviewPrefab;
    [SerializeField] private Transform _gridContainer; // Scroll View의 Content 오브젝트를 할당
    [SerializeField] private Vector2 _previewSize = new Vector2(200, 200); // 미리보기 크기
    [SerializeField] private TMP_Dropdown _filterDropdown; // 필터 드롭다운
    [SerializeField] private TMP_Dropdown _sortDropdown; // 정렬 드롭다운
    
    private List<GameObject> _previewList = new List<GameObject>();
    private List<TextAsset> _fullLevelList = new List<TextAsset>(); // 전체 레벨 목록 저장
    private int _selectedMoveCount = -1; // -1 means "All"
    private SortMode _currentSortMode = SortMode.ByIndex;
    private System.Action<int> _onWorldSelectedCallback; // Callback when world is selected

    private enum SortMode
    {
        ByIndex,      // 순서대로
        ByObjectCount // 기물 개수 순
    }

    public void Initialize(List<TextAsset> levelJsonList, System.Action<int> onWorldSelected = null)
    {
        _fullLevelList = levelJsonList;
        _onWorldSelectedCallback = onWorldSelected;
        
        // Setup filter dropdown
        if (_filterDropdown != null)
        {
            SetupFilterDropdown();
            _filterDropdown.onValueChanged.AddListener(OnFilterChanged);
        }
        
        // Setup sort dropdown
        if (_sortDropdown != null)
        {
            SetupSortDropdown();
            _sortDropdown.onValueChanged.AddListener(OnSortChanged);
        }
        
        // Show all worlds by default
        UpdateWorldList(-1);
    }

    private void CreateWorldPreview(TextAsset levelJson, int index)
    {
        if (_worldPreviewPrefab == null || _gridContainer == null) return;

        GameObject previewObj = Instantiate(_worldPreviewPrefab, _gridContainer);
        _previewList.Add(previewObj);

        // Get or create label
        TextMeshProUGUI labelText = previewObj.GetComponentInChildren<TextMeshProUGUI>();
        if (labelText == null)
        {
            // Create label if it doesn't exist
            GameObject labelObj = new GameObject("WorldLabel");
            labelObj.transform.SetParent(previewObj.transform, false);
            labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.fontSize = 20;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = Color.white;
            
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 1);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.pivot = new Vector2(0.5f, 1);
            labelRect.anchoredPosition = new Vector2(0, 0);
            labelRect.sizeDelta = new Vector2(0, 30);
        }

        // Set label text
        if (labelText != null)
        {
            labelText.text = $"World {index}";
        }

        // Get or create StageDrawer
        StageDrawer drawer = previewObj.GetComponentInChildren<StageDrawer>();
        if (drawer == null)
        {
            // Create drawer container if it doesn't exist
            GameObject drawerObj = new GameObject("StageDrawer");
            drawerObj.transform.SetParent(previewObj.transform, false);
            drawer = drawerObj.AddComponent<StageDrawer>();
            
            RectTransform drawerRect = drawerObj.GetComponent<RectTransform>();
            drawerRect.anchorMin = new Vector2(0.5f, 0);
            drawerRect.anchorMax = new Vector2(0.5f, 0);
            drawerRect.pivot = new Vector2(0.5f, 0);
            drawerRect.anchoredPosition = new Vector2(0, 5); // Small bottom padding
            drawerRect.sizeDelta = new Vector2(_previewSize.x - 10, _previewSize.y - 40); // Width and height with padding
        }
        else
        {
            // Ensure drawer has proper size and alignment
            RectTransform drawerRect = drawer.GetComponent<RectTransform>();
            if (drawerRect != null)
            {
                drawerRect.anchorMin = new Vector2(0.5f, 0);
                drawerRect.anchorMax = new Vector2(0.5f, 0);
                drawerRect.pivot = new Vector2(0.5f, 0);
                drawerRect.anchoredPosition = new Vector2(0, 5);
                drawerRect.sizeDelta = new Vector2(_previewSize.x - 10, _previewSize.y - 40);
            }
        }

        // Parse and draw map
        if (drawer != null && levelJson != null)
        {
            MapData mapData = JsonUtility.FromJson<MapData>(levelJson.text);
            if (mapData != null)
            {
                drawer.DrawMap(mapData, -1, false, false, -1); // No path line, no index
            }
        }

        // Set preview item size
        RectTransform previewRect = previewObj.GetComponent<RectTransform>();
        if (previewRect != null)
        {
            previewRect.sizeDelta = _previewSize;
        }

        // Add button component for click interaction
        Button button = previewObj.GetComponent<Button>();
        if (button == null)
        {
            button = previewObj.AddComponent<Button>();
        }
        
        // Add click listener
        int worldIndex = index; // Capture index for closure
        button.onClick.AddListener(() => OnWorldPreviewClicked(worldIndex));
    }

    public void Toggle()
    {
        if (gameObject.activeSelf)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void OnWorldPreviewClicked(int worldIndex)
    {
        // Invoke callback to load the selected world
        _onWorldSelectedCallback?.Invoke(worldIndex);
        
        // Close the panel
        Hide();
    }

    private void SetupFilterDropdown()
    {
        if (_filterDropdown == null || _fullLevelList == null) return;

        // Collect all unique OptimalPath counts
        HashSet<int> moveCounts = new HashSet<int>();
        foreach (var levelJson in _fullLevelList)
        {
            MapData mapData = JsonUtility.FromJson<MapData>(levelJson.text);
            if (mapData != null)
            {
                moveCounts.Add(mapData.OptimalPath.Count);
            }
        }

        // Create dropdown options
        _filterDropdown.ClearOptions();
        List<string> options = new List<string> { "All" };
        
        List<int> sortedCounts = new List<int>(moveCounts);
        sortedCounts.Sort();
        
        foreach (int count in sortedCounts)
        {
            options.Add($"{count} Move{(count != 1 ? "s" : "")}");
        }
        
        _filterDropdown.AddOptions(options);
        _filterDropdown.value = 0; // Default to "All"
    }

    private void SetupSortDropdown()
    {
        if (_sortDropdown == null) return;

        _sortDropdown.ClearOptions();
        List<string> options = new List<string>
        {
            "순서",           // ByIndex
            "기물 개수 순"    // ByObjectCount
        };
        
        _sortDropdown.AddOptions(options);
        _sortDropdown.value = 0; // Default to ByIndex
    }

    private void OnSortChanged(int index)
    {
        _currentSortMode = (SortMode)index;
        UpdateWorldList(_selectedMoveCount); // Refresh with current filter
    }

    private void OnFilterChanged(int index)
    {
        if (index == 0)
        {
            // "All" selected
            UpdateWorldList(-1);
        }
        else
        {
            // Extract move count from dropdown option text
            string optionText = _filterDropdown.options[index].text;
            string[] parts = optionText.Split(' ');
            if (int.TryParse(parts[0], out int moveCount))
            {
                UpdateWorldList(moveCount);
            }
        }
    }

    private void UpdateWorldList(int filterMoveCount)
    {
        ClearPreviews();
        _selectedMoveCount = filterMoveCount;

        // Create list of world data with indices
        List<WorldData> worldDataList = new List<WorldData>();
        
        for (int i = 0; i < _fullLevelList.Count; i++)
        {
            MapData mapData = JsonUtility.FromJson<MapData>(_fullLevelList[i].text);
            if (mapData == null) continue;

            // Apply filter
            if (filterMoveCount == -1 || mapData.OptimalPath.Count == filterMoveCount)
            {
                worldDataList.Add(new WorldData
                {
                    levelJson = _fullLevelList[i],
                    originalIndex = i,
                    mapData = mapData
                });
            }
        }

        // Sort based on current sort mode
        switch (_currentSortMode)
        {
            case SortMode.ByIndex:
                worldDataList.Sort((a, b) => a.originalIndex.CompareTo(b.originalIndex));
                break;
            case SortMode.ByObjectCount:
                worldDataList.Sort((a, b) => a.mapData.MapObjects.Count.CompareTo(b.mapData.MapObjects.Count));
                break;
        }

        // Create previews for sorted list
        foreach (var worldData in worldDataList)
        {
            CreateWorldPreview(worldData.levelJson, worldData.originalIndex);
        }
    }

    private class WorldData
    {
        public TextAsset levelJson;
        public int originalIndex;
        public MapData mapData;
    }

    private void ClearPreviews()
    {
        foreach (var preview in _previewList)
        {
            if (preview != null)
            {
                Destroy(preview);
            }
        }
        _previewList.Clear();
    }
}
