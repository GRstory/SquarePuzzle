using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_HUD : MonoBehaviour
{
    [SerializeField] private TMP_Text _tryCountText;
    [SerializeField] private GameObject _finishPanel;

    public static UI_HUD Instance; 
    private int _maxSolveCount;


    private void Awake()
    {
        _finishPanel.SetActive(false);

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetMaxSolveCount(int maxSolveCount)
    {
        _maxSolveCount = maxSolveCount;
    }

    public void UpdateTryCount(int tryCount)
    {
        _tryCountText.text = $"Tries: {tryCount}/ {_maxSolveCount}";
    }

    public void FinishGame()
    {
        _finishPanel.SetActive(true);
    }
}
