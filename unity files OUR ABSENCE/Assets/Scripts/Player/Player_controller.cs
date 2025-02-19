using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Player_controller : MonoBehaviour
{
    private CharacterController controller;
    private PlayerInput playerInput;
    public InputAction moveAction;
    private InputAction jumpAction;
    private Camera mainCamera;
    private Vector3 _direction;

    [SerializeField] private float speed = 2.5f;
    private float gravity = 9.81f;
    [SerializeField] private float pushForce = 5f;
    private Vector3 velocity;
    [SerializeField] private Animator animator;
    [SerializeField] private float rotationSpeed = 10f;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
        controller = GetComponent<CharacterController>();
        mainCamera = Camera.main; // Get the main camera

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        MovePlayer();
        if (jumpAction.triggered)
        {
            OnJump();
        }
    }

    void MovePlayer()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();

        // Get camera-relative movement directions
        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 cameraRight = mainCamera.transform.right;

        // Flatten Y axis to prevent unwanted vertical movement
        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();

        // Movement direction relative to camera
        _direction = cameraForward * input.y + cameraRight * input.x;

        if (_direction.magnitude > 0)
        {
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Idle") && !animator.IsInTransition(0))
            {
                animator.SetTrigger("TrWalking");
                animator.ResetTrigger("TrIdle");
            }

            // Rotate player towards movement direction
            Quaternion targetRotation = Quaternion.LookRotation(_direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.2f);
        }
        else if (animator.GetCurrentAnimatorStateInfo(0).IsName("Walking") && !animator.IsInTransition(0))
        {
            animator.ResetTrigger("TrWalking");
            animator.SetTrigger("TrIdle");
        }

        controller.Move(_direction * speed * Time.deltaTime);

        // Apply gravity
        if (!controller.isGrounded)
        {
            velocity.y -= gravity * Time.deltaTime;
        }
        else
        {
            velocity.y = -0.1f;
        }

        controller.Move(velocity * Time.deltaTime);
    }

    void OnJump()
    {
        if (controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(2 * gravity * 1.5f);
            animator.SetTrigger("Jump");
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
}
