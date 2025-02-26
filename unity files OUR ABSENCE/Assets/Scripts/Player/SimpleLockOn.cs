using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleLockOn : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float rotationSpeed = 500f; // Degrees per second (Increase for faster rotation)

    void OnEnable()
    {
        if (target == null) target = Camera.main.transform;
        StartCoroutine(LookAtTarget());
    }

    private IEnumerator LookAtTarget()
    {
        while (gameObject.activeInHierarchy)
        {
            Vector3 _dir = target.position - transform.position;
            _dir.y = 0; // Keep rotation horizontal

            if (_dir != Vector3.zero) // Avoid errors when target is exactly at the same position
            {
                Quaternion targetRotation = Quaternion.LookRotation(_dir);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            yield return null; // Wait for the next frame
        }
    }
}
