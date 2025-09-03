using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using UnityEngine;

public class BasicEnemyAI : MonoBehaviour, IAnimationController
{
    private Animator animator;
    private Transform enemy;
    private CharacterController controller;

    [Header("Movement")]
    public float speed = 5f;
    public string currentAnimation = "Idle";
    public State currentState;

    [Header("Combat")]
    [SerializeField] private float meleeAttackRange = 2f;
    //[SerializeField] private float rangedAttackRange = 7f;
    [SerializeField] private float stopChasingRange = 10f;

    [Header("Detection")]
    private Transform player;
    public float detectionRadius = 10f;
    public float detectionAngle = 45f;
    public enum State
    {
        Idle,
        Patrol,
        Chase,
        Attack_Melee,
        Attack_Ranged
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        enemy = GetComponent<Transform>();
        currentState = State.Idle;
    }
    void OnEnable()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        CheckState();
        SetAnimationByState();
        if (currentState == State.Attack_Melee)
        {
            ChangeAnimation(currentAnimation, 0.05f, 1f, true);
        }
        else
            ChangeAnimation(currentAnimation);
    }

    // State machine
    private void SetAnimationByState()
    {
        switch (currentState)
        {
            case State.Idle:
                currentAnimation = "Idle";
                break;
            case State.Patrol:
                currentAnimation = "Walk";
                break;
            case State.Chase:
                enemy.LookAt(player);
                currentAnimation = "Running";
                controller.Move(enemy.forward * speed * Time.deltaTime);
                break;
            case State.Attack_Melee:
                int attackType = Random.Range(0, 3);
                if (attackType == 0) currentAnimation = "Boxing left";
                break;
            case State.Attack_Ranged:
                currentAnimation = "Attack_Ranged";
                break;
            default:
                currentAnimation = "Idle";
                break;
        }
        animator.Play(currentAnimation);
    }

    private void CheckState()
    {
        switch (currentState)
        {
            case State.Idle:
                if (CheckPlayerInVisionCone()) //TODO: ditection by sound
                {
                    currentState = State.Chase;
                }
                break;
            case State.Patrol:
                // Check for player detection
                break;
            case State.Chase:
                if (Vector3.Distance(player.position, transform.position) > stopChasingRange)
                {
                    currentState = State.Idle;
                }
                else if (Vector3.Distance(player.position, transform.position) < meleeAttackRange && CheckPlayerInVisionCone())
                {
                    currentState = State.Attack_Melee;
                }
                break;
            case State.Attack_Melee:
                if (Vector3.Distance(player.position, transform.position) > meleeAttackRange || !CheckPlayerInVisionCone())
                {
                    currentState = State.Chase;
                }
                break;
            case State.Attack_Ranged:
                // Check for player detection
                break;
        }
    }

    public void ChangeAnimation(string animation, float CrossFade = 0.2f, float time = 0f, bool force = false)
    {
        if (currentAnimation.Equals(animation) && !force || animation == "") return;
        if (animation == "Walking" && currentAnimation == "Running") CrossFade = 0.2f;
        if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f && force)
            return;
        else
            animator.Play(animation, 0, 0f); // Reset the animation to the start
        if (time > 0) StartCoroutine(Wait());
        else Validate();

        IEnumerator Wait()
        {
            yield return new WaitForSeconds(time - CrossFade);
            Validate();
        }
        void Validate()
        {
            if (animation == "")
            {
                animator.CrossFade(currentAnimation, CrossFade);
            }
            else
            {
                currentAnimation = animation;
                animator.CrossFade(animation, CrossFade);
            }
        }

    }

    private bool CheckPlayerInVisionCone()
    {
        // Direction from enemy to player
        Vector3 dirToPlayer = (player.position - transform.position).normalized;

        // Distance check
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance < detectionRadius)
        {
            // Angle check
            float angle = Vector3.Angle(transform.forward, dirToPlayer);
            if (angle < detectionAngle)
            {
                // Optional: line of sight check with raycast
                Vector3 rayOrigin = transform.position + Vector3.up * 1.5f; // adjust height
                if (Physics.Raycast(rayOrigin, dirToPlayer, out RaycastHit hit, detectionRadius))
                {
                    Debug.DrawLine(rayOrigin, hit.point, Color.red);
                    if (hit.collider.CompareTag("Player"))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

}
