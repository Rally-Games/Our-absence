using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;



[RequireComponent(typeof(CharacterController))]
public class Player_controller : MonoBehaviour, IAnimationController
{
    private CharacterController controller;
    private PlayerInput playerInput;
    private Camera mainCamera;
    private Vector3 velocity;
    private Vector3 moveInput;
    public Vector3 direction;

    [Header("Player Settings")]
    [SerializeField] private float speed = 2.5f;
    [SerializeField] private float gravity = 9.81f;
    [SerializeField] private float rotateSpeed = 3f;
    [SerializeField] private float pushForce = 1f;

    [Header("Animation")]
    public Animator animator;
    public string currentAnimation = "Idle";

    [Header("Target Settings")]
    public bool lockMovement;

    private InputAction moveAction;
    private InputAction rollAction;
    private InputAction runAction;
    private bool isAtacking = false;
    private ObjectsState GlobalVariables;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        rollAction = playerInput.actions["Roll"];
        runAction = playerInput.actions["Run"];
        controller = GetComponent<CharacterController>();
        mainCamera = Camera.main;
    }

    void Start()
    {
        GlobalVariables = FindObjectOfType<ObjectsState>();
    }

    private void Update()
    {
        GetInput();
        if (!isAtacking) PlayerMovement();
        if (!lockMovement && !isAtacking) PlayerRotation();
        CheckAnimation();
        if (rollAction.triggered) Roll();
    }

    private void GetInput()
    {
        HandleShortcuts();

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
        //TODO: on roll make speed times a player weight number to decrease speed same for runing
        float currentSpeed = runAction.ReadValue<float>() > 0
        ? currentAnimation != "Roll" ? speed * 1.7f : speed * 0.7f : speed;

        if (velocity.y > -10) velocity.y -= Time.deltaTime * gravity;
        Vector3 movement = (direction * currentSpeed) + Vector3.up * velocity.y;
        controller.Move(movement * Time.deltaTime);
        animator.SetFloat("Horizontal", math.round(moveInput.x));
        animator.SetFloat("Vertical", math.round(moveInput.z));
        animator.SetFloat("movment", direction.magnitude, 0.1f, Time.deltaTime);
    }

    private void PlayerRotation()
    {
        if (direction.magnitude == 0) return;
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * rotateSpeed);
    }

    private void Roll()
    {
        if (!controller.isGrounded) return;
        if (direction.magnitude == 0)
        {
            ChangeAnimation("Standing Dodge Backward", 0.05f);
            return;
        }

        ChangeAnimation("Roll", 0.05f);
    }

    private void CheckAnimation()
    {
        if (currentAnimation == "Roll" || currentAnimation == "Standing Dodge Backward") return;
        if (currentAnimation == "Boxing left" || currentAnimation == "Boxing right") { isAtacking = true; return; }
        if (currentAnimation == "Boxing left lock on" || currentAnimation == "Boxing right lock on") { isAtacking = true; return; }
        isAtacking = false;
        if (moveInput.magnitude == 0)
        {
            ChangeAnimation("Idle");
        }
        else
        {
            ChangeAnimation(runAction.ReadValue<float>() > 0 ? "Running" : "Walking", 0.05f);
        }
    }

    public void ChangeAnimation(string animation, float CrossFade = 0.2f, float time = 0f, bool force = false)
    {
        if (currentAnimation.Equals(animation) && !force) return;
        if (animation == "Walking" && currentAnimation == "Running") CrossFade = 0.2f;
        if (time > 0) StartCoroutine(Wait());
        else Validate();

        IEnumerator Wait()
        {
            yield return new WaitForSeconds(time - CrossFade);
            Validate();
        }
        void Validate()
        {
            currentAnimation = animation;

            if (currentAnimation == "")
            {
                CheckAnimation();
            }
            else
                animator.CrossFade(animation, CrossFade);
        }

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
        if (Keyboard.current.iKey.wasPressedThisFrame
        || Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // Toggle menuOpen
            GlobalVariables.menuOpen = !GlobalVariables.menuOpen;

            // Update UI based on the new state
            GlobalVariables.mainMenuUI.rootVisualElement.style.display = GlobalVariables.menuOpen
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }
    }
}
