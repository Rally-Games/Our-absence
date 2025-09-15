using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

public class PickUpItem : MonoBehaviour
{
    public InventoryItem Item;
    public float detectionRadius = 3.0f;
    private PlayerInput playerInput;

    void Awake()
    {
        playerInput = FindAnyObjectByType<PlayerInput>();
    }

    public InventoryItem IsPickUpItemInRange()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player") && playerInput.actions["Interact"].triggered)
            {
                return Item;
            }
        }
        return null;
    }

}
