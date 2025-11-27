using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapDataVisualizer : MonoBehaviour
{
    [SerializeField] private List<TextAsset> _levelJsonList = new List<TextAsset>();
    [SerializeField] private StageDrawer _stageDrawerPrefab;

    [SerializeField] private TMP_Text _stageNumberText;
    [SerializeField] private TMP_Text _moveCountText;
    [SerializeField] private Button _prevStageButton;
    [SerializeField] private Button _nextStageButton;
    [SerializeField] private Button _prevMoveButton;
    [SerializeField] private Button _nextMoveButton;
    [SerializeField] private Button _worldListButton;

    [SerializeField] private Transform _mainViewParent;
    [SerializeField] private Transform _gridParent;
    [SerializeField] private WorldListPanel _worldListPanel;

    private int _currentStageIndex = 0;
    private int _currentMoveIndex = -1;
    private MapData _currentMapData = null;
    private StageDrawer _mainDrawer;
    private readonly List<StageDrawer> _gridDrawerList = new List<StageDrawer>();

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

        _mainDrawer = Instantiate(_stageDrawerPrefab, _mainViewParent);
        
        // Initialize world list panel
        if (_worldListPanel != null)
        {
            _worldListPanel.Initialize(_levelJsonList);
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
    }

    #region UI Function
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
        _mainDrawer.DrawMap(_currentMapData, _currentMoveIndex, true); // Show path line in main drawer
        UpdateUI();
    }

    private void StepNextMove()
    {
        _currentMoveIndex = Mathf.Min(_currentMoveIndex + 1, _currentMapData.OptimalPath.Count - 1);
        _mainDrawer.DrawMap(_currentMapData, _currentMoveIndex, true); // Show path line in main drawer
        UpdateUI();
    }
    #endregion


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

        _mainDrawer.DrawMap(_currentMapData, _currentMoveIndex, true); // Show path line in main drawer

        foreach(var drawer in _gridDrawerList)
        {
            Destroy(drawer);
        }
        _gridDrawerList.Clear();

        for(int i = 0; i < _currentMapData.OptimalPath.Count; i++)
        {
            var drawer = Instantiate(_stageDrawerPrefab, _gridParent);
            _gridDrawerList.Add(drawer);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_gridParent.GetComponent<RectTransform>());
        }

        for (int i = 0; i < _currentMapData.OptimalPath.Count; i++)
        {
            _gridDrawerList[i].DrawMap(_currentMapData, i - 1, false, true, i); // Hide path line, show index label
        }
        
        UpdateUI();
    }

    private void UpdateUI()
    {
        _stageNumberText.text = $"World: {_currentStageIndex.ToString()}";
        _moveCountText.text = $"Current Step: {(_currentMoveIndex + 1).ToString()}";
    }
    
    private void ToggleWorldList()
    {
        if (_worldListPanel != null)
        {
            _worldListPanel.Toggle();
        }
    }
}
