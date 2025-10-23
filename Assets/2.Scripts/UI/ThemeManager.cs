using System;
using UnityEngine;

public class ThemeManager : MonoBehaviour
{
    public static ThemeManager Instance;
    public static event Action LightmodeChanged;
    public static event Action DarkmodeChanged;

    public ColorTheme LightTheme;
    public ColorTheme DarkTheme;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SetTheme(EColorMode eColorMode)
    {
        switch (eColorMode)
        {
            case EColorMode.Light:
                LightmodeChanged?.Invoke();
                break;
            case EColorMode.Dark:
                DarkmodeChanged?.Invoke();
                break;
        }
    }
}

public enum EColorMode
{
    Light,
    Dark
}
