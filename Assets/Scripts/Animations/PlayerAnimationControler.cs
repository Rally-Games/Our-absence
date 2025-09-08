using System;
using System.Collections;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.InputSystem;

// AnimationManager.cs - Main animation management class

public class AnimationManager : MonoBehaviour, IAnimationController
{
    [Header("Animation")]
    public Animator animator;
    public string currentAnimation = "Idle";

    private MovementAnimationController movementAnimations;
    private AttackAnimationController attackAnimations;
    private DodgeAnimationController dodgeAnimations;

    private bool isAttackFinished = false;
    private bool isInAttackAnimation = false;

    public int ActiveLayerIndex { get; private set; } = 0;

    private void Awake()
    {
        movementAnimations = new MovementAnimationController(this);
        attackAnimations = new AttackAnimationController(this);
        dodgeAnimations = new DodgeAnimationController(this);
    }

    private void Update()
    {
        ActiveLayerIndex = animator.GetLayerWeight(1) == 1.0f ? 1 : 0;
    }

    public void OnAttackAnimationEnd(string animationName)
    {
        bool animationEnded = false;
        for (int i = 0; i < animator.layerCount; i++)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(i);
            if (stateInfo.IsName(animationName) &&
                stateInfo.normalizedTime >= 1f &&
                isInAttackAnimation)
            {
                animationEnded = true;
                break;
            }
        }

        if (animationEnded)
        {
            isAttackFinished = true;
            isInAttackAnimation = false;
            ChangeAnimation("Idle", 0.1f);
        }
    }

    public void OnAttackAnimationStart()
    {
        isAttackFinished = false;
        isInAttackAnimation = true;
    }

    public void OnDodgeAnimationEnded()
    {
        for (int i = 0; i < animator.layerCount; i++)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(i);
            if ((stateInfo.IsName("Roll") ||
            stateInfo.IsName("Standing Dodge Backward") ||
            stateInfo.IsName("Locked Roll") ||
            stateInfo.IsName("Locked Standing Dodge Backward")) &&
            stateInfo.normalizedTime >= 1.0f)
            {
                //ChangeAnimation("Idle", 0.1f);
            }
        }
    }

    public void CheckMovementAnimation(Vector3 moveInput, bool isRunning, bool isAttacking)
    {
        if (IsInSpecialAnimation()) return;
        if (IsAttacking() && !isAttackFinished) return;

        movementAnimations.HandleMovementAnimation(moveInput, isRunning);
    }

    public string CheckAttackAnimation(InputAction primaryAttackAction, InputAction secondaryAttackAction)
    {
        if (IsInSpecialAnimation()) return null;

        // Allow new attacks even during attack animations for combos
        if (IsAttacking() && !CanChainAttack()) return currentAnimation;

        if (primaryAttackAction.triggered)
        {
            OnAttackAnimationStart();
            attackAnimations.ExecuteLeftWeaponAttack(ActiveLayerIndex == 1);
        }
        else if (secondaryAttackAction.triggered)
        {
            OnAttackAnimationStart();
            attackAnimations.ExecuteRightWeaponAttack(ActiveLayerIndex == 1);
        }
        return currentAnimation;
    }

    // Executes the given action only if the specified animation has ended/completed
    public void ExecuteIfAnimationEnded(string animationName, Action action)
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName(animationName) && stateInfo.normalizedTime >= 0.99f)
        {
            action?.Invoke();
        }

    }
    private bool CanChainAttack()
    {
        // Add logic here if you want to allow attack chaining/combos
        return false;
    }

    public void HandleRollAnimation(Vector3 direction, bool isGrounded)
    {
        if (!isGrounded) return;
        dodgeAnimations.HandleRoll(direction);
    }

    public bool IsAttacking()
    {
        return attackAnimations.IsAttacking() || isInAttackAnimation;
    }

    public bool IsInSpecialAnimation()
    {
        return dodgeAnimations.IsInDodgeAnimation();
    }

    public void ChangeAnimation(string animation, float CrossFade = 0.2f, float time = 0f, bool force = false)
    {
        if (currentAnimation.Equals(animation) && !force) return;

        // Special crossfade handling for smooth transitions
        if (animation == "Walking" && currentAnimation == "Running")
            CrossFade = 0.2f;

        // Faster transition from attacks to idle
        if (attackAnimations.IsAttackAnimation(currentAnimation) && animation == "Idle")
            CrossFade = 0.05f;

        if (time > 0)
            StartCoroutine(DelayedAnimation(animation, CrossFade, time));
        else
            ExecuteAnimation(animation, CrossFade);
    }

    private IEnumerator DelayedAnimation(string animation, float CrossFade, float time)
    {
        yield return new WaitForSeconds(time - CrossFade);
        ExecuteAnimation(animation, CrossFade);
    }

    private void ExecuteAnimation(string animation, float CrossFade)
    {
        currentAnimation = animation;

        if (string.IsNullOrEmpty(currentAnimation))
        {
            CheckMovementAnimation(Vector3.zero, false, false);
        }
        else
        {
            animator.CrossFade(animation, CrossFade);
        }
    }
}

// AttackAnimationController.cs - Updated with better attack state tracking
public class AttackAnimationController
{
    private AnimationManager animationManager;

    // Animation names
    private const string BOXING_LEFT = "Boxing left";
    private const string BOXING_RIGHT = "Boxing right";
    private const string BOXING_LEFT_LOCK_ON = "Boxing left lock on";
    private const string BOXING_RIGHT_LOCK_ON = "Boxing right lock on";

    public AttackAnimationController(AnimationManager manager)
    {
        animationManager = manager;
    }

    public bool IsAttacking()
    {
        return IsAttackAnimation(animationManager.currentAnimation);
    }

    public bool IsAttackAnimation(string animationName)
    {
        if (animationName == BOXING_LEFT ||
            animationName == BOXING_RIGHT ||
            animationName == BOXING_LEFT_LOCK_ON ||
            animationName == BOXING_RIGHT_LOCK_ON)
        {
            // Check if the animation is at the end (normalizedTime >= 1.0)
            Animator animator = animationManager.animator;
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName(animationName) && stateInfo.normalizedTime < 1.0f)
            {
                return true;
            }
            return false;
        }
        return false;
    }

    public string ExecuteLeftWeaponAttack(bool isLockedOn = false)
    {
        string animation = isLockedOn ? BOXING_LEFT_LOCK_ON : BOXING_LEFT;
        animationManager.ChangeAnimation(animation, 0.05f); // Faster transition into attack
        return animation;
    }

    public string ExecuteRightWeaponAttack(bool isLockedOn = false)
    {
        string animation = isLockedOn ? BOXING_RIGHT_LOCK_ON : BOXING_RIGHT;
        animationManager.ChangeAnimation(animation, 0.05f); // Faster transition into attack
        return animation;
    }

    public bool IsLockedOn()
    {
        return animationManager.animator.GetLayerWeight(1) == 1.0f; // Layer index 1 is the targeting layer
    }
}

// MovementAnimationController.cs - No changes needed
public class MovementAnimationController
{
    private AnimationManager animationManager;

    // Animation names
    private const string IDLE = "Idle";
    private const string WALKING = "Walking";
    private const string RUNNING = "Running";

    public MovementAnimationController(AnimationManager manager)
    {
        animationManager = manager;
    }

    public void HandleMovementAnimation(Vector3 moveInput, bool isRunning)
    {
        if (moveInput.magnitude == 0)
        {
            animationManager.ChangeAnimation(IDLE);
        }
        else if (moveInput.magnitude > 0)
        {
            string targetAnimation = isRunning ? RUNNING : WALKING;
            animationManager.ChangeAnimation(targetAnimation, 0.05f);
        }
    }

    public void UpdateMovementParameters(Animator animator, Vector3 moveInput, Vector3 direction)
    {
        animator.SetFloat("Horizontal", Mathf.Round(moveInput.x));
        animator.SetFloat("Vertical", Mathf.Round(moveInput.z));
        animator.SetFloat("movment", direction.magnitude, 0.1f, Time.deltaTime);
    }
}

// DodgeAnimationController.cs - No changes needed
public class DodgeAnimationController
{
    private AnimationManager animationManager;

    // Animation names
    private const string ROLL = "Roll";
    private const string STANDING_DODGE_BACKWARD = "Standing Dodge Backward";
    private const string LOCKED_ROLL = "Locked Roll";
    private const string LOCKED_STANDING_DODGE_BACKWARD = "Locked Standing Dodge Backward";

    public DodgeAnimationController(AnimationManager manager)
    {
        animationManager = manager;
    }

    public void HandleRoll(Vector3 direction)
    {
        switch (animationManager.ActiveLayerIndex)
        {
            case 0: // Free movement layer
                if (direction.magnitude > 0.1f)
                    animationManager.ChangeAnimation(ROLL, 0.05f);
                else
                    animationManager.ChangeAnimation(STANDING_DODGE_BACKWARD, 0.05f);
                break;
            case 1: // Targeting layer
                if (direction.magnitude > 0.1f)
                    animationManager.ChangeAnimation(LOCKED_ROLL, 0.05f);
                else
                    animationManager.ChangeAnimation(LOCKED_STANDING_DODGE_BACKWARD, 0.05f);
                break;
        }
    }

    public bool IsInDodgeAnimation()
    {
        return animationManager.currentAnimation == ROLL ||
               animationManager.currentAnimation == STANDING_DODGE_BACKWARD ||
               animationManager.currentAnimation == LOCKED_ROLL ||
               animationManager.currentAnimation == LOCKED_STANDING_DODGE_BACKWARD;
    }
}