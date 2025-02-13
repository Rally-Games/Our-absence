using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
public class SmoothMouseLooking : MonoBehaviour
{
    [SerializeField] private float sensitivity = 2.0f;
    [SerializeField] private int POV = 60;
    private PlayerInput playerInput;
    private InputAction lookAction;
    public Transform player;
    float cameraVerticalRotation = 0;
    void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playerInput = GetComponent<PlayerInput>();
        lookAction = playerInput.actions["Look"];
        Camera.main.fieldOfView = POV;

    }

    void Update()
    {
        // Get the mouse input
        float mouseX = lookAction.ReadValue<Vector2>().x * sensitivity * Time.deltaTime;
        float mouseY = lookAction.ReadValue<Vector2>().y * sensitivity * Time.deltaTime;

        // Rotate the player horizontally

        // Rotate the camera vertically
        cameraVerticalRotation -= mouseY;
        cameraVerticalRotation = Mathf.Clamp(cameraVerticalRotation, -90, 90);
        transform.localEulerAngles = Vector3.right * cameraVerticalRotation;

        // Rotate the player horizontally
        player.Rotate(Vector3.up * mouseX);

    }
}