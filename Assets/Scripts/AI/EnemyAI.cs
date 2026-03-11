using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public class EnemyAI : MonoBehaviour
{
    #region Enums

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
        Reposition,
        Roll
    }

    #endregion

    #region Inspector Fields

    [Header("References")]
    public Transform player;

    [Header("Movement")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float speed = 2f;
    public State currentState;
    public float gravity = -9.81f;

    [Header("Combat")]
    [SerializeField] private float meleeAttackRange = 2f;
    [SerializeField] private float stopChasingRange = 10f;
    [SerializeField] private float optimalCombatRange = 4f;
    [SerializeField] private float distanceBuffer = 0.5f;

    [Header("Detection")]
    public float detectionRadius = 10f;
    public float detectionAngle = 45f;
    public float maxRetreatDistance = 5f;

    [Header("Orbit / Strafe")]
    [SerializeField] private float orbitRadius = 7f;
    [SerializeField] private float orbitSpeed = 1.5f;
    [SerializeField] private float strafeJitter = 0.3f;

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

    [Header("Player Analysis")]
    public float minSpeed = 0.1f;
    public float approachThreshold = 0.2f;
    public bool ignoreYForAnalysis = true;
    [Range(0f, 1f)] public float smoothing = 0.1f;

    [Header("State Transition Control")]
    public float stateTransitionCooldown = 0.3f;

    [Header("Pathfinding")]
    public NavMeshAgent agent;

    [Header("Weapons and Damage")]
    public GameObject hend;
    public GameObject weapon;
    public int damage = 0;

    [Header("Patrol Rest")]
    [SerializeField] private float minRestTime = 1f;
    [SerializeField] private float maxRestTime = 3f;

    [Header("Base Layer Animation")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float rollCooldown = 3f;
    [SerializeField][Range(0f, 1f)] private float rollChance = 0.3f;


    #endregion

    #region Private Fields

    private Animator animator;
    private CharacterController characterController;

    private Vector3 velocity;
    private int currentPatrolIndex = 0;
    private int orbitDirection = 1;
    private float orbitAngle = 0f;
    private float decisionTimer;
    private float smoothedApproachSpeed = 0f;
    private float lastStateChangeTime = 0f;
    private float restTimer = 0f;
    private bool isResting = false;
    private float lastRollTime = 0f;


    #endregion

    #region Unity Lifecycle

    void Start()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();

        currentState = State.Idle;
        lastPlayerPosition = player ? player.position : Vector3.zero;

        InitializeWeapon();
    }

    void Update()
    {
        ApplyGravity();

        if (!player) return;

        decisionTimer -= Time.deltaTime;
        combatStateTimer += Time.deltaTime;

        AnalyzePlayerBehavior();
        CheckState();
        HandleState();
    }

    #endregion

    #region Initialization

    private void InitializeWeapon()
    {
        if (weapon)
        {
            // TODO: Attach weapon to hend and set damage from weapon component
        }
        else
        {
            damage = 2; // Default unarmed damage
        }
    }

    #endregion

    #region State Machine

    private void CheckState()
    {
        float distance = Vector3.Distance(player.position, transform.position);
        bool canAttack = Time.time - lastAttackTime > attackCooldown;
        bool shouldReassess = combatStateTimer > maxCombatTime;
        bool hasMinCombatTime = combatStateTimer >= minCombatTime;
        bool canChangeState = Time.time - lastStateChangeTime > stateTransitionCooldown;

        if (!canChangeState) return;

        switch (currentState)
        {
            case State.Idle:
                CheckStateFromIdle();
                break;

            case State.Patrol:
                CheckStateFromPatrol(distance);
                break;

            case State.Detect_Player:
                CheckStateFromDetect(distance, hasMinCombatTime);
                break;

            case State.Engage:
                CheckStateFromEngage(distance, canAttack, hasMinCombatTime, shouldReassess);
                break;

            case State.Chase:
                CheckStateFromChase(distance);
                break;

            case State.Patrol_Player:
                CheckStateFromPatrolPlayer(distance, canAttack, hasMinCombatTime, shouldReassess);
                break;

            case State.Attack_Melee:
                CheckStateFromMeleeAttack(distance);
                break;

            case State.Reposition:
                CheckStateFromReposition(distance, hasMinCombatTime);
                break;

            case State.Retreat:
                CheckStateFromRetreat(distance, hasMinCombatTime);
                break;

            case State.Prepare:
                CheckStateFromPrepare(hasMinCombatTime);
                break;

            case State.Roll:
                CheckStateFromRoll();
                break;

            default:
                ChangeState(State.Idle);
                break;
        }
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
            case State.Roll:
                HandleRoll();
                break;
        }
    }

    private void ChangeState(State newState)
    {
        if (currentState == newState) return;

        Debug.Log($"State Change: {currentState} -> {newState} (Distance: {Vector3.Distance(player.position, transform.position):F1})");

        currentState = newState;
        lastStateChangeTime = Time.time;
        ResetCombatState();
    }

    #endregion

    #region State Transition Checks

    private void CheckStateFromIdle()
    {
        if (patrolPoints.Length > 0)
            ChangeState(State.Patrol);
        else if (CheckPlayerInVisionCone())
            ChangeState(State.Detect_Player);
    }

    private void CheckStateFromPatrol(float distance)
    {
        if (CheckPlayerInVisionCone() && distance <= detectionRadius)
            ChangeState(State.Detect_Player);
    }

    private void CheckStateFromDetect(float distance, bool hasMinCombatTime)
    {
        if (distance < stopChasingRange)
            ChangeState(State.Engage);
        else if (hasMinCombatTime)
            ChangeState(State.Idle);
    }

    private void CheckStateFromEngage(float distance, bool canAttack, bool hasMinCombatTime, bool shouldReassess)
    {
        if (distance > stopChasingRange + distanceBuffer)
            ChangeState(State.Idle);
        else if (hasMinCombatTime && (shouldReassess || decisionTimer <= 0f))
            DecideTacticalAction(distance, canAttack);
    }

    private void CheckStateFromChase(float distance)
    {
        if (distance > stopChasingRange + distanceBuffer)
        {
            ChangeState(State.Idle);
            return;
        }

        if (distance < optimalCombatRange - distanceBuffer)
            ChangeState(Random.value < aggression ? State.Chase : State.Engage);

        if (distance <= meleeAttackRange + distanceBuffer)
            ChangeState(State.Attack_Melee);
    }

    private void CheckStateFromPatrolPlayer(float distance, bool canAttack, bool hasMinCombatTime, bool shouldReassess)
    {
        if (distance > stopChasingRange + distanceBuffer)
            ChangeState(State.Idle);
        else if (distance <= meleeAttackRange && canAttack && CheckPlayerInVisionCone())
            ChangeState(State.Attack_Melee);
        else if (hasMinCombatTime && shouldReassess)
            DecideTacticalAction(distance, canAttack);
    }

    private void CheckStateFromMeleeAttack(float distance)
    {
        if (IsAttacking()) return;

        if (distance > meleeAttackRange + distanceBuffer || !CheckPlayerInVisionCone())
            ChangeState(State.Engage);
        else if (CanRoll() && Random.value < rollChance)
            ChangeState(State.Roll);                  // roll away instead of plain retreat
        else if (Random.value < 0.3f)
            ChangeState(State.Retreat);
    }

    private void CheckStateFromReposition(float distance, bool hasMinCombatTime)
    {
        if (hasMinCombatTime && (combatStateTimer > 2f || distance > stopChasingRange))
            ChangeState(State.Engage);
    }

    private void CheckStateFromRetreat(float distance, bool hasMinCombatTime)
    {
        if (hasMinCombatTime && distance > maxRetreatDistance + distanceBuffer)
            ChangeState(State.Engage);
        else if (Random.value < 0.3f && distance < optimalCombatRange)
            ChangeState(State.Patrol_Player);
    }

    private void CheckStateFromPrepare(bool hasMinCombatTime)
    {
        if (hasMinCombatTime && combatStateTimer > 1f)
            ChangeState(State.Engage);
    }

    private void CheckStateFromRoll()
    {
        // Wait for the roll animation to finish, then re-engage
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Roll"))
            ChangeState(State.Engage);
    }

    #endregion

    #region Tactical Decision Making

    private void DecideTacticalAction(float distance, bool canAttack)
    {
        float aggressionScore = CalculateAggressionScore(distance);
        float cautionScore = CalculateCautionScore(distance);

        if (TryAttack(distance, canAttack, aggressionScore, cautionScore)) return;
        if (TryReposition()) return;
        if (TryCounterPlayerApproach(aggressionScore)) return;
        if (TryPursueRetreatngPlayer(distance)) return;

        // Default decision based on distance and scores
        if (distance < meleeAttackRange - distanceBuffer && cautionScore > aggressionScore)
            ChangeState(State.Retreat);
        else if (distance > optimalCombatRange + distanceBuffer)
            ChangeState(State.Chase);
        else
            ChangeState(State.Patrol_Player);

        if (Random.value < aggression)
            ChangeState(State.Chase);
    }

    private bool TryAttack(float distance, bool canAttack, float aggressionScore, float cautionScore)
    {
        if (distance <= meleeAttackRange && canAttack && aggressionScore > cautionScore)
        {
            if (consecutiveAttacks < maxConsecutiveAttacks)
            {
                ChangeState(State.Attack_Melee);
                consecutiveAttacks++;
                lastAttackTime = Time.time;
                return true;
            }
        }
        return false;
    }

    private bool TryReposition()
    {
        if (consecutiveAttacks >= maxConsecutiveAttacks)
        {
            ChangeState(State.Reposition);
            consecutiveAttacks = 0;
            return true;
        }
        return false;
    }

    private bool TryCounterPlayerApproach(float aggressionScore)
    {
        if (playerMovingTowardsMe)
        {
            if (CanRoll() && Random.value < rollChance)
            {
                ChangeState(State.Roll);       // dodge the incoming player
                return true;
            }
            ChangeState(aggressionScore > 0.7f ? State.Prepare : State.Retreat);
            return true;
        }
        return false;
    }

    private bool TryPursueRetreatngPlayer(float distance)
    {
        if (playerMovingAway && distance > meleeAttackRange)
        {
            ChangeState(distance > optimalCombatRange + distanceBuffer ? State.Chase : State.Patrol_Player);
            return true;
        }
        return false;
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

    private void ResetCombatState()
    {
        combatStateTimer = 0f;
    }

    #endregion

    #region Player Analysis

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
            ClearPlayerMovementFlags();
            smoothedApproachSpeed = Mathf.Lerp(smoothedApproachSpeed, 0f, smoothing);
            lastPlayerPosition = currentPlayerPos;
            return;
        }

        float approachSpeed = Vector3.Dot(displacement, toEnemy.normalized) / dt;
        smoothedApproachSpeed = Mathf.Lerp(smoothedApproachSpeed, approachSpeed, smoothing);

        UpdatePlayerMovementFlags();

        lastPlayerPosition = currentPlayerPos;
    }

    private void ClearPlayerMovementFlags()
    {
        playerMovingTowardsMe = false;
        playerMovingAway = false;
    }

    private void UpdatePlayerMovementFlags()
    {
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
            ClearPlayerMovementFlags();
        }
    }

    #endregion

    #region Combat Actions

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
        return (distance < meleeAttackRange + distanceBuffer) ? Random.Range(1, 2) : 0;
    }

    public bool IsAttacking()
    {
        return animator.GetBool("isAttacking");
    }

    private bool CanRoll()
    {
        return Time.time - lastRollTime > rollCooldown;
    }

    #endregion

    #region State Behaviours

    private void TacticalEngage()
    {
        animator.SetLayerWeight(animator.GetLayerIndex("Target_layer"), 1f);
        LookAt(player.position);

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > optimalCombatRange + distanceBuffer)
        {
            MoveInDirection((player.position - transform.position).normalized);
            SetAnimatorBlend(1f, 0f);
        }
        else if (distance < optimalCombatRange - distanceBuffer)
        {
            MoveInDirection((transform.position - player.position).normalized);
            SetAnimatorBlend(-1f, 0f);
        }
        else if (distance < optimalCombatRange - distanceBuffer)
        {
            speed = walkSpeed;
            SetSpeed(2f);          // walk, not run, when backing off
            MoveInDirection((transform.position - player.position).normalized);
            SetAnimatorBlend(-1f, 0f);
        }
        else
        {
            SetAnimatorBlend(0f, 0f);
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
        SetAnimatorBlend(0f, Mathf.Sign(direction));
    }

    private void PrepareForCombat()
    {
        animator.SetLayerWeight(animator.GetLayerIndex("Target_layer"), 1f);
        SetAnimatorBlend(0f, 0f);
        LookAt(player.position);
        SetSpeed(0f);
    }

    private void PatrolPath()
    {
        animator.SetLayerWeight(animator.GetLayerIndex("Target_layer"), 0f);


        if (patrolPoints.Length == 0) return;

        if (isResting)
        {
            restTimer -= Time.deltaTime;
            SetSpeed(0f); // idle animation
            if (restTimer <= 0f)
                isResting = false;
            return;
        }

        Transform target = patrolPoints[currentPatrolIndex];

        LookAt(target.position);
        speed = walkSpeed;
        MoveTowards(target.position, speed * 0.5f);
        SetSpeed(1f);   // 0=idle, 1=walk, 2=run in your blend tree
        SetAnimatorBlend(1f, 0f);

        if (Vector3.Distance(transform.position, target.position) < 2f)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            isResting = true;
            restTimer = Random.Range(minRestTime, maxRestTime);
        }
    }

    private void CircleAroundPlayer()
    {
        animator.SetLayerWeight(animator.GetLayerIndex("Target_layer"), 1f);

        if (Random.value < 0.01f)
            orbitDirection *= -1;

        orbitAngle += orbitDirection * orbitSpeed * Time.deltaTime;

        float offsetX = Mathf.Cos(orbitAngle) * orbitRadius;
        float offsetZ = Mathf.Sin(orbitAngle) * orbitRadius;
        Vector3 orbitTarget = player.position + new Vector3(offsetX, 0, offsetZ);

        MoveTowards(orbitTarget, speed * 0.8f);
        LookAt(player.position);
        SetAnimatorBlend(Mathf.Sign(offsetZ), Mathf.Sign(offsetX));
    }

    private void RetreatFromPlayer()
    {
        animator.SetLayerWeight(animator.GetLayerIndex("Target_layer"), 1f);

        Vector3 dir = (transform.position - player.position).normalized;
        Vector3 targetPos = transform.position + dir * 2f;

        MoveTowards(targetPos, speed);
        LookAt(player.position);
        SetAnimatorBlend(-1f, 0f);
        SetSpeed(1.5f);
    }

    private void MoveForward()
    {
        animator.SetLayerWeight(animator.GetLayerIndex("Target_layer"), 1f);

        speed = runSpeed;                          // switch to run speed
        SetSpeed(2f);           // drive blend tree to Run

        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0f;
        characterController.Move(dir * speed * Time.deltaTime);
        LookAt(player.position);
        SetAnimatorBlend(1f, 0f);
    }

    private void HandleRoll()
    {
        animator.SetLayerWeight(animator.GetLayerIndex("Target_layer"), 0f); // Base Layer owns the roll
        animator.SetTrigger("Roll");

        animator.SetTrigger("Roll");
        lastRollTime = Time.time;

        // Roll away from player
        Vector3 rollDir = (transform.position - player.position).normalized;
        rollDir.y = 0f;
        characterController.Move(rollDir * speed * 1.8f * Time.deltaTime);
    }

    #endregion

    #region Movement Helpers

    private void ApplyGravity()
    {
        if (characterController.isGrounded && velocity.y < 0f)
            velocity.y = 0f;

        velocity.y -= gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    private void MoveInDirection(Vector3 dir)
    {
        dir.y = 0f;
        characterController.Move(dir * speed * Time.deltaTime);
    }

    private void MoveTowards(Vector3 target, float spd)
    {
        Vector3 dir = (target - transform.position).normalized;
        dir.y = 0f;
        characterController.Move(dir * spd * Time.deltaTime);
    }

    private void LookAt(Vector3 targetPosition)
    {
        Vector3 dir = (targetPosition - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
        }


    }

    #endregion

    #region Animator Helpers

    private void SetAnimatorBlend(float vertical, float horizontal)
    {
        animator.SetFloat("Vertical", vertical, 0.1f, Time.deltaTime);
        animator.SetFloat("Horizontal", horizontal, 0.1f, Time.deltaTime);
    }

    private void SetSpeed(float value)
    {
        animator.SetFloat("speed", value);
    }

    #endregion

    #region Detection

    private bool CheckPlayerInVisionCone()
    {
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance >= detectionRadius) return false;

        float angle = Vector3.Angle(transform.forward, dirToPlayer);
        if (angle >= detectionAngle) return false;

        Vector3 rayOrigin = transform.position + Vector3.up * 1.5f;
        if (Physics.Raycast(rayOrigin, dirToPlayer, out RaycastHit hit, detectionRadius))
            return hit.collider.CompareTag("Player");

        return false;
    }

    #endregion
}