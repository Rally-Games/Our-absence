using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// EnemyLockOn is a Unity MonoBehaviour script that enables a player character to lock onto nearby enemies.
/// It scans for enemies within a specified radius and angle, checks for line-of-sight, and manages lock-on state.
/// When locked on, it updates the camera, crosshair UI, and player orientation to face the target.
/// The script also handles unlocking when the target is out of range or blocked, and provides visual debugging with gizmos.
/// </summary>
public class EnemyLockOn : MonoBehaviour
{

    [Header("References")]
    Animator animator;
    [Tooltip("Layer Mask for Target Detection")]
    [SerializeField] LayerMask targetLayers;
    [Tooltip("Transform for Enemy Target Locator, empty object to not depend on other objects")]
    [SerializeField] Transform enemyTarget_Locator;
    [Tooltip("Camera Follow Script Reference")]
    [SerializeField] CameraFollow camFollow;
    [Tooltip("UI Canvas for Lock-On Target Display")]
    [SerializeField] Transform lockOnCanvas;
    Player_controller player_script;
    PlayerInput playerInput;
    Transform currentTarget;
    Transform cam;

    [Tooltip("StateDrivenMethod for Switching Cameras")]

    [Header("Settings")]
    [SerializeField] bool zeroVert_Look;
    [SerializeField] float noticeZone = 10;
    [SerializeField] float lookAtSmoothing = 2;
    [Tooltip("Angle Degree")][SerializeField] float maxNoticeAngle = 60;
    [SerializeField] float crossHair_Scale = 0.1f;

    bool enemyLocked;
    float currentYOffset;
    Vector3 pos;


    void Start()
    {
        player_script = GetComponent<Player_controller>();
        cam = Camera.main.transform;
        lockOnCanvas.gameObject.SetActive(false);
        playerInput = GetComponent<PlayerInput>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        camFollow.lockedTarget = enemyLocked;
        player_script.isLockOn = enemyLocked;
        if (playerInput.actions["LockOn"].triggered)
        {
            if (currentTarget)
            {
                ResetTarget();
                animator.SetLayerWeight(1, weight: 0.0f); // Ensure Target_layer is inactive when not locked on
                return;
            }
            if (currentTarget = ScanNearBy()) FoundTarget(); else ResetTarget();
        }
        if (enemyLocked)
        {
            if (!TargetOnRange()) ResetTarget();
            LookAtTarget();
            animator.SetLayerWeight(1, weight: 1.0f); // Ensure Target_layer is active when locked on
        }

    }

    /// <summary>
    /// Handles the logic when a target is found and locked onto.
    /// </summary>
    void FoundTarget()
    {
        lockOnCanvas.gameObject.SetActive(true);
        enemyLocked = true;
    }

    /// <summary>
    /// Resets the current target and lock-on state.
    /// </summary>
    void ResetTarget()
    {
        lockOnCanvas.gameObject.SetActive(false);
        currentTarget = null;
        enemyLocked = false;
    }

    /// <summary>
    /// Scans for nearby targets within the notice zone and angle.
    /// </summary>
    /// <returns>Returns the closest valid target Transform if found, otherwise null.</returns>
    private Transform ScanNearBy()
    {
        Collider[] nearbyTargets = Physics.OverlapSphere(transform.position, noticeZone, targetLayers);
        float closestAngle = maxNoticeAngle;
        Transform closestTarget = null;
        if (nearbyTargets.Length <= 0) return null;
        for (int i = 0; i < nearbyTargets.Length; i++)
        {
            Vector3 dir = nearbyTargets[i].transform.position - cam.position;
            dir.y = 0;
            float _angle = Vector3.Angle(cam.forward, dir);

            if (_angle < closestAngle)
            {
                closestTarget = nearbyTargets[i].transform;
                closestAngle = _angle;
            }
        }

        if (!closestTarget) return null;
        Transform enemyTransform = closestTarget.GetComponent<Transform>();
        float h1 = enemyTransform.localScale.y;

        currentYOffset = h1;
        if (zeroVert_Look && currentYOffset > 1.6f && currentYOffset < 1.6f * 3) currentYOffset = 1.6f;
        Vector3 tarPos = closestTarget.position + new Vector3(0, 1.8f, 0);
        if (Blocked(tarPos)) return null;
        return closestTarget;
    }

    /// <summary>
    /// Checks if there is an obstacle blocking the line of sight to the target position.
    /// </summary>
    /// <param name="t">The target position to check against.</param>
    /// <returns>True if blocked, false otherwise.</returns>
    bool Blocked(Vector3 t)
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, t - (transform.position + Vector3.up * 0.5f), out hit))
        {
            if (!hit.transform.CompareTag("Enemy")) return true;
        }
        return false;
    }

    bool TargetOnRange()
    {
        pos = currentTarget.position + new Vector3(0, currentYOffset, 0);
        float dis = (transform.position - pos).magnitude;
        if (dis / 2 > noticeZone) return false; else return true;
    }

    /// <summary>
    /// Handles the logic for looking at the current target when locked on.
    /// Updates the position and scale of the lock-on canvas, rotates the player to face the target,
    /// and informs the camera follow script of the locked target.
    /// </summary>
    private void LookAtTarget()
    {
        if (currentTarget == null)
        {
            ResetTarget();
            return;
        }
        pos = currentTarget.position + new Vector3(0, currentYOffset / 2, 0);
        lockOnCanvas.position = pos;
        lockOnCanvas.localScale = Vector3.one * ((cam.position - pos).magnitude * crossHair_Scale);

        enemyTarget_Locator.position = pos;
        Vector3 dir = currentTarget.position - transform.position;
        dir.y = 0;
        Quaternion rot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Lerp(transform.rotation, rot, Time.deltaTime * lookAtSmoothing);
        camFollow.LockOn(currentTarget);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, noticeZone);
    }
}
