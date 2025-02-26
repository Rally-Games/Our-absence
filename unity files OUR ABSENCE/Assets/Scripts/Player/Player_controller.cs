using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Cinemachine;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UIElements;


[RequireComponent(typeof(CharacterController))]
public class Player_controller : MonoBehaviour
{
    private CharacterController controller;
    private PlayerInput playerInput;
    private Camera mainCamera;
    private Vector3 velocity;
    private Vector3 moveInput;
    private Vector3 direction;

    [Header("Player Settings")]
    [SerializeField] private float speed = 2.5f;
    [SerializeField] private float gravity = 9.81f;
    [SerializeField] private float rotateSpeed = 3f;
    [SerializeField] private float pushForce = 1f;

    [Header("Animation")]
    public Animator animator;
    private string currentAnimation = "Idle";

    [Header("Target Settings")]
    public bool lockMovement;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction runAction;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Roll"];
        runAction = playerInput.actions["Run"];
        controller = GetComponent<CharacterController>();
        mainCamera = Camera.main;

        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;
    }

    private void Update()
    {
        GetInput();
        PlayerMovement();
        if (!lockMovement) PlayerRotation();
        CheckAnimation();
        if (jumpAction.triggered) OnJump();
    }

    private void GetInput()
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
        float currentSpeed = runAction.ReadValue<float>() > 0 ? speed + 5f : speed;

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

    private void OnJump()
    {
        if (controller.isGrounded)
        {
            ChangeAnimation("Roll");
        }
    }

    private void CheckAnimation()
    {
        if (currentAnimation == "Roll") return;

        if (moveInput.magnitude == 0)
        {
            ChangeAnimation("Idle");
        }
        else
        {
            ChangeAnimation(runAction.ReadValue<float>() > 0 ? "Running" : "Walking", 0.05f);
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
                    CheckAnimation();
                }
                else
                    animator.CrossFade(animation, CrossFade);
            }
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
