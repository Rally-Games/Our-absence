using System;
using System.Linq;
using System.Net.WebSockets;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AnimationManager))]
public class Player_controller : MonoBehaviour
{
    private CharacterController controller;
    private PlayerInput playerInput;
    private Camera mainCamera;
    private AnimationManager animationManager;
    private MovementAnimationController movementAnimations;
    private AttackAnimationController attackAnimations;
    private Rig playerRig;

    private Vector3 velocity;
    private Vector3 moveInput;
    public Vector3 direction;

    [Header("Player Settings")]
    [SerializeField] private float speed = 2.5f;
    [SerializeField] private float gravity = 9.81f;
    [SerializeField] private float rotateSpeed = 3f;
    [SerializeField] private float pushForce = 1f;
    public bool isPickingUp = false;

    [Header("Target Settings")]

    private InputAction moveAction;
    private InputAction rollAction;
    private InputAction runAction;
    private InputAction lockOnAction;
    private InputAction playerLeftAttack;
    private InputAction playerRightAttack;
    private InputAction undrawWeaponAction;

    [Header("Equipment")]
    private EquipmentControl equipmentManager;

    [Header("Equipment Prefabs")]
    public GameObject leftWeaponPrefab;
    public GameObject rightWeaponPrefab;

    public bool isLockOn = false;

    private ObjectsState GlobalVariables;

    private void Awake()
    {
        InitializeComponents();
        InitializeInput();
    }

    private void InitializeComponents()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
        animationManager = GetComponent<AnimationManager>();
        equipmentManager = FindObjectOfType<EquipmentControl>();
        playerRig = GetComponentInChildren<Rig>();
        mainCamera = Camera.main;

        // Initialize movement animation controller
        movementAnimations = new MovementAnimationController(animationManager);
        attackAnimations = new AttackAnimationController(animationManager);
    }

    private void InitializeInput()
    {
        moveAction = playerInput.actions["Move"];
        rollAction = playerInput.actions["Roll"];
        runAction = playerInput.actions["Run"];
        lockOnAction = playerInput.actions["LockOn"];
        playerLeftAttack = playerInput.actions["Fire"];
        playerRightAttack = playerInput.actions["SecFire"];
        undrawWeaponAction = playerInput.actions["UndrawWeapon"];
    }

    void Start()
    {
        GlobalVariables = FindObjectOfType<ObjectsState>();
    }

    public void UndrawWeapon(InventoryItem item1, InventoryItem item2)
    {
        if (animationManager.animator.GetBool("isWeaponDrawn"))
        {
            animationManager.SetBoolParam("isWeaponDrawn", false);

            // Remove the weapon from the left hand
            foreach (Transform child in leftWeaponPrefab.transform)
            {
                if (item1?.name != null ? child.name.Contains(item1.name) : false ||
                item2?.name != null ? child.name.Contains(item2.name) : false)
                {
                    Destroy(child.gameObject);
                    Debug.Log($"Destroyed weapon: {child.name} (matched by name)");
                    break;
                }
            }

            foreach (Transform child in rightWeaponPrefab.transform)
            {
                if (item1?.name != null ? child.name.Contains(item1.name) : false ||
                item2?.name != null ? child.name.Contains(item2.name) : false)
                {
                    Destroy(child.gameObject);
                    Debug.Log($"Destroyed weapon: {child.name} (matched by name)");
                    break;
                }
            }
        }
    }

    private void Update()
    {
        GetInput();

        if (undrawWeaponAction.triggered && GlobalVariables.menuOpen == false)
        {
            var item1 = equipmentManager.equipmentSlots["LW1"]?.item;
            var item2 = equipmentManager.equipmentSlots["RW1"]?.item;
            UndrawWeapon(item1, item2);
        }

        if (playerLeftAttack.triggered && GlobalVariables.menuOpen == false)
        {
            var item = equipmentManager.equipmentSlots["LW1"]?.item;

            if (animationManager.animator.GetBool("isWeaponDrawn") || item == null)
                attackAnimations.TriggerLeftWeaponAttack(item?.itemID ?? 0.0f);
            else
            {
                animationManager.TriggerAnimation("drawWeapon");

                var spawnedItem = Instantiate(
                    item.objectRef,
                    leftWeaponPrefab.transform.position,
                    leftWeaponPrefab.transform.rotation,
                    leftWeaponPrefab.transform
                );
                spawnedItem.transform.SetParent(leftWeaponPrefab.transform, false);
                spawnedItem.GetComponentInChildren<Rigidbody>().isKinematic = true;
                spawnedItem.GetComponentInChildren<Collider>().enabled = false;
                spawnedItem.transform.localRotation = Quaternion.Euler(90f, 90f, 0f);
                spawnedItem.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                spawnedItem.tag = leftWeaponPrefab.tag;
                spawnedItem.layer = leftWeaponPrefab.layer;

            }
        }

        if (playerRightAttack.triggered && GlobalVariables.menuOpen == false)
        {
            var item = equipmentManager.equipmentSlots["RW1"]?.item;

            if (animationManager.animator.GetBool("isWeaponDrawn") || item == null)
                attackAnimations.TriggerRightWeaponAttack(item?.itemID ?? 0.0f);
            else
            {
                animationManager.TriggerAnimation("drawWeapon");

                var spawnedItem = Instantiate(
                                    item.objectRef,
                                    rightWeaponPrefab.transform.position,
                                    rightWeaponPrefab.transform.rotation,
                                    rightWeaponPrefab.transform
                                );
                spawnedItem.transform.SetParent(rightWeaponPrefab.transform, false);
                spawnedItem.GetComponentInChildren<Rigidbody>().isKinematic = true;
                spawnedItem.GetComponentInChildren<Collider>().enabled = false;
                spawnedItem.transform.localRotation = Quaternion.Euler(90f, 0, 90f);
                spawnedItem.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                spawnedItem.tag = rightWeaponPrefab.tag;
                spawnedItem.layer = rightWeaponPrefab.layer;
            }
        }

        if (lockOnAction.triggered)
        {
            if (animationManager.ActiveLayersIndex[animationManager.GetLayerIndexByName("Target_layer")] == 0)
                animationManager.SetLayerWeight(animationManager.GetLayerIndexByName("Target_layer"), weight: 1.0f);
            else if (animationManager.ActiveLayersIndex[animationManager.GetLayerIndexByName("Target_layer")] == 1)
                animationManager.SetLayerWeight(animationManager.GetLayerIndexByName("Target_layer"), weight: 0.0f);
        }

        if (!attackAnimations.IsAttacking())
        {
            PlayerMovement();
        }

        if (!isLockOn && !attackAnimations.IsAttacking())
            PlayerRotation();

        if (rollAction.triggered)
            animationManager.HandleRollAnimation(direction, controller.isGrounded);

        if (isPickingUp || playerRig.weight > 0.0f)
            PickUpItemAnimation();
    }

    private void GetInput()
    {
        HandleShortcuts();
        ProcessMovementInput();
    }

    private void ProcessMovementInput()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        moveInput = new Vector3(input.x, 0, input.y);

        Vector3 forward = mainCamera.transform.forward;
        Vector3 right = mainCamera.transform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        direction = (forward * input.y + right * input.x).normalized;
    }

    private void PlayerMovement()
    {
        float currentSpeed = CalculateCurrentSpeed();
        ApplyGravity();

        Vector3 movement = (direction * currentSpeed) + Vector3.up * velocity.y;
        controller.Move(movement * Time.deltaTime);

        // Update movement parameters for animation
        movementAnimations.UpdateMovementParameters(animationManager.animator, moveInput, direction, IsRunning());
    }

    public void PickUpItemAnimation(float interval = 3.0f)
    {
        if (playerRig.weight < 1.0f && isPickingUp)
        {
            playerRig.weight += Time.deltaTime * interval;
        }
        else
        {
            playerRig.weight -= Time.deltaTime * interval;
            isPickingUp = false;
        }

    }

    private float CalculateCurrentSpeed()
    {
        bool isRunning = IsRunning();
        bool isRolling = animationManager.currentAnimation == "Roll";

        if (isRolling)
            return speed * 0.7f;
        else if (isRunning)
            return speed * 1.7f;
        else
            return speed;
    }

    private bool IsRunning()
    {
        return runAction.ReadValue<float>() == 1;
    }

    private void ApplyGravity()
    {
        if (velocity.y > -10)
            velocity.y -= Time.deltaTime * gravity;
    }

    private void PlayerRotation()
    {
        if (direction.magnitude == 0) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotateSpeed);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody rb = hit.collider.attachedRigidbody;
        if (rb != null && !rb.isKinematic)
        {
            Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
            rb.AddForce(pushDir * pushForce, ForceMode.Impulse);
        }
    }

    private void HandleShortcuts()
    {
        if (Keyboard.current.iKey.wasPressedThisFrame ||
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            GlobalVariables.menuOpen = !GlobalVariables.menuOpen;
            GlobalVariables.mainMenuUI.rootVisualElement.style.display =
                GlobalVariables.menuOpen ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}