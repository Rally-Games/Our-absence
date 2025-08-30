using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyLockOn : MonoBehaviour
{
    Transform currentTarget;
    Animator anim;

    [SerializeField] LayerMask targetLayers;
    [SerializeField] Transform enemyTarget_Locator;

    [Tooltip("StateDrivenMethod for Switching Cameras")]
    [SerializeField] Animator cinemachineAnimator;

    [Header("Settings")]
    [SerializeField] bool zeroVert_Look;
    [SerializeField] float noticeZone = 10;
    [SerializeField] float lookAtSmoothing = 2;
    [Tooltip("Angle_Degree")][SerializeField] float maxNoticeAngle = 60;
    [SerializeField] float crossHair_Scale = 0.1f;

    PlayerInput playerInput;
    Transform cam;
    bool enemyLocked;
    float currentYOffset;
    Vector3 pos;

    [SerializeField] CameraFollow camFollow;
    [SerializeField] Transform lockOnCanvas;
    Player_controller player_script;

    void Start()
    {
        player_script = GetComponent<Player_controller>();
        anim = GetComponent<Animator>();
        cam = Camera.main.transform;
        lockOnCanvas.gameObject.SetActive(false);
        playerInput = GetComponent<PlayerInput>();
    }

    void Update()
    {
        camFollow.lockedTarget = enemyLocked;
        player_script.lockMovement = enemyLocked;
        if (playerInput.actions["LockOn"].triggered)
        {

            if (currentTarget)
            {
                //If there is already a target, Reset.
                ResetTarget();
                return;
            }
            if (currentTarget = ScanNearBy()) FoundTarget(); else ResetTarget();
        }
        if (playerInput.actions["Roll"].triggered)
        {
            if (player_script.direction.magnitude == 0)
            {
                anim.CrossFade("Dodge Backward", 0.05f);
                return;
            }
            anim.CrossFade("LockedRoll", 0.05f);
        }

        if (enemyLocked)
        {
            if (!TargetOnRange()) ResetTarget();
            LookAtTarget();
        }

    }


    void FoundTarget()
    {
        lockOnCanvas.gameObject.SetActive(true);
        anim.SetLayerWeight(1, 1);
        enemyLocked = true;
        //cinemachineAnimator.Play("TargetCamera");
    }

    void ResetTarget()
    {
        lockOnCanvas.gameObject.SetActive(false);
        currentTarget = null;
        enemyLocked = false;
        anim.SetLayerWeight(1, 0);
        //cinemachineAnimator.Play("FollowCamera");
    }


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
        float h1 = 1.0f;
        Collider collider = closestTarget.GetComponent<Collider>();
        if (collider is CapsuleCollider capsuleCollider)
        {
            h1 = capsuleCollider.height;
        }
        else if (collider is BoxCollider boxCollider)
        {
            h1 = boxCollider.size.y;
        }
        else if (collider is SphereCollider sphereCollider)
        {
            h1 = sphereCollider.radius * 2;
        }
        else if (collider is MeshCollider meshCollider)
        {
            h1 = meshCollider.bounds.size.y;
        }
        float h2 = closestTarget.localScale.y;
        float h = h1 * h2;
        float half_h = (h / 2) / 2;
        currentYOffset = h - half_h;
        if (zeroVert_Look && currentYOffset > 1.6f && currentYOffset < 1.6f * 3) currentYOffset = 1.6f;
        Vector3 tarPos = closestTarget.position + new Vector3(0, currentYOffset, 0);
        if (Blocked(tarPos)) return null;
        return closestTarget;
    }

    bool Blocked(Vector3 t)
    {
        RaycastHit hit;
        if (Physics.Linecast(transform.position + Vector3.up * 0.5f, t, out hit))
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
