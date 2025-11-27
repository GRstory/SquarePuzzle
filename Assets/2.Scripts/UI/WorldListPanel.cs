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
    
    private List<GameObject> _previewList = new List<GameObject>();

    public void Initialize(List<TextAsset> levelJsonList)
    {
        ClearPreviews();

        for (int i = 0; i < levelJsonList.Count; i++)
        {
            CreateWorldPreview(levelJsonList[i], i);
        }
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
