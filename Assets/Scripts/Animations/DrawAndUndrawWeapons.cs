using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DrawAndUndrawWeapons : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInput inputSystem;
    [SerializeField] private AnimationManager animationManager;
    [SerializeField] private EquipmentControl equipmentManager;
    [SerializeField] private ObjectsState globalVariables;
    private AttackAnimationController attackAnimations;

    [Header("Weapon location objects Prefabs")]
    [SerializeField] private GameObject leftWeaponPrefab;
    [SerializeField] private GameObject rightWeaponPrefab;

    [Header("Inputs")]
    private InputAction undrawWeaponAction;
    private InputAction playerLeftAttack;
    private InputAction playerRightAttack;

    void Awake()
    {
        undrawWeaponAction = inputSystem.actions["UndrawWeapon"];
        playerLeftAttack = inputSystem.actions["Fire"];
        playerRightAttack = inputSystem.actions["SecFire"];

        attackAnimations = new AttackAnimationController(animationManager);

    }
    void Start()
    {
        equipmentManager = FindObjectOfType<EquipmentControl>();
        globalVariables = FindObjectOfType<ObjectsState>();
    }

    void Update()
    {
        if (undrawWeaponAction.triggered && globalVariables.menuOpen == false)
        {
            var item1 = equipmentManager.equipmentSlots["LW1"]?.item;
            var item2 = equipmentManager.equipmentSlots["RW1"]?.item;
            UndrawWeapon(item1, item2);
        }

        if (playerLeftAttack.triggered && globalVariables.menuOpen == false)
        {
            HandleLeftAttackAttempt();
        }
        else if (playerRightAttack.triggered && globalVariables.menuOpen == false)
        {
            HandleRightAttackAttempt();
        }
    }

    /// <summary>
    /// Handles the player's attempt to attack with the right weapon.
    /// If the weapon is drawn, it triggers the attack animation.
    /// If the weapon is not drawn, it triggers the draw weapon animation and instantiates the weapon model.
    /// </summary>
    public void HandleRightAttackAttempt()
    {
        var item = equipmentManager.equipmentSlots["RW1"]?.item;

        if (animationManager.animator.GetBool("isWeaponDrawn") || item?._definition == null)
            attackAnimations.TriggerRightWeaponAttack(item?._definition.ItemID ?? 0.0f);
        else if (item?._definition != null)
        {
            animationManager.TriggerAnimation("drawWeapon");

            var spawnedItem = Instantiate(
                                item._definition.ObjectRef,
                                rightWeaponPrefab.transform.position,
                                rightWeaponPrefab.transform.rotation,
                                rightWeaponPrefab.transform
                            );
            spawnedItem.transform.SetParent(rightWeaponPrefab.transform, false);
            spawnedItem.GetComponentInChildren<Rigidbody>().isKinematic = true;
            spawnedItem.transform.localRotation = Quaternion.Euler(90f, 0, 90f);
            spawnedItem.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            spawnedItem.tag = "Weapon";
            spawnedItem.layer = rightWeaponPrefab.layer;
        }
    }

    /// <summary>
    /// Handles the player's attempt to attack with the left weapon.
    /// If the weapon is drawn, it triggers the attack animation.
    /// If the weapon is not drawn, it triggers the draw weapon animation and instantiates the weapon model.
    /// </summary>
    public void HandleLeftAttackAttempt()
    {
        var item = equipmentManager.equipmentSlots["LW1"]?.item;

        if (animationManager.animator.GetBool("isWeaponDrawn") || item?._definition == null)
        {
            attackAnimations.TriggerLeftWeaponAttack(item?._definition.ItemID ?? 0.0f);
        }
        else if (item?._definition != null)
        {
            animationManager.TriggerAnimation("drawWeapon");

            var spawnedItem = Instantiate(
                item._definition.ObjectRef,
                leftWeaponPrefab.transform.position,
                leftWeaponPrefab.transform.rotation,
                leftWeaponPrefab.transform
            );
            spawnedItem.transform.SetParent(leftWeaponPrefab.transform, false);
            spawnedItem.GetComponentInChildren<Rigidbody>().isKinematic = true;
            spawnedItem.transform.localRotation = Quaternion.Euler(90f, 90f, 0f);
            spawnedItem.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            spawnedItem.tag = "Weapon";
            spawnedItem.layer = leftWeaponPrefab.layer;

        }
    }

    public void UndrawWeapon(ItemDataInstance item1, ItemDataInstance item2)
    {
        if (animationManager.animator.GetBool("isWeaponDrawn"))
        {
            animationManager.SetBoolParam("isWeaponDrawn", false);

            // Remove the weapon from the left hand
            foreach (Transform child in leftWeaponPrefab.transform)
            {
                if (item1?._definition != null ? child.name.Contains(item1._definition.name) : false ||
                item2?._definition != null ? child.name.Contains(item2._definition.name) : false)
                {
                    Destroy(child.gameObject);
                    Debug.Log($"Destroyed weapon: {child.name} (matched by name)");
                    break;
                }
            }

            foreach (Transform child in rightWeaponPrefab.transform)
            {
                if (item1?._definition != null ? child.name.Contains(item1._definition.name) : false ||
                item2?._definition != null ? child.name.Contains(item2._definition.name) : false)
                {
                    Destroy(child.gameObject);
                    Debug.Log($"Destroyed weapon: {child.name} (matched by name)");
                    break;
                }
            }
        }
    }
}
