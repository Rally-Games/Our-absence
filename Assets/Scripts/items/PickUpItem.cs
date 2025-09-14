using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

public class PickUpItem : MonoBehaviour
{
    public InventoryItem Item;
    public GameObject obj;
    private Player_controller playerScript;
    public float detectionRadius = 3.0f;
    private MainMenuController mainMenu;
    private PlayerInput playerInput;

    void Start()
    {
        mainMenu = FindAnyObjectByType<MainMenuController>();
        playerInput = FindAnyObjectByType<PlayerInput>();
        playerScript = FindAnyObjectByType<Player_controller>();
    }

    void Update()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player") && playerInput.actions["Interact"].triggered)
            {
                playerScript.isPickingUp = true;
                mainMenu.AddItemToInventory(Item);
                obj.SetActive(false);
            }
        }
    }

}
