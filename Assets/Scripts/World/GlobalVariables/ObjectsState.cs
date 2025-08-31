using System.Collections;
using System.Collections.Generic;
using System.Net.Security;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.UIElements;

public class ObjectsState : MonoBehaviour
/// <summary>
/// Manages the state of the MainMenu GameObject and tracks whether the menu is open.
/// </summary>
/// <remarks>
/// - Finds the MainMenu GameObject by name at startup.
/// - Updates the <see cref="menuOpen"/> flag each frame based on the MainMenu's active state.
/// </remarks>
{
    public GameObject MainMenu;
    public UIDocument mainMenuUI;
    public bool menuOpen = false;

    void OnEnable()
    {
        MainMenu = GameObject.Find("MainMenu");
        mainMenuUI = MainMenu.GetComponentInChildren<UIDocument>();

        mainMenuUI.rootVisualElement.style.display = DisplayStyle.None;
        UnityEngine.Cursor.visible = false;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        menuOpen = mainMenuUI.rootVisualElement.style.display == DisplayStyle.Flex;
    }

}
