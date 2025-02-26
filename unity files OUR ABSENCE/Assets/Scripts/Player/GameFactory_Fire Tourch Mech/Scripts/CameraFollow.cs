using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // New Input System

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector3 offset;
    [SerializeField] Vector2 clampAxis = new Vector2(-40, 80);
    [SerializeField] float follow_smoothing = 5;
    [SerializeField] float rotate_Smoothing = 5;
    [SerializeField] float sensitivity = 60;

    private float rotX, rotY;
    private bool cursorLocked = false;
    private Transform cam;
    public bool lockedTarget = false;
    private Transform lockOnTarget;

    private PlayerInput playerInput;
    private InputAction lookAction;
    private InputAction lockOnAction;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();

        // Assign input actions from Input System
        lookAction = playerInput.actions["Look"];
        lockOnAction = playerInput.actions["LockOn"];

        lockOnAction.performed += _ => ToggleLockOn();
    }

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        cam = Camera.main.transform;
    }

    void Update()
    {
        Vector3 targetPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, follow_smoothing * Time.deltaTime);

        if (!lockedTarget)
        {
            CameraTargetRotation();
        }
        else if (lockOnTarget != null)
        {
            LookAtTarget();
        }

        //HandleCursorLock();
    }

    void CameraTargetRotation()
    {
        Vector2 mouseAxis = lookAction.ReadValue<Vector2>(); // Get mouse/gamepad input
        rotX += (mouseAxis.x * sensitivity) * Time.deltaTime;
        rotY -= (mouseAxis.y * sensitivity) * Time.deltaTime;

        rotY = Mathf.Clamp(rotY, clampAxis.x, clampAxis.y);

        Quaternion targetRotation = Quaternion.Euler(rotY, rotX, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotate_Smoothing);
    }

    void LookAtTarget()
    {
        if (lockOnTarget == null) return;

        Vector3 directionToTarget = lockOnTarget.position - transform.position;
        Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotate_Smoothing);
    }

    public void LockOn(Transform enemyTarget)
    {
        lockedTarget = true;
        lockOnTarget = enemyTarget;
    }

    public void Unlock()
    {
        lockedTarget = false;
        lockOnTarget = null;
    }

    private void ToggleLockOn()
    {
        if (lockedTarget) Unlock();
    }

    void HandleCursorLock()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            cursorLocked = !cursorLocked;
            Cursor.visible = cursorLocked;
            Cursor.lockState = cursorLocked ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }
}
