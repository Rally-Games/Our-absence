using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PickUpItem : MonoBehaviour
{
    public InventoryItem Item;
    public float detectionRadius = 3.0f;
    private MainMenuController mainMenu;
    private PlayerInput playerInput;

    void Start()
    {
        mainMenu = FindAnyObjectByType<MainMenuController>();
        playerInput = FindAnyObjectByType<PlayerInput>();
    }

    void Update()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player") && playerInput.actions["Interact"].triggered)
            {
                mainMenu.AddItemToInventory(Item);
                Destroy(gameObject);
            }
        }
    }

}
