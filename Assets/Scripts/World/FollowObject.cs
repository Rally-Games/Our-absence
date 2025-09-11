using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowObject : MonoBehaviour
{
    public GameObject objectToFollow;
    public Vector3 offsetPosition;
    public Vector3 offsetRotation;
    public bool freezRotationX, freezRotationY, freezRotationZ;
    // Update is called once per frame
    void Update()
    {
        if (objectToFollow != null)
        {
            transform.position = objectToFollow.transform.position + offsetPosition;
            Vector3 targetEuler = objectToFollow.transform.eulerAngles + offsetRotation;

            if (freezRotationX)
                targetEuler.x = offsetRotation.x;
            if (freezRotationY)
                targetEuler.y = offsetRotation.y;
            if (freezRotationZ)
                targetEuler.z = offsetRotation.z;

            transform.rotation = Quaternion.Euler(targetEuler);
        }
    }
}
