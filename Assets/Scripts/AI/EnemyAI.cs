using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public class EnemyAI : MonoBehaviour
{
    private Animator animator;
    private CharacterController controller;

    [Header("References")]
    public Transform player;

    [Header("Movement")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float speed = 2f;
    public State currentState;
    private Vector3 velocity;
    public float gravity = 9.81f;

    [Header("Combat")]
    [SerializeField] private float meleeAttackRange = 2f;
    [SerializeField] private float stopChasingRange = 10f;
    [SerializeField] private float optimalCombatRange = 4f; // Changed: Now different from meleeAttackRange

    // NEW: Hysteresis buffers to prevent ping-ponging
    [SerializeField] private float distanceBuffer = 0.5f; // Buffer for state transitions

    [Header("Detection")]
    public float detectionRadius = 10f;
    public float detectionAngle = 45f;
    public float maxRetreatDistance = 5f;

    [Header("Target Layer Movement")]
    [SerializeField] private float orbitRadius = 7f;
    [SerializeField] private float orbitSpeed = 1.5f;
    [SerializeField] private float strafeJitter = 0.3f;
    private int orbitDirection = 1;

    [Header("Tactical Combat System")]
    [Range(0f, 1f)] public float aggression = 0.6f;
    [Range(0f, 1f)] public float caution = 0.3f;
    public float combatStateTimer = 0f;
    public float minCombatTime = 1f;
    public float maxCombatTime = 3f;

    [Header("Combat Conditions")]
    public int consecutiveAttacks = 0;
    public int maxConsecutiveAttacks = 2;
    public float lastAttackTime = 0f;
    public float attackCooldown = 1.5f;
    public bool playerMovingTowardsMe = false;
    public bool playerMovingAway = false;
    public Vector3 lastPlayerPosition;

    private int currentPatrolIndex = 0;
    private float orbitAngle = 0f;
    private float decisionTimer;

    [Header("Player Analysis (tweak)")]
    public float minSpeed = 0.1f;
    public float approachThreshold = 0.2f;
    public bool ignoreYForAnalysis = true;
    [Range(0f, 1f)] public float smoothing = 0.1f;
    private float smoothedApproachSpeed = 0f;

    // NEW: State transition cooldowns to prevent rapid switching
    [Header("State Transition Control")]
    public float stateTransitionCooldown = 0.3f;
    private float lastStateChangeTime = 0f;

    [Header("Pathfinding Obstacle Avoidance")]
    public NavMeshAgent agent;

    public enum State
    {
        Idle,
        Patrol,
        Detect_Player,
        Chase,
        Attack_Melee,
        Attack_Ranged,
        Patrol_Player,
        Retreat,
        Prepare,
        Engage,
        Reposition
    }

    void Start()
    {
        if (player) lastPlayerPosition = player.position;

        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        currentState = State.Idle;
        lastPlayerPosition = player ? player.position : Vector3.zero;
    }

    void Update()
    {
        if (!player) return;

        decisionTimer -= Time.deltaTime;
        combatStateTimer += Time.deltaTime;

        ApplyGravity();
        AnalyzePlayerBehavior();
        CheckState();
        HandleState();
    }

    private void AnalyzePlayerBehavior()
    {
        if (!player) return;

        Vector3 currentPlayerPos = player.position;
        Vector3 displacement = currentPlayerPos - lastPlayerPosition;
        float dt = Time.deltaTime;
        if (dt <= Mathf.Epsilon)
        {
            lastPlayerPosition = currentPlayerPos;
            return;
        }

        Vector3 toEnemy = transform.position - currentPlayerPos;
        if (ignoreYForAnalysis)
        {
            displacement.y = 0f;
            toEnemy.y = 0f;
        }

        float playerSpeed = displacement.magnitude / dt;
        if (playerSpeed < minSpeed)
        {
            playerMovingTowardsMe = false;
            playerMovingAway = false;
            smoothedApproachSpeed = Mathf.Lerp(smoothedApproachSpeed, 0f, smoothing);
            lastPlayerPosition = currentPlayerPos;
            return;
        }

        Vector3 dirToEnemy = toEnemy.normalized;
        float approachSpeed = Vector3.Dot(displacement, dirToEnemy) / dt;
        smoothedApproachSpeed = Mathf.Lerp(smoothedApproachSpeed, approachSpeed, smoothing);

        if (smoothedApproachSpeed > approachThreshold)
        {
            playerMovingTowardsMe = true;
            playerMovingAway = false;
        }
        else if (smoothedApproachSpeed < -approachThreshold)
        {
            playerMovingTowardsMe = false;
            playerMovingAway = true;
        }
        else
        {
            playerMovingTowardsMe = false;
            playerMovingAway = false;
        }

        lastPlayerPosition = currentPlayerPos;
    }

    private void HandleState()
    {
        switch (currentState)
        {
            case State.Idle:
                break;

            case State.Patrol:
                PatrolPath();
                break;

            case State.Detect_Player:
                LookAt(player.position);
                break;

            case State.Engage:
                TacticalEngage();
                break;

            case State.Reposition:
                TacticalReposition();
                break;

            case State.Patrol_Player:
                CircleAroundPlayer();
                break;

            case State.Retreat:
                RetreatFromPlayer();
                break;

            case State.Prepare:
                PrepareForCombat();
                break;

            case State.Chase:
                MoveForward();
                break;

            case State.Attack_Melee:
                TriggerTacticalAttack();
                break;

            case State.Attack_Ranged:
                animator.SetTrigger("Shoot");
                break;
        }
    }

    private void CheckState()
    {
        float distance = Vector3.Distance(player.position, transform.position);
        bool canAttack = Time.time - lastAttackTime > attackCooldown;
        bool shouldReassess = combatStateTimer > maxCombatTime;
        bool hasMinCombatTime = combatStateTimer >= minCombatTime;
        bool canChangeState = Time.time - lastStateChangeTime > stateTransitionCooldown;

        // Don't change states too rapidly
        if (!canChangeState) return;

        switch (currentState)
        {
            case State.Idle:
                if (patrolPoints.Length > 0)
                {
                    ChangeState(State.Patrol);
                }
                else if (CheckPlayerInVisionCone())
                {
                    ChangeState(State.Detect_Player);
                }
                break;
            case State.Patrol:
                if (CheckPlayerInVisionCone() && distance <= detectionRadius)
                {
                    ChangeState(State.Detect_Player);
                }
                break;

            case State.Detect_Player:
                if (distance < stopChasingRange)
                {
                    ChangeState(State.Engage);
                }
                else if (hasMinCombatTime)
                {
                    ChangeState(State.Idle);
                }
                break;

            case State.Engage:
                if (distance > stopChasingRange + distanceBuffer) // Added buffer
                {
                    ChangeState(State.Idle);
                }
                else if (hasMinCombatTime && (shouldReassess || decisionTimer <= 0f))
                {
                    DecideTacticalAction(distance, canAttack);
                }
                break;

            case State.Chase:
                if (distance > stopChasingRange + distanceBuffer)
                {
                    ChangeState(State.Idle);
                }
                else if (distance < optimalCombatRange - distanceBuffer) // Use optimal range, not melee range
                {
                    if (Random.value < aggression)
                        ChangeState(State.Chase);
                    else
                        ChangeState(State.Engage);
                }
                if (distance <= meleeAttackRange + distanceBuffer) // Added buffer
                {
                    ChangeState(State.Attack_Melee);
                }
                break;

            case State.Patrol_Player:
                if (distance > stopChasingRange + distanceBuffer)
                {
                    ChangeState(State.Idle);
                }
                else if (distance <= meleeAttackRange && canAttack && CheckPlayerInVisionCone())
                {
                    ChangeState(State.Attack_Melee);
                }
                else if (hasMinCombatTime && shouldReassess)
                {
                    DecideTacticalAction(distance, canAttack);
                }
                break;

            case State.Attack_Melee:
                if (!IsAttacking())
                {
                    if (distance > meleeAttackRange + distanceBuffer || !CheckPlayerInVisionCone())
                    {
                        ChangeState(State.Engage);
                    }
                    else if (Random.value < 0.3f)
                    {
                        ChangeState(State.Retreat);
                    }
                }
                break;

            case State.Reposition:
                if (hasMinCombatTime && (combatStateTimer > 2f || distance > stopChasingRange))
                {
                    ChangeState(State.Engage);
                }
                break;

            case State.Retreat:
                if (hasMinCombatTime && (distance > maxRetreatDistance + distanceBuffer))
                {
                    ChangeState(State.Engage);
                }
                else if (Random.value < 0.3f && distance < optimalCombatRange) // Occasionally switch to engage if close
                {
                    ChangeState(State.Patrol_Player);
                }
                break;

            case State.Prepare:
                if (hasMinCombatTime && combatStateTimer > 1f)
                {
                    ChangeState(State.Engage);
                }
                break;

            default:
                ChangeState(State.Idle);
                break;
        }
    }

    // NEW: Centralized state change method
    private void ChangeState(State newState)
    {
        if (currentState == newState) return;

        Debug.Log($"State Change: {currentState} -> {newState} (Distance: {Vector3.Distance(player.position, transform.position):F1})");

        currentState = newState;
        lastStateChangeTime = Time.time;
        ResetCombatState();
    }

    // --------------------
    // Tactical Combat System
    // --------------------

    private void DecideTacticalAction(float distance, bool canAttack)
    {
        float aggressionScore = CalculateAggressionScore(distance);
        float cautionScore = CalculateCautionScore(distance);

        // Attack if close enough, can attack, and conditions favor it
        if (distance <= meleeAttackRange && canAttack && aggressionScore > cautionScore)
        {
            if (consecutiveAttacks < maxConsecutiveAttacks)
            {
                ChangeState(State.Attack_Melee);
                consecutiveAttacks++;
                lastAttackTime = Time.time;
                return;
            }
        }

        // Too many consecutive attacks - must reposition
        if (consecutiveAttacks >= maxConsecutiveAttacks)
        {
            ChangeState(State.Reposition);
            consecutiveAttacks = 0;
            return;
        }

        // Player is charging - prepare to counter or retreat
        if (playerMovingTowardsMe)
        {
            if (aggressionScore > 0.7f)
                ChangeState(State.Prepare);
            else
                ChangeState(State.Retreat);
            return;
        }

        // Player is backing away - pursue or reposition
        if (playerMovingAway && distance > meleeAttackRange)
        {
            if (distance > optimalCombatRange + distanceBuffer) // Use optimal range with buffer
                ChangeState(State.Chase);
            else
                ChangeState(State.Patrol_Player);
            return;
        }

        // Default tactical decision based on distance and scores
        if (distance < meleeAttackRange - distanceBuffer && cautionScore > aggressionScore)
        {
            ChangeState(State.Retreat);
        }
        else if (distance > optimalCombatRange + distanceBuffer)
        {
            ChangeState(State.Chase);
        }
        else
        {
            ChangeState(State.Patrol_Player);
        }

        if (Random.value < aggression)
            ChangeState(State.Chase);
    }

    private float CalculateAggressionScore(float distance)
    {
        float score = aggression;

        if (distance <= optimalCombatRange)
            score += 0.2f;

        if (playerMovingTowardsMe)
            score += 0.3f;

        if (consecutiveAttacks >= maxConsecutiveAttacks - 1)
            score -= 0.4f;

        return Mathf.Clamp01(score);
    }

    private float CalculateCautionScore(float distance)
    {
        float score = caution;

        if (distance < meleeAttackRange * 0.7f)
            score += 0.3f;

        if (playerMovingAway)
            score += 0.2f;

        if (consecutiveAttacks > 0)
            score += 0.1f * consecutiveAttacks;

        return Mathf.Clamp01(score);
    }

    private void TacticalEngage()
    {
        animator.SetLayerWeight(animator.GetLayerIndex("Target_layer"), 1f);
        LookAt(player.position);
        agent.destination = player.position;

        float distance = Vector3.Distance(transform.position, player.position);

        // More precise movement control
        if (distance > optimalCombatRange + distanceBuffer)
        {
            // Move forward
            agent.speed = speed;
            animator.SetFloat("Vertical", 1f, 0.1f, Time.deltaTime);
            animator.SetFloat("Horizontal", 0f, 0.1f, Time.deltaTime);
        }
        else if (distance < optimalCombatRange - distanceBuffer)
        {
            // Move back slightly
            Vector3 backDir = (transform.position - player.position).normalized;
            agent.destination = backDir;
            agent.speed = speed;
            animator.SetFloat("Vertical", -1f, 0.1f, Time.deltaTime);
            animator.SetFloat("Horizontal", 0f, 0.1f, Time.deltaTime);
        }
        else
        {
            // Stay in position
            animator.SetFloat("Vertical", 0f, 0.1f, Time.deltaTime);
            animator.SetFloat("Horizontal", 0f, 0.1f, Time.deltaTime);
        }
    }

    private void TacticalReposition()
    {
        animator.SetLayerWeight(animator.GetLayerIndex("Target_layer"), 1f);

        Vector3 toPlayer = (player.position - transform.position).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, toPlayer);

        int direction = (transform.position.x > player.position.x) ? 1 : -1;
        Vector3 repositionTarget = transform.position + right * direction * 3f;

        MoveTowards(repositionTarget, speed * 1.2f);
        LookAt(player.position);

        animator.SetFloat("Vertical", 0f, 0.1f, Time.deltaTime);
        animator.SetFloat("Horizontal", Mathf.Sign(direction), 0.1f, Time.deltaTime);
    }

    private void PrepareForCombat()
    {
        animator.SetLayerWeight(animator.GetLayerIndex("Target_layer"), 1f);
        animator.SetFloat("Vertical", 0f);
        animator.SetFloat("Horizontal", 0f);
        LookAt(player.position);
        SetSpeed(0f);
    }

    private void TriggerTacticalAttack()
    {
        int attackType = DetermineAttackType();
        animator.SetInteger("attackType", attackType);
        animator.SetBool("isAttacking", true);
        lastAttackTime = Time.time;
    }

    private int DetermineAttackType()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance < meleeAttackRange + distanceBuffer)
            return Random.Range(1, 2);
        else
            return 0;
    }

    private void ResetCombatState()
    {
        combatStateTimer = 0f;
    }

    // --------------------
    // Movement Methods
    // --------------------

    // Replace your PatrolPath() method with this improved version
    private void PatrolPath()
    {
        if (patrolPoints.Length == 0) return;

        // Current patrol target
        Transform target = patrolPoints[currentPatrolIndex];

        // Get movement direction with obstacle avoidance

        // --- Movement ---
        LookAt(target.position);
        MoveTowards(target.position, speed * 0.5f);

        // --- Animator Blend Tree ---
        animator.SetFloat("Vertical", 1f, 0.1f, Time.deltaTime);
        animator.SetFloat("Horizontal", 0f, 0.1f, Time.deltaTime);

        // --- Arrived at waypoint ---
        if (Vector3.Distance(transform.position, target.position) < 2f)
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }

    private void CircleAroundPlayer()
    {
        animator.SetLayerWeight(animator.GetLayerIndex("Target_layer"), 1f);

        if (Random.value < 0.01f) // Reduced frequency
            orbitDirection *= -1;

        orbitAngle += orbitDirection * orbitSpeed * Time.deltaTime;

        float offsetX = Mathf.Cos(orbitAngle) * orbitRadius;
        float offsetZ = Mathf.Sin(orbitAngle) * orbitRadius;
        Vector3 orbitOffset = new Vector3(offsetX, 0, offsetZ);

        Vector3 orbitTarget = player.position + orbitOffset;

        MoveTowards(orbitTarget, speed * 0.8f);
        LookAt(player.position);

        animator.SetFloat("Vertical", Mathf.Sign(offsetZ), 0.1f, Time.deltaTime);
        animator.SetFloat("Horizontal", Mathf.Sign(offsetX), 0.1f, Time.deltaTime);
    }

    private void RetreatFromPlayer()
    {
        animator.SetLayerWeight(animator.GetLayerIndex("Target_layer"), 1f);

        Vector3 dir = (transform.position - player.position).normalized;
        Vector3 targetPos = transform.position + dir * 2f; // Reduced retreat distance

        MoveTowards(targetPos, speed);
        LookAt(player.position);

        Vector3 localDir = transform.InverseTransformDirection((targetPos - transform.position).normalized);
        animator.SetFloat("Vertical", -1f, 0.1f, Time.deltaTime);
        animator.SetFloat("Horizontal", 0f, 0.1f, Time.deltaTime);
        SetSpeed(1.5f);
    }

    private void MoveForward()
    {
        animator.SetLayerWeight(animator.GetLayerIndex("Target_layer"), 1f);

        agent.destination = player.position;
        agent.speed = speed;
        LookAt(player.position);

        animator.SetFloat("Vertical", 1f, 0.1f, Time.deltaTime);
        animator.SetFloat("Horizontal", 0f, 0.1f, Time.deltaTime);
    }

    private void LookAt(Vector3 moveDir)
    {
        Vector3 dir = (moveDir - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
        {
            // Smooth rotation using Quaternion.Lerp for extra smoothness
            Quaternion targetRotation = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
        }

        animator.SetLayerWeight(animator.GetLayerIndex("Target_layer"), 1f);
    }

    private void MoveTowards(Vector3 target, float spd)
    {
        agent.destination = target;
        agent.speed = spd;
    }

    private bool IsAttacking()
    {
        return animator.GetBool("isAttacking");
    }

    private bool CheckPlayerInVisionCone()
    {
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance < detectionRadius)
        {
            float angle = Vector3.Angle(transform.forward, dirToPlayer);
            if (angle < detectionAngle)
            {
                Vector3 rayOrigin = transform.position + Vector3.up * 1.5f;
                if (Physics.Raycast(rayOrigin, dirToPlayer, out RaycastHit hit, detectionRadius))
                {
                    if (hit.collider.CompareTag("Player"))
                        return true;
                }
            }
        }
        return false;
    }

    private void SetSpeed(float value)
    {
        animator.SetFloat("speed", value);
    }
    private void ApplyGravity()
    {
        if (velocity.y > -10)
            velocity.y -= Time.deltaTime * gravity;
    }
}