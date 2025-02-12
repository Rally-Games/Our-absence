using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(CharacterController))]
public class Player_controller : MonoBehaviour
{
    private CharacterController controller;
    private PlayerInput playerInput;
    private InputAction moveAction;

    [SerializeField] private float speed = 12f;
    [SerializeField] private float gravity = 9.81f;
    [SerializeField] private float pushForce = 5f; // Force to apply to pushable objects
    private Vector3 velocity;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        MovePlayer();
    }

    void MovePlayer()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 move = new Vector3(input.x, 0, input.y);

        // Move the player using CharacterController
        controller.Move(move * speed * Time.deltaTime);

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
