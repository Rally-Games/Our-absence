using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    private Animator animator;
    private CharacterController controller;

    [Header("References")]
    public Transform player;

    [Header("Movement")]
    [SerializeField] private Transform[] patrolPoints;
    public float speed = 2f;
    public State currentState;

    [Header("Combat")]
    [SerializeField] private float meleeAttackRange = 2f;
    [SerializeField] private float stopChasingRange = 10f;
    [SerializeField] private float optimalCombatRange = 4f; // Preferred fighting distance

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
    [Range(0f, 1f)] public float aggression = 0.6f; // How aggressive the enemy is
    [Range(0f, 1f)] public float caution = 0.3f;    // How cautious the enemy is
    public float combatStateTimer = 0f;
    public float minCombatTime = 1f; // Minimum time to stay in combat state
    public float maxCombatTime = 3f; // Maximum time before reassessing

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
        Engage, // New tactical engagement state
        Reposition // New repositioning state
    }

    void Start()
    {
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

        AnalyzePlayerBehavior();
        CheckState();
        HandleState();
    }

    private void AnalyzePlayerBehavior()
    {
        if (!player) return;

        Vector3 currentPlayerPos = player.position;
        Vector3 playerMovement = currentPlayerPos - lastPlayerPosition;
        Vector3 toMe = (transform.position - currentPlayerPos).normalized;

        // Check if player is moving towards or away from enemy
        playerMovingTowardsMe = Vector3.Dot(playerMovement.normalized, toMe) < -0.3f;
        playerMovingAway = Vector3.Dot(playerMovement.normalized, toMe) > 0.3f;

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
                LookAtPlayer();
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

        switch (currentState)
        {
            case State.Idle:
            case State.Patrol:
                if (CheckPlayerInVisionCone())
                {
                    currentState = State.Detect_Player;
                    ResetCombatState();
                }
                break;

            case State.Patrol_Player:
                if (distance > stopChasingRange)
                {
                    currentState = State.Idle;
                }
                else if (distance < meleeAttackRange && CheckPlayerInVisionCone())
                {
                    currentState = State.Attack_Melee;
                    ResetCombatState();
                }
                else
                {
                    DecideTacticalAction(distance, canAttack);
                }
                break;

            case State.Detect_Player:
                if (distance < stopChasingRange)
                {
                    currentState = State.Engage;
                    ResetCombatState();
                }
                else
                    currentState = State.Idle;
                break;

            case State.Engage:
                if (distance > stopChasingRange)
                {
                    currentState = State.Chase;
                }
                else if (shouldReassess || combatStateTimer > minCombatTime)
                {
                    DecideTacticalAction(distance, canAttack);
                }
                break;

            case State.Chase:
                if (distance > stopChasingRange)
                    currentState = State.Idle;
                else if (distance < optimalCombatRange)
                    currentState = State.Engage;
                break;

            case State.Attack_Melee:
                if (!IsAttacking() && (distance > meleeAttackRange * 1.2f || !CheckPlayerInVisionCone()))
                {
                    currentState = State.Engage;
                    ResetCombatState();
                }
                break;

            case State.Reposition:
                if (combatStateTimer > 2f || distance > stopChasingRange)
                {
                    currentState = State.Engage;
                    ResetCombatState();
                }
                break;

            case State.Retreat:
                if (distance > maxRetreatDistance || combatStateTimer > 2f)
                {
                    currentState = State.Engage;
                    ResetCombatState();
                }
                break;

            case State.Prepare:
                if (combatStateTimer > 1.5f)
                {
                    currentState = State.Engage;
                    ResetCombatState();
                }
                break;

            default:
                currentState = State.Idle;
                break;
        }
    }

    // --------------------
    // Tactical Combat System
    // --------------------

    private void DecideTacticalAction(float distance, bool canAttack)
    {
        float aggressionScore = CalculateAggressionScore(distance);
        float cautionScore = CalculateCautionScore(distance);

        ResetCombatState();

        // Attack if close enough, can attack, and conditions favor it
        if (distance <= meleeAttackRange && canAttack && aggressionScore > cautionScore)
        {
            if (consecutiveAttacks < maxConsecutiveAttacks)
            {
                currentState = State.Attack_Melee;
                consecutiveAttacks++;
                lastAttackTime = Time.time;
                return;
            }
        }

        // Too many consecutive attacks - must reposition
        if (consecutiveAttacks >= maxConsecutiveAttacks)
        {
            currentState = State.Reposition;
            consecutiveAttacks = 0;
            return;
        }

        // Player is charging - prepare to counter or retreat
        if (playerMovingTowardsMe)
        {
            if (aggressionScore > 0.7f)
                currentState = State.Prepare; // Stand ground and prepare counter
            else
                currentState = State.Retreat; // Back away tactically
            return;
        }

        // Player is backing away - pursue or reposition
        if (playerMovingAway)
        {
            if (distance > optimalCombatRange)
                currentState = State.Chase;
            else
                currentState = State.Patrol_Player; // Circle to cut off escape
            return;
        }

        // Default tactical decision based on distance and scores
        if (distance < meleeAttackRange * 0.8f && cautionScore > aggressionScore)
        {
            currentState = State.Retreat; // Too close, back off
        }
        else if (distance > optimalCombatRange * 1.5f)
        {
            currentState = State.Chase; // Too far, close distance
        }
        else
        {
            currentState = State.Patrol_Player; // Good range, maintain pressure
        }
    }

    private float CalculateAggressionScore(float distance)
    {
        float score = aggression;

        // More aggressive when player is closer to ideal range
        if (distance <= optimalCombatRange)
            score += 0.2f;

        // More aggressive if player is moving towards us (opportunity)
        if (playerMovingTowardsMe)
            score += 0.3f;

        // Less aggressive if we've been attacking a lot
        if (consecutiveAttacks >= maxConsecutiveAttacks - 1)
            score -= 0.4f;

        return Mathf.Clamp01(score);
    }

    private float CalculateCautionScore(float distance)
    {
        float score = caution;

        // More cautious when very close
        if (distance < meleeAttackRange * 0.7f)
            score += 0.3f;

        // More cautious if player is moving away (might be baiting)
        if (playerMovingAway)
            score += 0.2f;

        // More cautious after consecutive attacks
        if (consecutiveAttacks > 0)
            score += 0.1f * consecutiveAttacks;

        return Mathf.Clamp01(score);
    }

    private void TacticalEngage()
    {
        animator.SetLayerWeight(animator.GetLayerIndex("Target_layer"), 1f);
        LookAtPlayer();

        // Slight movement to stay active but maintain position
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > optimalCombatRange + 1f)
        {
            MoveForward();
        }
        else if (distance < optimalCombatRange - 1f)
        {
            // Slight backing up
            Vector3 backDir = (transform.position - player.position).normalized * 0.5f;
            controller.Move(backDir * speed * 0.5f * Time.deltaTime);
        }

        animator.SetFloat("Vertical", controller.velocity.x, 0.1f, Time.deltaTime);
        animator.SetFloat("Horizontal", controller.velocity.z, 0.1f, Time.deltaTime);
        SetSpeed(0.5f);
    }

    private void TacticalReposition()
    {
        animator.SetLayerWeight(animator.GetLayerIndex("Target_layer"), 1f);

        // Move to flank player or create distance
        Vector3 toPlayer = (player.position - transform.position).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, toPlayer);

        // Choose left or right based on some logic
        int direction = (transform.position.x > player.position.x) ? 1 : -1;
        Vector3 repositionTarget = transform.position + right * direction * 3f;

        MoveTowards(repositionTarget, speed * 1.2f);
        LookAtPlayer();

        Vector3 localDir = transform.InverseTransformDirection((repositionTarget - transform.position).normalized);
        animator.SetFloat("Vertical", localDir.z, 0.1f, Time.deltaTime);
        animator.SetFloat("Horizontal", localDir.x, 0.1f, Time.deltaTime);
        SetSpeed(1.2f);
    }

    private void PrepareForCombat()
    {
        animator.SetLayerWeight(animator.GetLayerIndex("Target_layer"), 1f);
        animator.SetFloat("Vertical", 0f);
        animator.SetFloat("Horizontal", 0f);
        LookAtPlayer();
        SetSpeed(0f);

        // Ready stance animation or preparation
    }

    private void TriggerTacticalAttack()
    {
        // Choose attack based on situation
        int attackType = DetermineAttackType();
        animator.SetInteger("attackType", attackType);
        animator.SetBool("isAttacking", true);

        lastAttackTime = Time.time;
    }

    private int DetermineAttackType()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        // Closer = more aggressive attacks
        if (distance < meleeAttackRange)
            return Random.Range(1, 2); // Heavy attacks
        else
            return 0; // Quick attack
    }

    private void ResetCombatState()
    {
        combatStateTimer = 0f;
    }

    // --------------------
    // Original Movement Methods (kept the same)
    // --------------------

    private void PatrolPath()
    {
        if (patrolPoints.Length == 0) return;

        Transform target = patrolPoints[currentPatrolIndex];
        MoveTowards(target.position, speed);

        if (Vector3.Distance(transform.position, target.position) < 0.5f)
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;

        SetSpeed(1f);
    }

    private void CircleAroundPlayer()
    {
        animator.SetLayerWeight(animator.GetLayerIndex("Target_layer"), 1f);

        // Occasionally flip orbit direction
        if (Random.value < 0.25f)
            orbitDirection *= -1;

        // Update orbit angle
        orbitAngle += orbitDirection * orbitSpeed * Time.deltaTime;

        // Calculate orbit offset using sin/cos
        float offsetX = Mathf.Cos(orbitAngle) * orbitRadius;
        float offsetZ = Mathf.Sin(orbitAngle) * orbitRadius;
        Vector3 orbitOffset = new Vector3(offsetX, 0, offsetZ);

        // Target orbit position relative to player
        Vector3 orbitTarget = player.position + orbitOffset;

        Debug.DrawLine(transform.position + Vector3.up * 1.5f, orbitTarget + Vector3.up * 1.5f, Color.cyan);
        // Move enemy towards orbit position
        MoveTowards(orbitTarget, speed);

        // Always look at player
        LookAtPlayer();

        // Local movement direction for Blend Tree
        Vector3 localDir = transform.InverseTransformDirection((orbitTarget - transform.position).normalized);
        animator.SetFloat("Vertical", localDir.z, 0.1f, Time.deltaTime);
        animator.SetFloat("Horizontal", localDir.x, 0.1f, Time.deltaTime);
    }

    private void RetreatFromPlayer()
    {
        animator.SetLayerWeight(animator.GetLayerIndex("Target_layer"), 1f);

        Vector3 dir = (transform.position - player.position).normalized;
        Vector3 targetPos = transform.position + dir * 3f;

        MoveTowards(targetPos, speed * 1.5f);
        LookAtPlayer();

        Vector3 localDir = transform.InverseTransformDirection((targetPos - transform.position).normalized);
        animator.SetFloat("Vertical", localDir.z, 0.1f, Time.deltaTime);
        animator.SetFloat("Horizontal", localDir.x, 0.1f, Time.deltaTime);
        SetSpeed(1.5f);
    }

    private void MoveForward()
    {
        animator.SetLayerWeight(animator.GetLayerIndex("Target_layer"), 1f);

        controller.Move(transform.forward * speed * Time.deltaTime);
        LookAtPlayer();

        animator.SetFloat("Vertical", 1f, 0.1f, Time.deltaTime);
        animator.SetFloat("Horizontal", 0f, 0.1f, Time.deltaTime);
    }

    private void LookAtPlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);

        animator.SetLayerWeight(animator.GetLayerIndex("Target_layer"), 1f);
    }

    private void MoveTowards(Vector3 target, float spd)
    {
        Vector3 move = (target - transform.position).normalized;
        controller.Move(move * spd * Time.deltaTime);
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
}