using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[RequireComponent(typeof(CharacterController))]
public class Player_controller : MonoBehaviour
{
    private CharacterController controller;
    private PlayerInput playerInput;
    public InputAction moveAction;
    private InputAction jumpAction;
    private InputAction runAction;
    private Camera mainCamera;
    private Vector3 _direction;

    [SerializeField] private float speed = 2.5f;
    private float gravity = 9.81f;
    [SerializeField] private float pushForce = 5f;
    private Vector3 velocity;
    [SerializeField] private Animator animator;
    private string currentAnimation = "Idle";
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float jumpForce = 3f;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
        runAction = playerInput.actions["Run"];
        controller = GetComponent<CharacterController>();
        mainCamera = Camera.main; // Get the main camera

        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;
    }

    private void Update()
    {
        MovePlayer();
        if (jumpAction.triggered)
        {
            OnJump();
        }
        CheackAnimation();
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
        if (currentAnimation != "Roll")
        {
            if (_direction.magnitude > 0)
            {

                // Rotate player towards movement direction
                Quaternion targetRotation = Quaternion.LookRotation(_direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.7f);
            }

            if (runAction.ReadValue<float>() > 0)
            {
                controller.Move(_direction * (speed + 5f) * Time.deltaTime);
            }
            else
            {
                controller.Move(_direction * speed * Time.deltaTime);
            }
        }
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

            ChangeAnimation("Roll");
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

    public void ChangeAnimation(string animation, float CrossFade = 0.2f, float time = 0f)
    {
        if (time > 0) StartCoroutine(Wait());
        else Validate();

        IEnumerator Wait()
        {
            yield return new WaitForSeconds(time - CrossFade);
            Validate();
        }
        void Validate()
        {
            if (currentAnimation != animation)
            {
                currentAnimation = animation;

                if (currentAnimation == "")
                {
                    CheackAnimation();
                }
                else
                    animator.CrossFade(animation, CrossFade);
            }
        }

    }

    private void CheackAnimation()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        if (currentAnimation == "Roll") return;

        if (input.magnitude == 0)
        {
            ChangeAnimation("Idle");
        }
        else if (input.magnitude > 0)
        {
            if (runAction.ReadValue<float>() > 0)
            {
                ChangeAnimation("Running", 0.05f);
            }
            else
                ChangeAnimation("Walking", 0.05f);
        }


    }
}
