using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEditor.Experimental.GraphView;
using UnityEngine.PlayerLoop;
public class SmoothMouseLooking : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    void Update()
    {
        Vector3 playerDir = player.forward;

        //slerp the camera to match the player's forward direction
        transform.forward = Vector3.Slerp(transform.forward, playerDir, Time.deltaTime * 5f);
    }
}