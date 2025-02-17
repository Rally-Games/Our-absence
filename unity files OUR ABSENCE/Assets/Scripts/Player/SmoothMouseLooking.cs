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
    public Transform CameraTarget;
    private GameObject player;
    float cameraVerticalRotation = 0;

    // third Person View 
    public bool thirdPersonView = false;
    public float cameraDistance = 5.0f;
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playerInput = GetComponent<PlayerInput>();
        lookAction = playerInput.actions["Look"];
        GetComponent<Camera>().fieldOfView = POV;

    }

    void Update()
    {
        // Get the mouse input
        float mouseX = lookAction.ReadValue<Vector2>().x * sensitivity * Time.deltaTime;
        float mouseY = lookAction.ReadValue<Vector2>().y * sensitivity * Time.deltaTime;

        if (thirdPersonView)
        {
            // Rotate the player horizontally
            CameraTarget.Rotate(Vector3.up * mouseX);
            // Calculate the new camera position and rotation
            cameraVerticalRotation -= mouseY;
            cameraVerticalRotation = Mathf.Clamp(cameraVerticalRotation, -20, 60); // Adjust the clamp values for third person view

            Quaternion cameraRotation = Quaternion.Euler(cameraVerticalRotation, CameraTarget.eulerAngles.y, 0);
            Vector3 cameraOffset = cameraRotation * new Vector3(0, 0, -cameraDistance); // Adjust the offset as needed

            transform.position = CameraTarget.position + cameraOffset;
            transform.LookAt(CameraTarget.position + Vector3.up * 1.5f); // Adjust the look at point as needed
        }
        else
        {
            // Return the camera to the player position
            transform.position = CameraTarget.position + Vector3.up * 0.7f; // Adjust the height and forward offset as needed (player hight + eye level)
            CameraTarget.Rotate(Vector3.up * mouseX);


            // Rotate the camera vertically
            cameraVerticalRotation -= mouseY;
            cameraVerticalRotation = Mathf.Clamp(cameraVerticalRotation, -90, 90);
            transform.localEulerAngles = Vector3.right * cameraVerticalRotation;
        }
    }
}