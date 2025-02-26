using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;

public class OnFinishLocked : StateMachineBehaviour
{
    [SerializeField] private string animation = "";
    [SerializeField] private float CrossFade = 0.2f;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float time = stateInfo.length;
        if (time > 0) animator.gameObject.GetComponent<MonoBehaviour>().StartCoroutine(Wait());

        IEnumerator Wait()
        {
            yield return new WaitForSeconds(time - CrossFade);
            animator.CrossFade("combat_movment", CrossFade, layerIndex);

        }
    }
}
