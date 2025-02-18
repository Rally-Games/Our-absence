using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Transactions;
using UnityEditor.Animations;
using UnityEditor.Experimental.GraphView;
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
    [SerializeField] private float pushForce = 5f; // Force to apply to pushable objects
    private Vector3 velocity;
    [SerializeField] private Animator animator;
    [SerializeField] private float rotationSpeed = 10f;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
        controller = GetComponent<CharacterController>();
        mainCamera = GetComponentInChildren<Camera>();
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
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        if (input != Vector2.zero)
        {
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Idle") && !animator.IsInTransition(0))
            {
                print("Walking trigger");
                animator.SetTrigger("TrWalking");
                animator.ResetTrigger("TrIdle");
            }
        }
        else if (input == Vector2.zero && animator.GetCurrentAnimatorStateInfo(0).IsName("Walking") && !animator.IsInTransition(0))
        {
            animator.ResetTrigger("TrWalking");
            animator.SetTrigger("TrIdle");
        }

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        _direction = forward * input.y + right * input.x;



        controller.Move(_direction * speed * Time.deltaTime);



        // Apply gravity
        if (!controller.isGrounded)
        {
            velocity.y -= gravity * Time.deltaTime;
        }
        else
        {
            velocity.y = -0.1f; // Small downward force to keep grounded
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

    // here do things that happen when the player collides with something that has a rigidbody
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Check if the object has a Rigidbody and is not kinematic
        Rigidbody rb = hit.collider.attachedRigidbody;
        if (rb != null && !rb.isKinematic)
        {
            // Calculate push direction (only horizontal)
            Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
            rb.AddForce(pushDir * pushForce, ForceMode.Impulse);
        }
    }
}
